using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DichVuService
{
    private readonly QuanLyKhachSanContext _db;
    public DichVuService(QuanLyKhachSanContext db) => _db = db;

    public async Task UpsertDichVuAsync(
        string maHoaDon, string maDatPhong, string maPhong,
        string maDichVu, int delta = 1)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu)
            ?? throw new KeyNotFoundException($"Dịch vụ {maDichVu} không tồn tại");

        var item = await _db.DichVuChiTiets
            .FindAsync(maHoaDon, maDatPhong, maPhong, maDichVu);

        if (item == null)
        {
            if (delta <= 0)
                throw new ArgumentException("Không thể thêm số lượng âm cho dịch vụ mới");

            _db.DichVuChiTiets.Add(new DichVuChiTiet
            {
                MaHoaDon = maHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = maPhong,
                MaDichVu = maDichVu,
                SoLuong = delta,
                DonGia = dv.Gia ?? 0,
                NgaySuDung = DateTime.Now
            });
        }
        else
        {
            item.SoLuong += delta;
            if (item.SoLuong <= 0)
                _db.DichVuChiTiets.Remove(item);
        }

        await _db.SaveChangesAsync();
        await SyncTienDichVuAsync(maHoaDon);
    }

    private async Task SyncTienDichVuAsync(string maHoaDon)
    {
        var tongDv = await _db.DichVuChiTiets
            .Where(d => d.MaHoaDon == maHoaDon)
            .SumAsync(d => (decimal?)((decimal)d.SoLuong * d.DonGia)) ?? 0;

        var hd = await _db.HoaDons
            .Include(h => h.MaKhuyenMaiNavigation)
            .Include(h => h.MaDatPhongNavigation) // Thêm Include để lấy tiền cọc
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

        if (hd == null) return;

        hd.TienDichVu = tongDv;

        decimal tienPhong = hd.TienPhong ?? 0;
        decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        decimal kmPercent = 0;

        if (hd.MaKhuyenMaiNavigation is { IsActive: true })
        {
            var km = hd.MaKhuyenMaiNavigation;
            kmPercent = km.LoaiKhuyenMai == "Phần trăm"
                ? km.GiaTriKm ?? 0
                : tienPhong > 0 ? (km.GiaTriKm ?? 0) / tienPhong * 100 : 0;
        }

        // Re-calculate: ((Room * (1-KM%) * (1+VAT%)) + Services * (1+VAT%)) - Deposit
        // Đơn giản hơn: ((Tiền phòng sau KM + Tiền DV) * (1+VAT)) - Tiền cọc
        hd.TongThanhToan = (((tienPhong * (1 - kmPercent / 100)) + tongDv) * (1 + (hd.Vat ?? 10) / 100m)) - tienCoc;

        await _db.SaveChangesAsync();
    }

    public async Task<List<DichVuViewModel>> GetAllDichVuAsync()
        => await _db.DichVus
            .Select(d => new DichVuViewModel
            {
                MaDichVu = d.MaDichVu,
                TenDichVu = d.TenDichVu,
                Gia = d.Gia ?? 0,
                DonViTinh = d.DonViTinh ?? "",
                IsActive = d.IsActive ?? true
            }).ToListAsync();
}



