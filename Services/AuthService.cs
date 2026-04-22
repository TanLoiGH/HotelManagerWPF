using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class AuthService : IAuthService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Fix: Đã xóa HASH_PREFIX ở đây vì PasswordHasher đã đảm nhiệm việc đó!

    public AuthService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<LoginResult?> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
                return null;

            var tk = await _db.TaiKhoans
                .Include(t => t.MaNhanVienNavigation)
                .ThenInclude(nv => nv.MaTrangThaiNavigation)
                .Include(t => t.MaQuyenNavigation)
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap && t.IsActive == true);

            if (tk == null) return null;

            bool authenticated = false;
            string stored = tk.MatKhau ?? "";

            // Senior Fix: Gọi trực tiếp hàm IsHashed() đọc rất tự nhiên giống tiếng Anh
            if (PasswordHasher.IsHashed(stored))
            {
                authenticated = PasswordHasher.Verify(matKhau, stored);
            }
            else if (!string.IsNullOrEmpty(stored) && stored == matKhau)
            {
                // Tự động nâng cấp mật khẩu thường thành Hash bảo mật
                authenticated = true;
                tk.MatKhau = PasswordHasher.Hash(matKhau);
                await _db.SaveChangesAsync();
            }

            if (!authenticated || tk.MaNhanVienNavigation?.MaTrangThaiNavigation?.AllowLogin != true)
                return null;

            var nv = tk.MaNhanVienNavigation;

            return new LoginResult
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "Nhân viên",
                Quyen = new List<string> { tk.MaQuyen }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError($"Lỗi đăng nhập cho user [{tenDangNhap}]", ex);
            throw new InvalidOperationException("Hệ thống đăng nhập đang gặp sự cố, vui lòng thử lại sau.", ex);
        }
    }

    // Senior Note: Khuyên bạn nên chuyển hàm này sang NhanVienService.cs cho đúng chuẩn SOLID
    public async Task<List<NhanVienViewModel>> GetNhanViensAsync()
    {
        return await _db.NhanViens
            .AsNoTracking()
            .Include(nv => nv.MaTrangThaiNavigation)
            .Include(nv => nv.TaiKhoans).ThenInclude(tk => tk.MaQuyenNavigation)
            .Select(nv => new NhanVienViewModel
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "",
                DienThoai = nv.DienThoai ?? "",
                Email = nv.Email ?? "",
                TenTrangThai = nv.MaTrangThaiNavigation != null ? nv.MaTrangThaiNavigation.TenTrangThai ?? "" : "",
                Quyen = nv.TaiKhoans.Where(t => t.IsActive == true).Select(t => t.MaQuyenNavigation.TenQuyen ?? "")
                    .ToList()
            }).ToListAsync();
    }

    public async Task DoiMatKhauAsync(string maNhanVien, string? tenDangNhap, string matKhauCu, string matKhauMoi)
    {
        if (string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi))
            throw new ArgumentException("Mật khẩu không được để trống.");

        var account = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaNhanVien == maNhanVien && t.IsActive == true)
                      ?? throw new KeyNotFoundException("Không tìm thấy tài khoản nhân viên đang hoạt động.");

        string stored = account.MatKhau ?? "";

        // Senior Fix: Thay bằng hàm IsHashed()
        bool ok = PasswordHasher.IsHashed(stored)
            ? PasswordHasher.Verify(matKhauCu, stored)
            : stored == matKhauCu;

        if (!ok)
            throw new UnauthorizedAccessException("Mật khẩu cũ không chính xác.");

        account.MatKhau = PasswordHasher.Hash(matKhauMoi);
        await _db.SaveChangesAsync();
    }
}