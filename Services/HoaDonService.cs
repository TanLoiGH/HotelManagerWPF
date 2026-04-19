using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System.Threading;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class HoaDonService : IHoaDonService
{
    private readonly QuanLyKhachSanContext _db;
    private readonly IKhachHangService _khachHangSvc;

    // Khóa tĩnh để đảm bảo an toàn luồng cho DbContext
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public HoaDonService(QuanLyKhachSanContext db, IKhachHangService khachHangSvc)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
    }

    #region Lấy dữ liệu (Queries)

    public async Task<List<HoaDon>> LayHoaDonsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.DichVuChiTiets)
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();
        }
        finally { _semaphore.Release(); }
    }

    public async Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        }
        finally { _semaphore.Release(); }
    }

    public async Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.ThanhToans
                .AsNoTracking()
                .Include(t => t.MaPtttNavigation)
                .Where(t => t.MaHoaDon == maHoaDon)
                .OrderBy(t => t.NgayThanhToan)
                .ToListAsync();
        }
        finally { _semaphore.Release(); }
    }

    public async Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .Include(h => h.MaNhanVienNavigation)
                .Include(h => h.HoaDonChiTiets)
                    .ThenInclude(ct => ct.DatPhongChiTiet)
                .Include(h => h.DichVuChiTiets)
                    .ThenInclude(dv => dv.MaDichVuNavigation)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        }
        finally { _semaphore.Release(); }
    }

    public async Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.MaKhuyenMaiNavigation)
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .Include(h => h.MaNhanVienNavigation)
                .Include(h => h.HoaDonChiTiets)
                    .ThenInclude(ct => ct.DatPhongChiTiet)
                .Include(h => h.DichVuChiTiets)
                    .ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        }
        finally { _semaphore.Release(); }
    }

    public async Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.PhuongThucThanhToans
                .Select(p => new PhuongThucThanhToanDto { MaPTTT = p.MaPttt, TenPhuongThuc = p.TenPhuongThuc })
                .ToListAsync();
        }
        finally { _semaphore.Release(); }
    }

    #endregion

    #region Xuất hóa đơn

    public async Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            bool hdExist = await _db.HoaDons
                .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

            if (hdExist)
                throw new InvalidOperationException("Đặt phòng này đã có hóa đơn active.");

            var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
            if (!chiTiets.Any())
                throw new InvalidOperationException("Không có phòng nào trong đặt phòng.");

            var dp = await _db.DatPhongs.FindAsync(maDatPhong);
            decimal tienCoc = dp?.TienCoc ?? 0;

            decimal tienPhong = 0;
            bool coPhongChuaNhapGia = false;
            foreach (var ct in chiTiets)
            {
                if (ct.DonGia <= 0) coPhongChuaNhapGia = true;
                int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra);
                tienPhong += ct.DonGia * soDem;
            }

            var khuyenMaiHopLe = await LayKhuyenMaiHopLeAsync(maKhuyenMai);
            if (coPhongChuaNhapGia && khuyenMaiHopLe == null)
                throw new InvalidOperationException("Phát hiện phòng chưa được thiết lập giá.");

            decimal giamGia = 0;
            if (khuyenMaiHopLe != null)
                giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, 0, khuyenMaiHopLe.LoaiKhuyenMai ?? "", khuyenMaiHopLe.GiaTriKm ?? 0);

            var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync();
            var newMaHd = MaHelper.Next("HD", lastMa);
            decimal vatPercent = SystemSettingsService.Load().VatPercent;

            // TÍNH TOÁN: VAT chỉ tính trên (TienPhong - GiamGia)
            decimal tongThanhToan = ((tienPhong - giamGia) * (1 + vatPercent / 100)) - tienCoc;

            var hd = new HoaDon
            {
                MaHoaDon = newMaHd,
                MaDatPhong = maDatPhong,
                MaNhanVien = maNhanVien,
                NgayLap = TimeHelper.GetVietnamTime(),
                TienPhong = tienPhong,
                TienDichVu = 0,
                Vat = vatPercent,
                MaKhuyenMai = maKhuyenMai,
                TongThanhToan = tongThanhToan,
                TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan
            };

            _db.HoaDons.Add(hd);
            foreach (var ct in chiTiets)
            {
                _db.HoaDonChiTiets.Add(new HoaDonChiTiet
                {
                    MaHoaDon = hd.MaHoaDon,
                    MaDatPhong = maDatPhong,
                    MaPhong = ct.MaPhong,
                    SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return hd;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.AsNoTracking().FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

            var chiTietDatPhong = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == hd.MaDatPhong).ToListAsync();
            var existingKeys = await _db.HoaDonChiTiets.Where(c => c.MaHoaDon == maHoaDon)
                .Select(c => c.MaDatPhong + "|" + c.MaPhong).ToListAsync();

            var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var toAdd = chiTietDatPhong.Where(ct => !existingSet.Contains(ct.MaDatPhong + "|" + ct.MaPhong))
                .Select(ct => new HoaDonChiTiet
                {
                    MaHoaDon = maHoaDon,
                    MaDatPhong = ct.MaDatPhong,
                    MaPhong = ct.MaPhong,
                    SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
                }).ToList();

            if (toAdd.Count == 0) return 0;
            _db.HoaDonChiTiets.AddRange(toAdd);
            await _db.SaveChangesAsync();
            return toAdd.Count;
        }
        finally { _semaphore.Release(); }
    }

    #endregion

    #region Thanh toán & Giao dịch

    public async Task<bool> ThanhToanAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null)
    {
        var thongTin = await ThanhToanVaTraKetQuaAsync(maHoaDon, soTien, maPTTT, nguoiThu, loaiGiaoDich, noiDung);
        return thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat;
    }

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (loaiGiaoDich != "Hoàn tiền" && soTien < 0) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Số tiền không hợp lệ!");
            if (loaiGiaoDich == "Hoàn tiền" && soTien > 0) soTien = -soTien;

            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            // TÍNH LẠI TỔNG THỰC TẾ: (TienPhong - GiamGia) * VAT + TienDichVu
            decimal tienPhong = hd.TienPhong ?? 0;
            decimal tienDv = hd.TienDichVu ?? 0;
            decimal vatPercent = hd.Vat ?? 0;
            decimal giamGia = 0;

            var km = !string.IsNullOrWhiteSpace(hd.MaKhuyenMai) ? await _db.KhuyenMais.FindAsync(hd.MaKhuyenMai) : null;
            if (km != null && km.IsActive == true)
                giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, tienDv, km.LoaiKhuyenMai ?? "", km.GiaTriKm ?? 0);

            decimal tongPhaiTra = ((tienPhong - giamGia) * (1 + vatPercent / 100)) + tienDv;
            var tongDaThuLichSu = await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;
            var tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            var tongDaThuHienTai = tongDaThuLichSu + tienCoc;

            if (soTien != 0)
            {
                var lastTt = await _db.ThanhToans.OrderByDescending(t => t.MaThanhToan).Select(t => t.MaThanhToan).FirstOrDefaultAsync();
                _db.ThanhToans.Add(new ThanhToan
                {
                    MaThanhToan = MaHelper.Next("TT", lastTt),
                    MaHoaDon = maHoaDon,
                    MaPttt = maPTTT,
                    SoTien = soTien,
                    LoaiGiaoDich = loaiGiaoDich,
                    NgayThanhToan = TimeHelper.GetVietnamTime(),
                    NguoiThu = nguoiThu,
                    NoiDung = noiDung
                });
            }

            if (tongDaThuHienTai + soTien >= tongPhaiTra) hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThuHienTai + soTien, tongPhaiTra - (tongDaThuHienTai + soTien), "Thành công");
        }
        catch (Exception ex) { return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, ex.Message); }
        finally { _semaphore.Release(); }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Lỗi");

            var tongDaThu = await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;
            if (tongDaThu >= (hd.TongThanhToan ?? 0)) hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThu, 0, "Xong");
        }
        finally { _semaphore.Release(); }
    }

    #endregion

    #region Trả phòng & Cập nhật tiền phòng

    public async Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Lỗi");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiemTraPhong);
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, 0, 0, "Đã cập nhật");
        }
        finally { _semaphore.Release(); }
    }

    public async Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new KeyNotFoundException("Lỗi");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? TimeHelper.GetVietnamTime());

            if (hd.MaDatPhongNavigation != null)
            {
                foreach (var ct in hd.MaDatPhongNavigation.DatPhongChiTiets)
                {
                    var p = await _db.Phongs.FindAsync(ct.MaPhong);
                    if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
                }
                hd.MaDatPhongNavigation.TrangThai = DatPhongTrangThaiTexts.DaTraPhong;
                await _khachHangSvc.NangHangAsync(hd.MaDatPhongNavigation.MaKhachHang, hd.TongThanhToan ?? 0);
            }

            hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
            await _db.SaveChangesAsync();
        }
        finally { _semaphore.Release(); }
    }

    #endregion

    #region VAT & Hủy

    public async Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hoaDons = await _db.HoaDons.Where(h => h.TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan).ToListAsync();
            foreach (var hd in hoaDons)
            {
                hd.Vat = vatPercent;
                // Tính lại tổng: (TienPhong - GiamGia) * VAT + TienDichVu
                decimal giamGia = 0; // Giả sử lấy từ logic khuyến mãi
                hd.TongThanhToan = ((hd.TienPhong ?? 0 - giamGia) * (1 + vatPercent / 100)) + (hd.TienDichVu ?? 0);
            }
            await _db.SaveChangesAsync();
            return hoaDons.Count;
        }
        finally { _semaphore.Release(); }
    }

    public async Task HuyHoaDonAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.FindAsync(maHoaDon);
            if (hd != null && hd.TrangThai != HoaDonTrangThaiTexts.DaThanhToan)
            {
                hd.TrangThai = HoaDonTrangThaiTexts.DaHuy;
                await _db.SaveChangesAsync();
            }
        }
        finally { _semaphore.Release(); }
    }

    #endregion

    #region Helper Private (Tuyệt đối KHÔNG dùng Semaphore ở đây)

    private async Task CapNhatTienPhongTheoThoiDiemTraPhongAsync(HoaDon hd, DateTime thoiDiem)
    {
        if (hd.MaDatPhongNavigation == null) return;
        var chiTiets = hd.MaDatPhongNavigation.DatPhongChiTiets.ToList();

        decimal tienPhong = 0;

        // Cài đặt giờ checkout chuẩn của khách sạn (Ví dụ 12:00 trưa)
        var gioQuyDinh = new TimeSpan(12, 0, 0);

        foreach (var ct in chiTiets)
        {
            // 1. Tính tiền những đêm khách ở bình thường
            int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra);
            decimal tienCuaPhong = ct.DonGia * soDem;

            // 2. LOGIC TÍNH PHỤ THU QUÁ HẠN (LATE CHECK-OUT)
            // Chỉ phạt khi khách trả phòng đúng vào ngày Checkout nhưng lại ra trễ giờ
            if (thoiDiem.Date == ct.NgayTra.Date && thoiDiem.TimeOfDay > gioQuyDinh)
            {
                double soGioTre = (thoiDiem.TimeOfDay - gioQuyDinh).TotalHours;
                decimal phuThu = 0;

                if (soGioTre <= 3)
                    phuThu = ct.DonGia * 0.3m; // Phạt 30%
                else if (soGioTre <= 6)
                    phuThu = ct.DonGia * 0.5m; // Phạt 50%
                else
                    phuThu = ct.DonGia;        // Phạt 100%

                tienCuaPhong += phuThu;
            }

            // Cộng dồn vào tổng tiền phòng của hóa đơn
            tienPhong += tienCuaPhong;

            var hdct = hd.HoaDonChiTiets.FirstOrDefault(x => x.MaPhong == ct.MaPhong);
            if (hdct != null) hdct.SoDem = soDem; // Bạn có thể lưu thêm thuộc tính PhuThu vào bảng HoaDonChiTiet nếu DB có cột này
        }

        hd.TienPhong = tienPhong;
        decimal vatPercent = hd.Vat ?? 0;
        decimal tienDv = hd.TienDichVu ?? 0;
        decimal tienCoc = hd.MaDatPhongNavigation.TienCoc ?? 0;

        // CÔNG THỨC: VAT chỉ tính trên tiền phòng
        hd.TongThanhToan = (tienPhong * (1 + vatPercent / 100)) + tienDv - tienCoc;

        await _db.SaveChangesAsync();
    }

    private async Task<KhuyenMai?> LayKhuyenMaiHopLeAsync(string? maKhuyenMai)
    {
        if (string.IsNullOrEmpty(maKhuyenMai)) return null;
        return await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == maKhuyenMai && k.IsActive == true);
    }

    #endregion
}

public enum KetQuaThanhToan { GhiNhanChuaDu, HoanTat, DaHoanTat, TuChoi }
public sealed record ThongTinThanhToan(KetQuaThanhToan KetQua, decimal TongDaThu, decimal ConLai, string ThongDiep);