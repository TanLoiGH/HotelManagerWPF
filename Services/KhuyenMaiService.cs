using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhuyenMaiService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom Magic String vào hằng số để dễ quản lý và thay đổi sau này
    private const string PREFIX_KHUYEN_MAI = "KM";

    public KhuyenMaiService(QuanLyKhachSanContext db) => _db = db;

    #region QUẢN LÝ TRUY VẤN DỮ LIỆU

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
        var now = TimeHelper.GetVietnamTime();
        return await _db.KhuyenMais
            .AsNoTracking()
            .Where(k => k.IsActive == true
                        && k.NgayBatDau <= now
                        && k.NgayKetThuc >= now
                        && (maLoaiKhach == null || k.MaLoaiKhach == maLoaiKhach))
            .OrderByDescending(k => k.NgayBatDau)
            .ToListAsync();
    }

    #endregion

    #region THAO TÁC DỮ LIỆU (CRUD)

    public async Task TaoMoiAsync(
        string tenKhuyenMai, string loaiKhuyenMai, decimal giaTri,
        decimal giaTriToiThieu, DateTime ngayBatDau, DateTime ngayKetThuc,
        string? maLoaiKhach, bool isActive)
    {
        // Senior Fix: Bổ sung Validation logic nghiệp vụ căn bản
        if (ngayKetThuc.Date < ngayBatDau.Date)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

        var lastMa = await _db.KhuyenMais
            .OrderByDescending(k => k.MaKhuyenMai)
            .Select(k => k.MaKhuyenMai)
            .FirstOrDefaultAsync();

        _db.KhuyenMais.Add(new KhuyenMai
        {
            MaKhuyenMai = MaHelper.Next(PREFIX_KHUYEN_MAI, lastMa),
            // Trim() để cắt khoảng trắng thừa do người dùng vô tình gõ phím cách
            TenKhuyenMai = tenKhuyenMai.Trim(),
            LoaiKhuyenMai = loaiKhuyenMai.Trim(),
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
        string maKhuyenMai, string tenKhuyenMai, string loaiKhuyenMai,
        decimal giaTri, decimal giaTriToiThieu, DateTime ngayBatDau,
        DateTime ngayKetThuc, string? maLoaiKhach, bool isActive)
    {
        // Senior Fix: Validation logic nghiệp vụ
        if (ngayKetThuc.Date < ngayBatDau.Date)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

        // Senior Fix: Bắt lỗi rõ ràng nếu không tìm thấy dữ liệu
        var km = await _db.KhuyenMais.FindAsync(maKhuyenMai)
                 ?? throw new KeyNotFoundException($"Không tìm thấy chương trình khuyến mãi mã {maKhuyenMai}.");

        km.TenKhuyenMai = tenKhuyenMai.Trim();
        km.LoaiKhuyenMai = loaiKhuyenMai.Trim();
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
        var km = await _db.KhuyenMais.FindAsync(maKhuyenMai)
                 ?? throw new KeyNotFoundException($"Không tìm thấy chương trình khuyến mãi mã {maKhuyenMai} để xóa.");

        bool daDung = await _db.HoaDons.AnyAsync(h => h.MaKhuyenMai == km.MaKhuyenMai);
        if (daDung)
        {
            // Đã phát sinh giao dịch -> Soft Delete
            km.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // Chưa phát sinh giao dịch -> Hard Delete
        _db.KhuyenMais.Remove(km);
        await _db.SaveChangesAsync();
        return false;
    }

    #endregion
}