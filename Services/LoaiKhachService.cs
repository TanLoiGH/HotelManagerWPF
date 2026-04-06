using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record LoaiKhachItem(
    string MaLoaiKhach,
    string TenLoaiKhach,
    decimal NguongTichLuy,
    int SoKhach);

public class LoaiKhachService
{
    private readonly QuanLyKhachSanContext _db;
    public LoaiKhachService(QuanLyKhachSanContext db) => _db = db;

    public async Task<List<LoaiKhachItem>> LayDanhSachAsync()
    {
        return await _db.LoaiKhaches
            .AsNoTracking()
            .OrderBy(l => l.NguongTichLuy)
            .Select(l => new LoaiKhachItem(
                l.MaLoaiKhach,
                l.TenLoaiKhach ?? string.Empty,
                l.NguongTichLuy ?? 0,
                l.KhachHangs.Count()))
            .ToListAsync();
    }

    public async Task TaoMoiAsync(string tenLoaiKhach, decimal nguongTichLuy)
    {
        var lastMa = await _db.LoaiKhaches
            .OrderByDescending(l => l.MaLoaiKhach)
            .Select(l => l.MaLoaiKhach)
            .FirstOrDefaultAsync();

        _db.LoaiKhaches.Add(new LoaiKhach
        {
            MaLoaiKhach = MaHelper.Next("LK", lastMa),
            TenLoaiKhach = tenLoaiKhach,
            NguongTichLuy = nguongTichLuy,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maLoaiKhach, string tenLoaiKhach, decimal nguongTichLuy)
    {
        var lk = await _db.LoaiKhaches.FindAsync(maLoaiKhach);
        if (lk == null) return;

        lk.TenLoaiKhach = tenLoaiKhach;
        lk.NguongTichLuy = nguongTichLuy;
        await _db.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLoaiKhach)
    {
        var lk = await _db.LoaiKhaches.FindAsync(maLoaiKhach);
        if (lk == null) return;

        _db.LoaiKhaches.Remove(lk);
        await _db.SaveChangesAsync();
    }
}
