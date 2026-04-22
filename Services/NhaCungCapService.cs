using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class NhaCungCapService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Đưa quy tắc sinh mã vào hằng số để dễ bảo trì
    private const string PREFIX_NHA_CUNG_CAP = "NCC";

    public NhaCungCapService(QuanLyKhachSanContext db) => _db = db;

    #region TRUY VẤN DỮ LIỆU

    public async Task<List<NhaCungCap>> LayDanhSachAsync()
    {
        return await _db.NhaCungCaps
            .AsNoTracking()
            .OrderBy(n => n.TenNcc)
            .ToListAsync();
    }

    #endregion

    #region THAO TÁC DỮ LIỆU (CRUD)

    public async Task TaoMoiAsync(
        string tenNcc, string? dienThoai, string? email,
        string? diaChi, string? nguoiLienHe, bool isActive)
    {
        // Senior Fix: Bắt buộc phải có Tên nhà cung cấp
        if (string.IsNullOrWhiteSpace(tenNcc))
            throw new ArgumentException("Tên nhà cung cấp không được để trống.");

        var lastMa = await _db.NhaCungCaps
            .OrderByDescending(n => n.MaNcc)
            .Select(n => n.MaNcc)
            .FirstOrDefaultAsync();

        _db.NhaCungCaps.Add(new NhaCungCap
        {
            MaNcc = MaHelper.Next(PREFIX_NHA_CUNG_CAP, lastMa),

            // Senior Fix: Loại bỏ khoảng trắng thừa ở 2 đầu chuỗi
            TenNcc = tenNcc.Trim(),
            DienThoai = dienThoai?.Trim(),
            Email = email?.Trim(),
            DiaChi = diaChi?.Trim(),
            NguoiLienHe = nguoiLienHe?.Trim(),
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(
        string maNcc, string tenNcc, string? dienThoai,
        string? email, string? diaChi, string? nguoiLienHe, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(tenNcc))
            throw new ArgumentException("Tên nhà cung cấp không được để trống.");

        // Senior Fix: Dừng việc "nuốt" lỗi. Quăng exception nếu ID không tồn tại.
        var ncc = await _db.NhaCungCaps.FindAsync(maNcc)
                  ?? throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp mã {maNcc} để cập nhật.");

        ncc.TenNcc = tenNcc.Trim();
        ncc.DienThoai = dienThoai?.Trim();
        ncc.Email = email?.Trim();
        ncc.DiaChi = diaChi?.Trim();
        ncc.NguoiLienHe = nguoiLienHe?.Trim();
        ncc.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaAsync(string maNcc)
    {
        // Senior Fix: Báo lỗi cụ thể thay vì chỉ trả về false mập mờ
        var ncc = await _db.NhaCungCaps.FindAsync(maNcc)
                  ?? throw new KeyNotFoundException($"Không tìm thấy nhà cung cấp mã {maNcc} để thao tác.");

        bool coLienQuan = await _db.TienNghis.AnyAsync(t => t.MaNcc == ncc.MaNcc)
                          || await _db.ChiPhis.AnyAsync(c => c.MaNcc == ncc.MaNcc);

        if (coLienQuan)
        {
            // Soft Delete: Chuyển trạng thái sang ngưng hoạt động
            ncc.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // Hard Delete: Xóa vĩnh viễn khỏi Database
        _db.NhaCungCaps.Remove(ncc);
        await _db.SaveChangesAsync();
        return false;
    }

    #endregion
}