using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record LoaiKhachItem(
    string MaLoaiKhach,
    string TenLoaiKhach,
    decimal NguongTichLuy,
    int SoKhach);

public class LoaiKhachService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom Magic String vào hằng số
    private const string PREFIX_LOAI_KHACH = "LK";

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
        // Senior Fix: Ngăn chặn lưu dữ liệu rác
        if (string.IsNullOrWhiteSpace(tenLoaiKhach))
            throw new ArgumentException("Tên loại khách không được để trống.");

        var lastMa = await _db.LoaiKhaches
            .OrderByDescending(l => l.MaLoaiKhach)
            .Select(l => l.MaLoaiKhach)
            .FirstOrDefaultAsync();

        _db.LoaiKhaches.Add(new LoaiKhach
        {
            MaLoaiKhach = MaHelper.Next(PREFIX_LOAI_KHACH, lastMa),
            TenLoaiKhach = tenLoaiKhach.Trim(),
            NguongTichLuy = nguongTichLuy,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maLoaiKhach, string tenLoaiKhach, decimal nguongTichLuy)
    {
        if (string.IsNullOrWhiteSpace(tenLoaiKhach))
            throw new ArgumentException("Tên loại khách không được để trống.");

        // Senior Fix: Ném lỗi thay vì return im lặng
        var lk = await _db.LoaiKhaches.FindAsync(maLoaiKhach)
                 ?? throw new KeyNotFoundException($"Không tìm thấy loại khách mã {maLoaiKhach}.");

        lk.TenLoaiKhach = tenLoaiKhach.Trim();
        lk.NguongTichLuy = nguongTichLuy;

        await _db.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLoaiKhach)
    {
        var lk = await _db.LoaiKhaches.FindAsync(maLoaiKhach)
                 ?? throw new KeyNotFoundException($"Không tìm thấy loại khách mã {maLoaiKhach} để xóa.");

        // Senior Fix: Kiểm tra an toàn dữ liệu trước khi Hard Delete
        // Nếu loại khách này đã từng được gắn cho khách hàng hoặc dùng trong khuyến mãi, KHÔNG cho phép xóa.
        bool dangSuDung = await _db.KhachHangs.AnyAsync(k => k.MaLoaiKhach == maLoaiKhach) ||
                          await _db.KhuyenMais.AnyAsync(km => km.MaLoaiKhach == maLoaiKhach);

        if (dangSuDung)
        {
            throw new InvalidOperationException(
                $"Không thể xóa loại khách '{lk.TenLoaiKhach}' vì đang có Khách hàng hoặc Khuyến mãi phụ thuộc vào loại này.");
        }

        _db.LoaiKhaches.Remove(lk);
        await _db.SaveChangesAsync();
    }
}