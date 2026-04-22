using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhachHangService : IKhachHangService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom Magic String vào hằng số để dễ quản lý
    private const string PREFIX_KHACH_HANG = "KH";

    public KhachHangService(QuanLyKhachSanContext db) => _db = db;

    #region QUẢN LÝ DANH MỤC KHÁCH HÀNG

    public async Task<List<LoaiKhach>> LayLoaiKhachAsync()
    {
        return await _db.LoaiKhaches
            .AsNoTracking()
            .OrderBy(l => l.NguongTichLuy)
            .ToListAsync();
    }

    public async Task<KhachHang?> LayTheoMaAsync(string maKhachHang)
    {
        if (string.IsNullOrWhiteSpace(maKhachHang)) return null;

        return await _db.KhachHangs
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.MaKhachHang == maKhachHang);
    }

    public async Task TaoMoiAsync(
        string tenKhachHang, string? dienThoai, string? cccd,
        string? email, string? diaChi, string? passport,
        string visa, string quocTich, string? maLoaiKhach)
    {
        var lastMa = await _db.KhachHangs
            .OrderByDescending(k => k.MaKhachHang)
            .Select(k => k.MaKhachHang)
            .FirstOrDefaultAsync();

        _db.KhachHangs.Add(new KhachHang
        {
            MaKhachHang = MaHelper.Next(PREFIX_KHACH_HANG, lastMa),
            TenKhachHang = tenKhachHang,
            DienThoai = dienThoai?.Trim(),
            Cccd = cccd?.Trim(),
            Email = email?.Trim(),
            DiaChi = diaChi?.Trim(),
            Passport = passport?.Trim(),
            Visa = visa?.Trim(),
            QuocTich = quocTich?.Trim(),
            MaLoaiKhach = maLoaiKhach,
            TongTichLuy = 0
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(
        string maKhachHang, string tenKhachHang, string? dienThoai,
        string? cccd, string? email, string? diaChi,
        string? passport, string? visa, string? quocTich, string? maLoaiKhach)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang)
                 ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng mã {maKhachHang} để cập nhật.");

        kh.TenKhachHang = tenKhachHang;
        kh.DienThoai = dienThoai?.Trim();
        kh.Cccd = cccd?.Trim();
        kh.Email = email?.Trim();
        kh.DiaChi = diaChi?.Trim();
        kh.Passport = passport?.Trim();
        kh.Visa = visa?.Trim();
        kh.QuocTich = quocTich?.Trim();
        kh.MaLoaiKhach = maLoaiKhach;

        await _db.SaveChangesAsync();
    }

    public async Task XoaAsync(string maKhachHang)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang)
                 ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng mã {maKhachHang} để xóa.");

        _db.KhachHangs.Remove(kh);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region NGHIỆP VỤ TÍCH LŨY & ĐẶT PHÒNG

    public async Task<bool> CoLichSuDatPhongAsync(string maKhachHang)
    {
        if (string.IsNullOrWhiteSpace(maKhachHang)) return false;

        return await _db.DatPhongs
            .AsNoTracking()
            .AnyAsync(d => d.MaKhachHang == maKhachHang);
    }

    public async Task<KhachHang> TimHoacTaoAsync(
        string tenKhachHang, string? dienThoai, string? cccd, string? email,
        string? maLoaiKhachMacDinh, string? diaChi = null, string? passport = null,
        string? visa = null, string? quocTich = null)
    {
        // Senior Refactor: Trim chuỗi trước khi kiểm tra độ dài để tránh lỗi khoảng trắng thừa
        dienThoai = dienThoai?.Trim();
        cccd = cccd?.Trim();

        if (!string.IsNullOrWhiteSpace(dienThoai) && dienThoai.Length > 15) dienThoai = dienThoai.Substring(0, 15);
        if (!string.IsNullOrWhiteSpace(cccd) && cccd.Length > 12) cccd = cccd.Substring(0, 12);

        KhachHang? kh = null;
        if (!string.IsNullOrWhiteSpace(cccd))
            kh = await _db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Cccd == cccd);
        else if (!string.IsNullOrWhiteSpace(dienThoai))
            kh = await _db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.DienThoai == dienThoai);

        if (kh != null) return kh;

        // Nếu không tìm thấy -> Tạo mới
        var maKhachCuoi = await _db.KhachHangs.AsNoTracking()
            .OrderByDescending(k => k.MaKhachHang)
            .Select(k => k.MaKhachHang).FirstOrDefaultAsync();

        var tongTichLuyBanDau = 0m;
        var maLoaiKhach = await TinhHangAsync(_db, tongTichLuyBanDau) ?? maLoaiKhachMacDinh;

        bool loaiKhachTonTai = await _db.LoaiKhaches.AnyAsync(l => l.MaLoaiKhach == maLoaiKhach);
        if (!loaiKhachTonTai)
        {
            maLoaiKhach = await _db.LoaiKhaches.Select(l => l.MaLoaiKhach).FirstOrDefaultAsync();
        }

        kh = new KhachHang
        {
            MaKhachHang = MaHelper.Next(PREFIX_KHACH_HANG, maKhachCuoi),
            TenKhachHang = tenKhachHang,
            DienThoai = dienThoai,
            Cccd = cccd,
            Email = email?.Trim(),
            DiaChi = diaChi?.Trim(),
            TongTichLuy = tongTichLuyBanDau,
            MaLoaiKhach = maLoaiKhach,
            Passport = passport?.Trim(),
            Visa = visa?.Trim(),
            QuocTich = quocTich?.Trim()
        };

        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();

        // Nhả tracking để tránh xung đột DbContext ở các layer khác
        _db.Entry(kh).State = EntityState.Detached;

        return kh;
    }

    private async Task<string?> TinhHangAsync(QuanLyKhachSanContext db, decimal tongTichLuy)
    {
        var loai = await db.LoaiKhaches
            .Where(l => l.NguongTichLuy <= tongTichLuy)
            .OrderByDescending(l => l.NguongTichLuy)
            .FirstOrDefaultAsync();

        return loai?.MaLoaiKhach;
    }

    public async Task NangHangAsync(string maKhachHang, decimal soTienMoi)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang)
                 ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng mã {maKhachHang} để nâng hạng.");

        kh.TongTichLuy = (kh.TongTichLuy ?? 0) + soTienMoi;

        var maLoaiMoi = await TinhHangAsync(_db, kh.TongTichLuy ?? 0);

        if (maLoaiMoi != null && maLoaiMoi != kh.MaLoaiKhach)
            kh.MaLoaiKhach = maLoaiMoi;

        await _db.SaveChangesAsync();
    }

    #endregion

    #region TÌM KIẾM & HIỂN THỊ

    public async Task<List<KhachHangViewModel>> GetListAsync(string? keyword = null)
    {
        var q = _db.KhachHangs
            .AsNoTracking()
            .Include(k => k.MaLoaiKhachNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            q = q.Where(k =>
                k.TenKhachHang.Contains(keyword) ||
                (k.DienThoai != null && k.DienThoai.Contains(keyword)) ||
                (k.Cccd != null && k.Cccd.Contains(keyword)));
        }

        return await q.Select(k => new KhachHangViewModel
        {
            MaKhachHang = k.MaKhachHang,
            // Senior Fix: Thay vì "1", trả về "Khách vãng lai" nếu không có tên
            TenKhachHang = string.IsNullOrEmpty(k.TenKhachHang) ? "Khách vãng lai" : k.TenKhachHang,
            DienThoai = k.DienThoai ?? "",
            Email = k.Email ?? "",
            Cccd = k.Cccd ?? "",
            DiaChi = k.DiaChi ?? "",
            Passport = k.Passport,
            Visa = k.Visa,
            QuocTich = k.QuocTich,
            TenLoaiKhach = k.MaLoaiKhachNavigation != null ? (k.MaLoaiKhachNavigation.TenLoaiKhach ?? "") : "",
            TongTichLuy = k.TongTichLuy ?? 0
        }).ToListAsync();
    }

    public async Task<List<KhachHang>> SearchKhachHangAsync(string keyword, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<KhachHang>();

        keyword = keyword.Trim(); // Trim để chống tìm kiếm bằng dấu cách gây tốn tải DB

        return await _db.KhachHangs
            .AsNoTracking()
            .Include(k => k.MaLoaiKhachNavigation)
            .Where(k => k.TenKhachHang.Contains(keyword) ||
                        (k.DienThoai != null && k.DienThoai.Contains(keyword)) ||
                        (k.Cccd != null && k.Cccd.Contains(keyword)))
            .Take(limit)
            .ToListAsync();
    }

    #endregion
}