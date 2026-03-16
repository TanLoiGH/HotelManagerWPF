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
                DonGia = dv.Gia,
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
        var tong = await _db.DichVuChiTiets
            .Where(d => d.MaHoaDon == maHoaDon)
            .SumAsync(d => (decimal?)((decimal)d.SoLuong * d.DonGia)) ?? 0;

        var hd = await _db.HoaDons.FindAsync(maHoaDon);
        if (hd == null) return;

        hd.TienDichVu = tong;
        hd.TongThanhToan = TinhTong(hd.TienPhong ?? 0, tong, hd.Vat ?? 10);
        await _db.SaveChangesAsync();
    }

    public async Task<List<DichVuViewModel>> GetAllDichVuAsync()
        => await _db.DichVus
            .Where(d => d.IsActive == true)
            .Select(d => new DichVuViewModel
            {
                MaDichVu = d.MaDichVu,
                TenDichVu = d.TenDichVu,
                Gia = d.Gia ?? 0,
                DonViTinh = d.DonViTinh ?? ""
            }).ToListAsync();

    private static decimal TinhTong(decimal tienPhong, decimal tienDv, decimal vat)
        => (tienPhong + tienDv) * (1 + vat / 100m);
}