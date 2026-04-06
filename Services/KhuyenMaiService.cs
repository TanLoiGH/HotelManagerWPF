using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhuyenMaiService
{
    private readonly QuanLyKhachSanContext _db;
    public KhuyenMaiService(QuanLyKhachSanContext db) => _db = db;

    public async Task<List<LoaiKhach>> LayLoaiKhachAsync()
    {
        return await _db.LoaiKhaches
            .AsNoTracking()
            .OrderBy(l => l.NguongTichLuy)
            .ToListAsync();
    }

    public async Task<List<KhuyenMai>> LayDanhSachAsync()
    {
        return await _db.KhuyenMais
            .AsNoTracking()
            .OrderByDescending(k => k.NgayBatDau)
            .ToListAsync();
    }

    public async Task<List<KhuyenMai>> LayKhuyenMaiConHieuLucTheoLoaiKhachAsync(string? maLoaiKhach)
    {
        var now = DateTime.Now;
        return await _db.KhuyenMais
            .AsNoTracking()
            .Where(k => k.IsActive == true
                        && k.NgayBatDau <= now
                        && k.NgayKetThuc >= now
                        && (maLoaiKhach == null || k.MaLoaiKhach == maLoaiKhach))
            .OrderByDescending(k => k.NgayBatDau)
            .ToListAsync();
    }

    public async Task TaoMoiAsync(
        string tenKhuyenMai,
        string loaiKhuyenMai,
        decimal giaTri,
        decimal giaTriToiThieu,
        DateTime ngayBatDau,
        DateTime ngayKetThuc,
        string? maLoaiKhach,
        bool isActive)
    {
        var lastMa = await _db.KhuyenMais
            .OrderByDescending(k => k.MaKhuyenMai)
            .Select(k => k.MaKhuyenMai)
            .FirstOrDefaultAsync();

        _db.KhuyenMais.Add(new KhuyenMai
        {
            MaKhuyenMai = MaHelper.Next("KM", lastMa),
            TenKhuyenMai = tenKhuyenMai,
            LoaiKhuyenMai = loaiKhuyenMai,
            GiaTriKm = giaTri,
            GiaTriToiThieu = giaTriToiThieu,
            NgayBatDau = ngayBatDau,
            NgayKetThuc = ngayKetThuc,
            MaLoaiKhach = maLoaiKhach,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(
        string maKhuyenMai,
        string tenKhuyenMai,
        string loaiKhuyenMai,
        decimal giaTri,
        decimal giaTriToiThieu,
        DateTime ngayBatDau,
        DateTime ngayKetThuc,
        string? maLoaiKhach,
        bool isActive)
    {
        var km = await _db.KhuyenMais.FindAsync(maKhuyenMai);
        if (km == null) return;

        km.TenKhuyenMai = tenKhuyenMai;
        km.LoaiKhuyenMai = loaiKhuyenMai;
        km.GiaTriKm = giaTri;
        km.GiaTriToiThieu = giaTriToiThieu;
        km.NgayBatDau = ngayBatDau;
        km.NgayKetThuc = ngayKetThuc;
        km.MaLoaiKhach = maLoaiKhach;
        km.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacTatAsync(string maKhuyenMai)
    {
        var km = await _db.KhuyenMais.FindAsync(maKhuyenMai);
        if (km == null) return false;

        bool daDung = await _db.HoaDons.AnyAsync(h => h.MaKhuyenMai == km.MaKhuyenMai);
        if (daDung)
        {
            km.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.KhuyenMais.Remove(km);
        await _db.SaveChangesAsync();
        return false;
    }
}
