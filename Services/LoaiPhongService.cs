using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record LoaiPhongItem(
    string MaLoaiPhong,
    string TenLoaiPhong,
    int? SoNguoiToiDa,
    decimal GiaPhong,
    int SoPhong);

public class LoaiPhongService
{
    private readonly QuanLyKhachSanContext _db;
    public LoaiPhongService(QuanLyKhachSanContext db) => _db = db;

    public async Task<List<LoaiPhongItem>> LayDanhSachAsync()
    {
        return await _db.LoaiPhongs
            .AsNoTracking()
            .OrderBy(lp => lp.MaLoaiPhong)
            .Select(lp => new LoaiPhongItem(
                lp.MaLoaiPhong,
                lp.TenLoaiPhong ?? string.Empty,
                lp.SoNguoiToiDa,
                lp.GiaPhong,
                lp.Phongs.Count()
            ))
            .ToListAsync();
    }

    public async Task TaoMoiAsync(string tenLoaiPhong, int? soNguoiToiDa, decimal giaPhong)
    {
        var lastMa = await _db.LoaiPhongs
            .OrderByDescending(lp => lp.MaLoaiPhong)
            .Select(lp => lp.MaLoaiPhong)
            .FirstOrDefaultAsync();

        _db.LoaiPhongs.Add(new LoaiPhong
        {
            MaLoaiPhong = MaHelper.Next("LP", lastMa),
            TenLoaiPhong = tenLoaiPhong,
            SoNguoiToiDa = soNguoiToiDa,
            GiaPhong = giaPhong,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maLoaiPhong, string tenLoaiPhong, int? soNguoiToiDa, decimal giaPhong)
    {
        var lp = await _db.LoaiPhongs.FindAsync(maLoaiPhong);
        if (lp == null) return;

        lp.TenLoaiPhong = tenLoaiPhong;
        lp.SoNguoiToiDa = soNguoiToiDa;
        lp.GiaPhong = giaPhong;
        await _db.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLoaiPhong)
    {
        var lp = await _db.LoaiPhongs.FindAsync(maLoaiPhong);
        if (lp == null) return;

        _db.LoaiPhongs.Remove(lp);
        await _db.SaveChangesAsync();
    }
}
