using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhachHangService : IKhachHangService
{
    private readonly QuanLyKhachSanContext _db;
    public KhachHangService(QuanLyKhachSanContext db) => _db = db;

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
        string tenKhachHang,
        string? dienThoai,
        string? cccd,
        string? email,
        string? diaChi,
        string? maLoaiKhach)
    {
        var lastMa = await _db.KhachHangs
            .OrderByDescending(k => k.MaKhachHang)
            .Select(k => k.MaKhachHang)
            .FirstOrDefaultAsync();

        _db.KhachHangs.Add(new KhachHang
        {
            MaKhachHang = MaHelper.Next("KH", lastMa),
            TenKhachHang = tenKhachHang,
            DienThoai = dienThoai,
            Cccd = cccd,
            Email = email,
            DiaChi = diaChi,
            MaLoaiKhach = maLoaiKhach,
            TongTichLuy = 0
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(
        string maKhachHang,
        string tenKhachHang,
        string? dienThoai,
        string? cccd,
        string? email,
        string? diaChi,
        string? maLoaiKhach)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang);
        if (kh == null) return;

        kh.TenKhachHang = tenKhachHang;
        kh.DienThoai = dienThoai;
        kh.Cccd = cccd;
        kh.Email = email;
        kh.DiaChi = diaChi;
        kh.MaLoaiKhach = maLoaiKhach;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> CoLichSuDatPhongAsync(string maKhachHang)
    {
        if (string.IsNullOrWhiteSpace(maKhachHang)) return false;
        return await _db.DatPhongs
            .AsNoTracking()
            .AnyAsync(d => d.MaKhachHang == maKhachHang);
    }

    public async Task XoaAsync(string maKhachHang)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang);
        if (kh == null) return;

        _db.KhachHangs.Remove(kh);
        await _db.SaveChangesAsync();
    }

    public async Task<KhachHang> TimHoacTaoAsync(
        string tenKhachHang, string? dienThoai, string? cccd, string? email,
        string? maLoaiKhachMacDinh, string? diaChi = null, string? passport = null,
        string? visa = null, string? quocTich = null)
    {
        // Tránh lỗi tràn độ dài cột DB
        if (!string.IsNullOrWhiteSpace(dienThoai) && dienThoai.Length > 15) dienThoai = dienThoai.Substring(0, 15);
        if (!string.IsNullOrWhiteSpace(cccd) && cccd.Length > 12) cccd = cccd.Substring(0, 12);

        // Dùng AsNoTracking để tránh EF khóa object
        KhachHang? kh = null;
        if (!string.IsNullOrWhiteSpace(cccd))
            kh = await _db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.Cccd == cccd);
        else if (!string.IsNullOrWhiteSpace(dienThoai))
            kh = await _db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.DienThoai == dienThoai);

        if (kh != null) return kh;

        var maKhachCuoi = await _db.KhachHangs.AsNoTracking()
            .OrderByDescending(k => k.MaKhachHang)
            .Select(k => k.MaKhachHang).FirstOrDefaultAsync();

        var tongTichLuyBanDau = 0m;
        var maLoaiKhach = await TinhHangAsync(_db, tongTichLuyBanDau) ?? maLoaiKhachMacDinh;

        bool loaiKhachTonTai = await _db.LoaiKhaches.AnyAsync(l => l.MaLoaiKhach == maLoaiKhach);
        if (!loaiKhachTonTai)
        {
            // Nếu mã không tồn tại, tự động lấy mã loại khách đầu tiên có trong bảng
            maLoaiKhach = await _db.LoaiKhaches.Select(l => l.MaLoaiKhach).FirstOrDefaultAsync();
        }

        kh = new KhachHang
        {
            MaKhachHang = MaHelper.Next("KH", maKhachCuoi),
            TenKhachHang = tenKhachHang,
            DienThoai = dienThoai,
            Cccd = cccd,
            Email = email,
            DiaChi = diaChi,
            TongTichLuy = tongTichLuyBanDau,
            MaLoaiKhach = maLoaiKhach,
            Passport = passport,
            Visa = visa,
            QuocTich = quocTich
        };

        _db.KhachHangs.Add(kh);
        await _db.SaveChangesAsync();
        _db.Entry(kh).State = EntityState.Detached; // Nhả tracking ngay sau khi save

        return kh;
    }

    // Tính hạng khách theo tổng tích lũy hiện tại.
    private async Task<string?> TinhHangAsync(QuanLyKhachSanContext db, decimal tongTichLuy)
    {
        var loai = await db.LoaiKhaches
            .Where(l => l.NguongTichLuy <= tongTichLuy)
            .OrderByDescending(l => l.NguongTichLuy)
            .FirstOrDefaultAsync();

        return loai?.MaLoaiKhach;
    }

    // Cộng điểm tích lũy và tự động nâng hạng nếu đạt ngưỡng.
    internal async Task NangHangAsync(string maKhachHang, decimal soTienMoi)
    {
        var kh = await _db.KhachHangs.FindAsync(maKhachHang);
        if (kh == null) return;

        kh.TongTichLuy = (kh.TongTichLuy ?? 0) + soTienMoi;

        var maLoaiMoi = await TinhHangAsync(_db, kh.TongTichLuy ?? 0);

        if (maLoaiMoi != null && maLoaiMoi != kh.MaLoaiKhach)
            kh.MaLoaiKhach = maLoaiMoi;

        await _db.SaveChangesAsync();
    }

    // Lấy danh sách khách để hiển thị trang quản lý, có hỗ trợ lọc theo từ khóa.
    public async Task<List<KhachHangViewModel>> GetListAsync(string? keyword = null)
    {
        var q = _db.KhachHangs
            .AsNoTracking()
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
            Email = k.Email ?? "",
            Cccd = k.Cccd ?? "",
            DiaChi = k.DiaChi ?? "",
            TenLoaiKhach = k.MaLoaiKhachNavigation != null
                           ? k.MaLoaiKhachNavigation.TenLoaiKhach ?? "" : "",
            TongTichLuy = k.TongTichLuy ?? 0
        }).ToListAsync();
    }

    // Tìm nhanh khách hàng phục vụ thao tác đặt phòng.
    public async Task<List<KhachHang>> SearchKhachHangAsync(string keyword, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<KhachHang>();

        return await _db.KhachHangs
            .AsNoTracking()
            .Include(k => k.MaLoaiKhachNavigation)
            .Where(k => k.TenKhachHang.Contains(keyword) ||
                        (k.DienThoai != null && k.DienThoai.Contains(keyword)) ||
                        (k.Cccd != null && k.Cccd.Contains(keyword)))
            .Take(limit)
            .ToListAsync();
    }
}




