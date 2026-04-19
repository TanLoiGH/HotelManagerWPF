using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class HoaDonService : IHoaDonService
{
    private readonly QuanLyKhachSanContext _db;
    private readonly IKhachHangService _khachHangSvc;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public HoaDonService(QuanLyKhachSanContext db, IKhachHangService khachHangSvc)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
    }

    // --- IMPLEMENT CÁC HÀM CÒN THIẾU MÀ INTERFACE YÊU CẦU ---

    public async Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon) => await LayHoaDonChiTietAsync(maHoaDon);
    public async Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon) => await LayHoaDonChiTietAsync(maHoaDon);

    public async Task<bool> ThanhToanAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null)
    {
        var res = await ThanhToanVaTraKetQuaAsync(maHoaDon, soTien, maPTTT, nguoiThu, loaiGiaoDich, noiDung);
        return res.KetQua == KetQuaThanhToan.HoanTat;
    }

    public async Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hds = await _db.HoaDons.Where(h => h.TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan).ToListAsync();
            foreach (var h in hds) h.Vat = vatPercent;
            return await _db.SaveChangesAsync();
        }
        finally { _semaphore.Release(); }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Lỗi");
            // Logic đồng bộ đơn giản
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, 0, 0, "Xong");
        }
        finally { _semaphore.Release(); }
    }

    // --- CÁC HÀM QUERIES ---

    public async Task<List<HoaDon>> LayHoaDonsAsync()
    {
        await _semaphore.WaitAsync();
        try { return await _db.HoaDons.AsNoTracking().Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.MaKhachHangNavigation).OrderByDescending(h => h.NgayLap).ToListAsync(); }
        finally { _semaphore.Release(); }
    }

    public async Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try { return await _db.HoaDons.AsNoTracking().Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.MaKhachHangNavigation).Include(h => h.HoaDonChiTiets).ThenInclude(ct => ct.DatPhongChiTiet).Include(h => h.DichVuChiTiets).ThenInclude(dv => dv.MaDichVuNavigation).Include(h => h.MaKhuyenMaiNavigation).Include(h => h.ThanhToans).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon); }
        finally { _semaphore.Release(); }
    }

    public async Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try { return await _db.ThanhToans.AsNoTracking().Include(t => t.MaPtttNavigation).Where(t => t.MaHoaDon == maHoaDon).OrderBy(t => t.NgayThanhToan).ToListAsync(); }
        finally { _semaphore.Release(); }
    }

    public async Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync()
    {
        await _semaphore.WaitAsync();
        try { return await _db.PhuongThucThanhToans.Select(p => new PhuongThucThanhToanDto { MaPTTT = p.MaPttt, TenPhuongThuc = p.TenPhuongThuc }).ToListAsync(); }
        finally { _semaphore.Release(); }
    }

    // --- NGHIỆP VỤ CHÍNH ---

    public async Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
            var dp = await _db.DatPhongs.FindAsync(maDatPhong) ?? throw new Exception("Error");
            decimal tienPhong = chiTiets.Sum(ct => ct.DonGia * TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra));
            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == maKhuyenMai && k.IsActive == true);

            var res = TinhToanHoaDonService.TinhToanToanBo(tienPhong, 0, SystemSettingsService.Load().VatPercent, km?.GiaTriKm ?? 0, km?.LoaiKhuyenMai ?? "", dp.TienCoc ?? 0, 0);

            var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync();
            var hd = new HoaDon
            {
                MaHoaDon = MaHelper.Next("HD", lastMa),
                MaDatPhong = maDatPhong,
                MaNhanVien = maNhanVien,
                NgayLap = TimeHelper.GetVietnamTime(),
                TienPhong = res.TienPhong,
                TienDichVu = 0,
                Vat = SystemSettingsService.Load().VatPercent,
                MaKhuyenMai = maKhuyenMai,
                TongThanhToan = res.TongThanhToan,
                TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan
            };
            _db.HoaDons.Add(hd);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return hd;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).Include(h => h.MaKhuyenMaiNavigation).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon) ?? throw new Exception("Null");

            if (loaiGiaoDich == "Hoàn tiền" && soTien > 0) soTien = -soTien;

            string nextTt = MaHelper.Next("TT", await _db.ThanhToans.OrderByDescending(t => t.MaThanhToan).Select(t => t.MaThanhToan).FirstOrDefaultAsync());
            _db.ThanhToans.Add(new ThanhToan { MaThanhToan = nextTt, MaHoaDon = maHoaDon, MaPttt = maPTTT, SoTien = soTien, LoaiGiaoDich = loaiGiaoDich, NgayThanhToan = TimeHelper.GetVietnamTime(), NguoiThu = nguoiThu, NoiDung = noiDung });

            if (loaiGiaoDich == "Hoàn tiền")
            {
                // Lấy mã phòng đầu tiên của hóa đơn làm gốc báo cáo (nếu có)
                string? maPhongDauTien = hd.MaDatPhongNavigation?.DatPhongChiTiets.FirstOrDefault()?.MaPhong;

                // Ghi sổ chi phí (truyền vào số dương)
                await GhiNhanChiPhiHoanTienAsync(maHoaDon, Math.Abs(soTien), nguoiThu, maPhongDauTien);
            }

            var lichSu = await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;
            var res = TinhToanHoaDonService.TinhToanToanBo(hd.TienPhong ?? 0, hd.TienDichVu ?? 0, hd.Vat ?? 0, hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0, hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "", hd.MaDatPhongNavigation?.TienCoc ?? 0, lichSu);

            if (res.ConLai <= 0) hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, res.TongThanhToan - res.ConLai, res.ConLai, "Thành công");
        }
        catch (Exception ex) { return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, ex.Message); }
        finally { _semaphore.Release(); }
    }

    public async Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon) ?? throw new Exception("Lỗi");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? TimeHelper.GetVietnamTime());

            
            if (hd.MaDatPhongNavigation != null)
            {
                foreach (var ct in hd.MaDatPhongNavigation.DatPhongChiTiets)
                {
                    var p = await _db.Phongs.FindAsync(ct.MaPhong);
                    if (p != null)
                    {
                        p.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
                    }
                }
            }

            hd.MaDatPhongNavigation!.TrangThai = DatPhongTrangThaiTexts.DaTraPhong;
            hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
        }
        finally { _semaphore.Release(); }
    }

    public async Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets).FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon) ?? throw new Exception("Null");
            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiemTraPhong);
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, 0, 0, "Xong");
        }
        finally { _semaphore.Release(); }
    }

    private async Task CapNhatTienPhongTheoThoiDiemTraPhongAsync(HoaDon hd, DateTime thoiDiem)
    {
        decimal tienPhong = 0;
        foreach (var ct in hd.MaDatPhongNavigation!.DatPhongChiTiets)
        {
            DateTime thoiDiemTinh = thoiDiem < ct.NgayTra ? thoiDiem : ct.NgayTra;
            int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, thoiDiemTinh);
            tienPhong += ct.DonGia * soDem;
            if (thoiDiem < ct.NgayTra) ct.NgayTra = thoiDiem;
        }
        var res = TinhToanHoaDonService.TinhToanToanBo(tienPhong, hd.TienDichVu ?? 0, hd.Vat ?? 0, 0, "", hd.MaDatPhongNavigation?.TienCoc ?? 0, 0);
        hd.TienPhong = res.TienPhong;
        hd.TongThanhToan = res.TongThanhToan;
        await _db.SaveChangesAsync();
    }

    public async Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon) { return 0; }
    public async Task HuyHoaDonAsync(string maHoaDon) { }


    private async Task GhiNhanChiPhiHoanTienAsync(string maHoaDon, decimal soTien, string maNhanVien, string? maPhong)
    {
        // 1. Tìm hoặc tạo mã Loại chi phí "Hoàn tiền"
        var loaiCp = await _db.LoaiChiPhis.FirstOrDefaultAsync(l => l.MaLoaiCp == "LCP007" || l.TenLoaiCp.Contains("Hoàn tiền"));
        if (loaiCp == null)
        {
            loaiCp = new LoaiChiPhi { MaLoaiCp = "LCP007", TenLoaiCp = "Hoàn tiền hóa đơn" };
            _db.LoaiChiPhis.Add(loaiCp);
            await _db.SaveChangesAsync();
        }

        // 2. Tạo mã phiếu chi tự động (VD: CP005)
        string nextCp = MaHelper.Next("CP", await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync());

        // 3. Đưa vào sổ Chi Phí
        _db.ChiPhis.Add(new ChiPhi
        {
            MaChiPhi = nextCp,
            MaLoaiCp = loaiCp.MaLoaiCp,
            MaNhanVien = maNhanVien,
            MaPhong = maPhong,
            TenChiPhi = $"Hoàn tiền thừa khách (HĐ: {maHoaDon})",
            SoTien = soTien,
            NgayChiPhi = TimeHelper.GetVietnamTime(),
            GhiChu = "Tự động ghi nhận khi hoàn tiền thừa cho khách."
        });
    }
}