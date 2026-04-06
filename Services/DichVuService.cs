using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DichVuService
{
    private readonly QuanLyKhachSanContext _db;

    public DichVuService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<List<DichVu>> LayDanhSachAsync()
    {
        return await _db.DichVus
            .AsNoTracking()
            .OrderBy(d => d.TenDichVu)
            .ToListAsync();
    }

    public async Task TaoMoiAsync(string tenDichVu, decimal gia, string? donViTinh, bool isActive)
    {
        var lastMa = await _db.DichVus
            .OrderByDescending(d => d.MaDichVu)
            .Select(d => d.MaDichVu)
            .FirstOrDefaultAsync();

        _db.DichVus.Add(new DichVu
        {
            MaDichVu = MaHelper.Next("DV", lastMa),
            TenDichVu = tenDichVu,
            Gia = gia,
            DonViTinh = donViTinh,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maDichVu, string tenDichVu, decimal gia, string? donViTinh, bool isActive)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu);
        if (dv == null) return;

        dv.TenDichVu = tenDichVu;
        dv.Gia = gia;
        dv.DonViTinh = donViTinh;
        dv.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacTatAsync(string maDichVu)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu);
        if (dv == null) return false;

        bool coGiaoDich = await _db.DichVuChiTiets.AnyAsync(d => d.MaDichVu == dv.MaDichVu);
        if (coGiaoDich)
        {
            dv.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.DichVus.Remove(dv);
        await _db.SaveChangesAsync();
        return false;
    }

    public async Task<List<DichVu>> GetDichVusAsync()
    {
        return await _db.DichVus.ToListAsync();
    }

    public async Task<List<DichVuChiTiet>> GetDichVuChiTietsForHoaDonAsync(string maHoaDon)
    {
        return await _db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.MaHoaDon == maHoaDon)
            .ToListAsync();
    }

    public async Task<List<DichVu>> GetAllDichVuAsync()
    {
        return await _db.DichVus.Where(d => d.IsActive == true).ToListAsync();
    }

    public async Task UpsertDichVuAsync(string maHoaDon, string maDatPhong, string maPhong, string maDichVu, int soLuong)
    {
        var existing = await _db.DichVuChiTiets.FirstOrDefaultAsync(d => d.MaHoaDon == maHoaDon && d.MaDichVu == maDichVu && d.MaPhong == maPhong);

        if (existing != null)
        {
            existing.SoLuong += soLuong;
        }
        else
        {
            var dv = await _db.DichVus.FindAsync(maDichVu) ?? throw new KeyNotFoundException("Dịch vụ không tồn tại");
            _db.DichVuChiTiets.Add(new DichVuChiTiet
            {
                MaHoaDon = maHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = maPhong,
                MaDichVu = maDichVu,
                SoLuong = soLuong,
                DonGia = dv.Gia ?? 0,
                NgaySuDung = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();

        var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        if (hd != null)
        {
            decimal tongDv = await _db.DichVuChiTiets
                .Where(d => d.MaHoaDon == maHoaDon)
                .SumAsync(d => (decimal?)d.SoLuong * d.DonGia) ?? 0;

            hd.TienDichVu = tongDv;

            decimal tienPhong = hd.TienPhong ?? 0;
            decimal vatPercent = hd.Vat ?? 10;
            decimal tienCoc = 0;

            if (!string.IsNullOrWhiteSpace(hd.MaDatPhong))
            {
                var dp = await _db.DatPhongs.FindAsync(hd.MaDatPhong);
                tienCoc = dp?.TienCoc ?? 0;
            }

            decimal kmPercent = 0;
            if (!string.IsNullOrWhiteSpace(hd.MaKhuyenMai) && hd.MaKhuyenMaiNavigation == null)
                hd.MaKhuyenMaiNavigation = await _db.KhuyenMais.FindAsync(hd.MaKhuyenMai);

            var km = hd.MaKhuyenMaiNavigation;
            if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
            {
                kmPercent = km.LoaiKhuyenMai == "Phần trăm"
                    ? km.GiaTriKm ?? 0
                    : tienPhong > 0 ? (km.GiaTriKm ?? 0) / tienPhong * 100 : 0;
            }

            hd.TongThanhToan = (((tienPhong * (1 - kmPercent / 100m)) + tongDv) * (1 + vatPercent / 100m)) - tienCoc;

            await _db.SaveChangesAsync();
        }
    }
}
