using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class HoaDonService
{
    private readonly QuanLyKhachSanContext _db;
    private readonly KhachHangService _khachHangSvc;

    public HoaDonService(QuanLyKhachSanContext db, KhachHangService khachHangSvc)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
    }

    public async Task<List<HoaDon>> GetHoaDonsAsync()
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
            .OrderByDescending(h => h.NgayLap)
            .ToListAsync();
    }

    public async Task<HoaDon> XuatHoaDonAsync(
        string maDatPhong, string maNhanVien,
        string? maKhuyenMai = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != "Đà hủy");

        if (hdExist)
            throw new InvalidOperationException("Đặt phòng này đã có hóa đơn active.");

        var chiTiets = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == maDatPhong)
            .ToListAsync();

        if (!chiTiets.Any())
            throw new InvalidOperationException("Không có phòng nào trong đặt phòng.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;

        decimal tienPhong = chiTiets
            .Sum(c => (decimal)(c.NgayTra - c.NgayNhan).TotalDays * c.DonGia);

        decimal kmPercent = 0;
        if (!string.IsNullOrWhiteSpace(maKhuyenMai))
        {
            var km = await _db.KhuyenMais.FindAsync(maKhuyenMai);
            if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
            {
                kmPercent = km.LoaiKhuyenMai == "Phần trăm"
                    ? km.GiaTriKm ?? 0
                    : tienPhong > 0 ? (km.GiaTriKm ?? 0) / tienPhong * 100 : 0;
            }
        }

        var lastMa = await _db.HoaDons
            .OrderByDescending(h => h.MaHoaDon)
            .Select(h => h.MaHoaDon)
            .FirstOrDefaultAsync();

        var newMaHd = MaHelper.Next("HD", lastMa);

        decimal vatPercent = 10;

        var hd = new HoaDon
        {
            MaHoaDon = newMaHd,
            MaDatPhong = maDatPhong,
            MaNhanVien = maNhanVien,
            NgayLap = DateTime.Now,
            TienPhong = tienPhong,
            TienDichVu = 0,
            Vat = vatPercent,
            MaKhuyenMai = maKhuyenMai,
            TongThanhToan = ((tienPhong * (1 - kmPercent / 100)) * (1 + vatPercent / 100m)) - tienCoc,
            TrangThai = "Chưa thanh toán"
        };

        _db.HoaDons.Add(hd);

        foreach (var ct in chiTiets)
        {
            var detail = new HoaDonChiTiet
            {
                MaHoaDon = hd.MaHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = Math.Max(1, (int)(ct.NgayTra - ct.NgayNhan).TotalDays)
            };

            _db.HoaDonChiTiets.Add(detail);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return hd;
    }

    public async Task<int> EnsureHoaDonChiTietAsync(string maHoaDon)
    {
        var hd = await _db.HoaDons
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

        if (string.IsNullOrWhiteSpace(hd.MaDatPhong))
            throw new InvalidOperationException("Hóa đơn không có mã đặt phòng.");

        var chiTietDatPhong = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == hd.MaDatPhong)
            .ToListAsync();

        if (!chiTietDatPhong.Any())
            throw new InvalidOperationException("Đặt phòng không có chi tiết phòng.");

        var existingKeys = await _db.HoaDonChiTiets
            .Where(c => c.MaHoaDon == maHoaDon)
            .Select(c => c.MaDatPhong + "|" + c.MaPhong)
            .ToListAsync();

        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = chiTietDatPhong
            .Where(ct => !existingSet.Contains(ct.MaDatPhong + "|" + ct.MaPhong))
            .Select(ct => new HoaDonChiTiet
            {
                MaHoaDon = maHoaDon,
                MaDatPhong = ct.MaDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = Math.Max(1, (int)(ct.NgayTra - ct.NgayNhan).TotalDays)
            })
            .ToList();

        if (toAdd.Count == 0)
            return 0;

        _db.HoaDonChiTiets.AddRange(toAdd);
        await _db.SaveChangesAsync();
        return toAdd.Count;
    }

    public async Task<bool> ThanhToanAsync(
        string maHoaDon,
        decimal soTien,
        string maPTTT,
        string nguoiThu,
        string loaiGiaoDich = "Thanh toán cuối",
        string? noiDung = null)
    {
        var thongTin = await ThanhToanVaTraKetQuaAsync(maHoaDon, soTien, maPTTT, nguoiThu, loaiGiaoDich, noiDung);
        return thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat;
    }

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(
        string maHoaDon,
        decimal soTien,
        string maPTTT,
        string nguoiThu,
        string loaiGiaoDich = "Thanh toán cuối",
        string? noiDung = null)
    {
        if (soTien <= 0)
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Tu choi: so tien khong hop le.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Khong tim thay hoa don.");

            var tongThanhToan = hd.TongThanhToan ?? 0;

            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (hd.TrangThai == "Đã thanh toán" || tongDaThu >= tongThanhToan)
            {
                // Nếu số tiền đã thu đủ nhưng trạng thái chưa đúng thì đồng bộ lại.
                if (tongDaThu >= tongThanhToan && hd.TrangThai != "Đã thanh toán")
                {
                    hd.TrangThai = "Đã thanh toán";
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return new ThongTinThanhToan(
                    KetQuaThanhToan.DaHoanTat,
                    tongDaThu,
                    tongThanhToan - tongDaThu,
                    "Hoa don da thanh toan.");
            }

            var lastTt = await _db.ThanhToans
                .OrderByDescending(t => t.MaThanhToan)
                .Select(t => t.MaThanhToan)
                .FirstOrDefaultAsync();

            _db.ThanhToans.Add(new ThanhToan
            {
                MaThanhToan = MaHelper.Next("TT", lastTt),
                MaHoaDon = maHoaDon,
                MaPttt = maPTTT,
                SoTien = soTien,
                LoaiGiaoDich = loaiGiaoDich,
                NgayThanhToan = DateTime.Now,
                NguoiThu = nguoiThu,
                NoiDung = noiDung
            });

            tongDaThu += soTien;
            bool daThuDu = tongDaThu >= tongThanhToan;

            if (daThuDu)
                hd.TrangThai = "Đã thanh toán";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var conLai = tongThanhToan - tongDaThu;
            return daThuDu
                ? new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThu, conLai, "Thanh toan hoan tat.")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThu, conLai, "Da ghi nhan thanh toan, khach chua thanh toan du.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Loi thanh toan: {ex.Message}");
        }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Khong tim thay hoa don.");

            var tongThanhToan = hd.TongThanhToan ?? 0;
            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu >= tongThanhToan && hd.TrangThai != "Đã thanh toán")
            {
                hd.TrangThai = "Đã thanh toán";
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            var conLai = tongThanhToan - tongDaThu;
            return tongDaThu >= tongThanhToan
                ? new ThongTinThanhToan(KetQuaThanhToan.DaHoanTat, tongDaThu, conLai, "Hoa don da du tien.")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThu, conLai, "Hoa don chua du tien.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Loi dong bo trang thai: {ex.Message}");
        }
    }

    // Cap nhat tong tien khi khach tra som (khong check out, chi tinh lai so dem + tong tien).
    // Neu sau khi tinh lai tongDaThu >= TongThanhToan thi tu dong dong bo TrangThai = "Đã thanh toán".
    public async Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Khong tim thay hoa don.");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiemTraPhong);

            var tongThanhToan = hd.TongThanhToan ?? 0;
            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu >= tongThanhToan && hd.TrangThai != "Đã thanh toán")
                hd.TrangThai = "Đã thanh toán";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var conLai = tongThanhToan - tongDaThu;
            return tongDaThu >= tongThanhToan
                ? new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThu, conLai, "Da cap nhat tong tien va hoa don da du tien.")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThu, conLai, "Da cap nhat tong tien, hoa don chua du tien.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Loi cap nhat tong tien: {ex.Message}");
        }
    }

    public async Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

            if (hd.MaDatPhongNavigation == null || string.IsNullOrWhiteSpace(hd.MaDatPhong))
                throw new InvalidOperationException("Hóa đơn không gắn với đặt phòng.");

            var dp = hd.MaDatPhongNavigation;
            if (dp.TrangThai == "Đã trả phòng")
            {
                await tx.RollbackAsync();
                return;
            }

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? DateTime.Now);

            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu < (hd.TongThanhToan ?? 0))
                throw new InvalidOperationException("Chưa thanh toán đủ, không thể trả phòng.");

            hd.TrangThai = "Đã thanh toán";

            foreach (var ct in dp.DatPhongChiTiets ?? [])
            {
                var p = await _db.Phongs.FindAsync(ct.MaPhong);
                if (p != null) p.MaTrangThaiPhong = "PTT03";
            }

            dp.TrangThai = "Đã trả phòng";

            if (!string.IsNullOrWhiteSpace(dp.MaKhachHang))
                await _khachHangSvc.NangHangAsync(dp.MaKhachHang, hd.TongThanhToan ?? 0);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception)
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task CapNhatTienPhongTheoThoiDiemTraPhongAsync(HoaDon hd, DateTime thoiDiem)
    {
        if (string.IsNullOrWhiteSpace(hd.MaDatPhong) || hd.MaDatPhongNavigation == null)
            return;

        var dp = hd.MaDatPhongNavigation;
        var chiTiets = dp.DatPhongChiTiets?.ToList() ?? new List<DatPhongChiTiet>();
        if (chiTiets.Count == 0) return;

        DateTime checkout = thoiDiem;

        foreach (var ct in chiTiets)
        {
            if (checkout != ct.NgayTra)
                ct.NgayTra = checkout;
        }

        decimal tienPhong = 0;
        foreach (var ct in chiTiets)
        {
            int soDem = Math.Max(1, (checkout.Date - ct.NgayNhan.Date).Days);
            tienPhong += ct.DonGia * soDem;

            var hdct = hd.HoaDonChiTiets.FirstOrDefault(x =>
                x.MaHoaDon == hd.MaHoaDon &&
                x.MaDatPhong == ct.MaDatPhong &&
                x.MaPhong == ct.MaPhong);

            if (hdct != null)
                hdct.SoDem = soDem;
        }

        hd.TienPhong = tienPhong;

        decimal tienDv = hd.TienDichVu ?? 0;
        decimal vatPercent = hd.Vat ?? 10;
        decimal tienCoc = dp.TienCoc ?? 0;

        decimal kmPercent = 0;
        var km = hd.MaKhuyenMaiNavigation;
        if (km == null && !string.IsNullOrWhiteSpace(hd.MaKhuyenMai))
            km = await _db.KhuyenMais.FindAsync(hd.MaKhuyenMai);

        if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
        {
            kmPercent = km.LoaiKhuyenMai == "Phần trăm"
                ? km.GiaTriKm ?? 0
                : tienPhong > 0 ? (km.GiaTriKm ?? 0) / tienPhong * 100 : 0;
        }

        hd.TongThanhToan = (((tienPhong * (1 - kmPercent / 100m)) + tienDv) * (1 + vatPercent / 100m)) - tienCoc;

        await _db.SaveChangesAsync();
    }

    public async Task<List<PhuongThucThanhToanDto>> GetPTTTAsync()
    {
        return await _db.PhuongThucThanhToans
            .Select(p => new PhuongThucThanhToanDto { MaPTTT = p.MaPttt, TenPhuongThuc = p.TenPhuongThuc })
            .ToListAsync();
    }

    public async Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
    }

    public async Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon)
    {
        return await _db.ThanhToans
            .AsNoTracking()
            .Include(t => t.MaPtttNavigation)
            .Where(t => t.MaHoaDon == maHoaDon)
            .OrderBy(t => t.NgayThanhToan)
            .ToListAsync();
    }

    public async Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon)
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

    public async Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaKhuyenMaiNavigation)
            .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
            .Include(h => h.MaNhanVienNavigation)
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
    }
}

public enum KetQuaThanhToan
{
    GhiNhanChuaDu,
    HoanTat,
    DaHoanTat,
    TuChoi
}

public sealed record ThongTinThanhToan(
    KetQuaThanhToan KetQua,
    decimal TongDaThu,
    decimal ConLai,
    string ThongDiep);
