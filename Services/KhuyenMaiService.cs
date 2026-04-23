using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class KhuyenMaiService : IKhuyenMaiService
{
    private readonly QuanLyKhachSanContext _db;

    // ✅ SENIOR FIX 1: Thêm SemaphoreSlim để đảm bảo an toàn Threading trong WPF
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private const string PREFIX_KHUYEN_MAI = "KM";

    public KhuyenMaiService(QuanLyKhachSanContext db) => _db = db;

    #region QUẢN LÝ TRUY VẤN DỮ LIỆU

    // ✅ SENIOR FIX 2: Bổ sung hàm này để khớp với IKhuyenMaiService mà HoaDonChiTietViewModel đang gọi
    public async Task<List<KhuyenMai>> GetAllKhuyenMaiAsync()
    {
        return await LayDanhSachAsync();
    }

    public async Task<List<LoaiKhach>> LayLoaiKhachAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.LoaiKhaches
                .AsNoTracking()
                .OrderBy(l => l.NguongTichLuy)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<KhuyenMai>> LayDanhSachAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.KhuyenMais
                .AsNoTracking()
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<KhuyenMai>> LayKhuyenMaiConHieuLucTheoLoaiKhachAsync(string? maLoaiKhach)
    {
        await _semaphore.WaitAsync();
        try
        {
            var now = TimeHelper.GetVietnamTime();
            return await _db.KhuyenMais
                .AsNoTracking()
                .Where(k => k.IsActive == true
                            // ✅ SENIOR FIX 3: Xử lý an toàn trường hợp ngày tháng bị Null trong DB
                            && (k.NgayBatDau == null || k.NgayBatDau <= now)
                            && (k.NgayKetThuc == null || k.NgayKetThuc >= now)
                            && (string.IsNullOrEmpty(maLoaiKhach) || k.MaLoaiKhach == maLoaiKhach))
                .OrderByDescending(k => k.NgayBatDau)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region THAO TÁC DỮ LIỆU (CRUD)

    public async Task TaoMoiAsync(
        string tenKhuyenMai, string loaiKhuyenMai, decimal giaTri,
        decimal giaTriToiThieu, DateTime ngayBatDau, DateTime ngayKetThuc,
        string? maLoaiKhach, bool isActive)
    {
        if (ngayKetThuc.Date < ngayBatDau.Date)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

        await _semaphore.WaitAsync();
        try
        {
            var lastMa = await _db.KhuyenMais
                .OrderByDescending(k => k.MaKhuyenMai)
                .Select(k => k.MaKhuyenMai)
                .FirstOrDefaultAsync();

            _db.KhuyenMais.Add(new KhuyenMai
            {
                MaKhuyenMai = MaHelper.Next(PREFIX_KHUYEN_MAI, lastMa),

                // ✅ SENIOR FIX 4: Thêm toán tử ?. và ?? để tránh lỗi NullReferenceException
                TenKhuyenMai = tenKhuyenMai?.Trim() ?? "Không tên",
                LoaiKhuyenMai = loaiKhuyenMai?.Trim() ?? "Tiền mặt",
                GiaTriKm = giaTri,
                GiaTriToiThieu = giaTriToiThieu,
                NgayBatDau = ngayBatDau,
                NgayKetThuc = ngayKetThuc,
                MaLoaiKhach = string.IsNullOrWhiteSpace(maLoaiKhach) ? null : maLoaiKhach,
                IsActive = isActive,
            });

            await _db.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CapNhatAsync(
        string maKhuyenMai, string tenKhuyenMai, string loaiKhuyenMai,
        decimal giaTri, decimal giaTriToiThieu, DateTime ngayBatDau,
        DateTime ngayKetThuc, string? maLoaiKhach, bool isActive)
    {
        if (ngayKetThuc.Date < ngayBatDau.Date)
            throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

        await _semaphore.WaitAsync();
        try
        {
            var km = await _db.KhuyenMais.FindAsync(maKhuyenMai)
                     ?? throw new KeyNotFoundException($"Không tìm thấy chương trình khuyến mãi mã {maKhuyenMai}.");

            km.TenKhuyenMai = tenKhuyenMai?.Trim() ?? "Không tên";
            km.LoaiKhuyenMai = loaiKhuyenMai?.Trim() ?? "Tiền mặt";
            km.GiaTriKm = giaTri;
            km.GiaTriToiThieu = giaTriToiThieu;
            km.NgayBatDau = ngayBatDau;
            km.NgayKetThuc = ngayKetThuc;
            km.MaLoaiKhach = string.IsNullOrWhiteSpace(maLoaiKhach) ? null : maLoaiKhach;
            km.IsActive = isActive;

            await _db.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> XoaHoacTatAsync(string maKhuyenMai)
    {
        await _semaphore.WaitAsync();
        try
        {
            var km = await _db.KhuyenMais.FindAsync(maKhuyenMai)
                     ?? throw new KeyNotFoundException($"Không tìm thấy khuyến mãi mã {maKhuyenMai}.");

            bool daDung = await _db.HoaDons.AnyAsync(h => h.MaKhuyenMai == km.MaKhuyenMai);
            if (daDung)
            {
                km.IsActive = false; // Soft delete
                await _db.SaveChangesAsync();
                return true;
            }

            _db.KhuyenMais.Remove(km); // Hard delete
            await _db.SaveChangesAsync();
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}