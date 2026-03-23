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

    public async Task<HoaDon> XuatHoaDonAsync(
        string maDatPhong, string maNhanVien,
        string? maKhuyenMai = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != "Đã hủy");
        if (hdExist)
            throw new InvalidOperationException("Đặt phòng này đã có hóa đơn active.");

        var chiTiets = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == maDatPhong)
            .ToListAsync();

        if (!chiTiets.Any())
            throw new InvalidOperationException("Không có phòng nào trong đặt phòng.");

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

        var hd = new HoaDon
        {
            MaHoaDon = MaHelper.Next("HD", lastMa),
            MaDatPhong = maDatPhong,
            MaNhanVien = maNhanVien,
            NgayLap = DateTime.Now,
            TienPhong = tienPhong,
            TienDichVu = 0,
            Vat = 10,
            MaKhuyenMai = maKhuyenMai,
            TongThanhToan = tienPhong * (1 + 10m / 100) * (1 - kmPercent / 100),
            TrangThai = "Chưa thanh toán"
        };
        _db.HoaDons.Add(hd);

        foreach (var ct in chiTiets)
        {
            _db.HoaDonChiTiets.Add(new HoaDonChiTiet
            {
                MaHoaDon = hd.MaHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = Math.Max(1, (int)(ct.NgayTra - ct.NgayNhan).TotalDays)
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return hd;
    }

    public async Task<bool> ThanhToanAsync(
        string maHoaDon,
        decimal soTien,
        string maPTTT,
        string nguoiThu,
        string loaiGiaoDich = "Thanh toán cuối",
        string? noiDung = null)
    {
        if (soTien <= 0)
            throw new ArgumentException("Số tiền thanh toán phải lớn hơn 0", nameof(soTien));

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (hd.TrangThai == "Đã thanh toán" || tongDaThu >= (hd.TongThanhToan ?? 0))
            {
                await tx.RollbackAsync();
                return true;
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
            bool daThuDu = tongDaThu >= (hd.TongThanhToan ?? 0);

            if (daThuDu)
            {
                hd.TrangThai = "Đã thanh toán";

                foreach (var ct in hd.MaDatPhongNavigation?.DatPhongChiTiets ?? [])
                {
                    var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                    if (phong != null) phong.MaTrangThaiPhong = "PTT03";
                }

                if (hd.MaDatPhongNavigation != null)
                    hd.MaDatPhongNavigation.TrangThai = "Đã trả phòng";

                var maKhach = hd.MaDatPhongNavigation?.MaKhachHang;
                if (!string.IsNullOrWhiteSpace(maKhach))
                    await _khachHangSvc.NangHangAsync(maKhach, hd.TongThanhToan ?? 0);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return daThuDu;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PhuongThucThanhToanDto>> GetPTTTAsync()
        => await _db.PhuongThucThanhToans
            .Select(p => new PhuongThucThanhToanDto
            {
                MaPTTT = p.MaPttt,
                TenPhuongThuc = p.TenPhuongThuc
            }).ToListAsync();
}





