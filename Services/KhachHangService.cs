using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhachHangService
{
    private readonly QuanLyKhachSanContext _db;
    public KhachHangService(QuanLyKhachSanContext db) => _db = db;

    public async Task<KhachHang> TimHoacTaoAsync(
        string tenKhachHang, string? dienThoai, string? cccd, string? email = null)
    {
        KhachHang? existing = null;
        if (!string.IsNullOrWhiteSpace(cccd))
            existing = await _db.KhachHangs.FirstOrDefaultAsync(k => k.Cccd == cccd);
        else if (!string.IsNullOrWhiteSpace(dienThoai))
            existing = await _db.KhachHangs.FirstOrDefaultAsync(k => k.DienThoai == dienThoai);

        if (existing != null) return existing;

        var lastMa = await _db.KhachHangs
            .OrderByDescending(k => k.MaKhachHang)
            .Select(k => k.MaKhachHang)
            .FirstOrDefaultAsync();

        var kh = new KhachHang
        {
            MaKhachHang = MaHelper.Next("KH", lastMa),
            TenKhachHang = tenKhachHang,
            DienThoai = dienThoai,
            Cccd = cccd,
            Email = email,
            MaLoaiKhach = "LK001",
            TongTichLuy = 0
        };
        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();
        return kh;
    }

    public async Task<List<KhachHangViewModel>> GetListAsync(string? keyword = null)
    {
        var q = _db.KhachHangs
            .Include(k => k.MaLoaiKhachNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            q = q.Where(k =>
                k.TenKhachHang.Contains(keyword) ||
                (k.DienThoai != null && k.DienThoai.Contains(keyword)) ||
                (k.Cccd != null && k.Cccd.Contains(keyword)));

        return await q.Select(k => new KhachHangViewModel
        {
            MaKhachHang = k.MaKhachHang,
            TenKhachHang = k.TenKhachHang,
            DienThoai = k.DienThoai ?? "",
            Cccd = k.Cccd ?? "",
            TenLoaiKhach = k.MaLoaiKhachNavigation != null
                           ? k.MaLoaiKhachNavigation.TenLoaiKhach ?? "" : "",
            TongTichLuy = k.TongTichLuy ?? 0
        }).ToListAsync();
    }

    internal async Task NangHangAsync(string maKhachHang, decimal soTienMoi)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang);
        if (kh == null) return;

        kh.TongTichLuy = (kh.TongTichLuy ?? 0) + soTienMoi;

        var loaiMoi = await _db.LoaiKhaches
            .Where(l => l.NguongTichLuy <= kh.TongTichLuy)
            .OrderByDescending(l => l.NguongTichLuy)
            .FirstOrDefaultAsync();

        if (loaiMoi != null && loaiMoi.MaLoaiKhach != kh.MaLoaiKhach)
            kh.MaLoaiKhach = loaiMoi.MaLoaiKhach;

        // Lưu cập nhật hạng/tích lũy
        await _db.SaveChangesAsync();
    }
}




