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


    public AuthService(QuanLyKhachSanContext db)
    {
        _db = db;
        
    }

    public async Task<LoginResult?> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        try
        {
            var tk = await _db.TaiKhoans
                .Include(t => t.MaNhanVienNavigation).ThenInclude(nv => nv.MaTrangThaiNavigation)
                .Include(t => t.MaQuyenNavigation)
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap && t.IsActive == true);

            if (tk == null) return null;

            bool authenticated = false;
            string stored = tk.MatKhau ?? "";

            if (!string.IsNullOrEmpty(stored) && stored.StartsWith("HASH2:"))
            {
                authenticated = PasswordHasher.Verify(matKhau, stored);
            }
            else if (!string.IsNullOrEmpty(stored) && stored == matKhau)
            {
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
                ChucVu = nv.ChucVu ?? "",
                Quyen = new List<string> { tk.MaQuyen }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi đăng nhập", ex);
            throw;
        }
    }

    public async Task<List<NhanVienViewModel>> GetNhanViensAsync()
        => await _db.NhanViens
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
                Quyen = nv.TaiKhoans.Where(t => t.IsActive == true).Select(t => t.MaQuyenNavigation.TenQuyen ?? "").ToList()
            }).ToListAsync();

    public async Task DoiMatKhauAsync(string maNhanVien, string? tenDangNhap, string matKhauCu, string matKhauMoi)
    {
        var account = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaNhanVien == maNhanVien && t.IsActive == true);
        if (account == null) throw new InvalidOperationException("Không tìm thấy tài khoản.");

        bool ok = account.MatKhau.StartsWith("HASH2:")
            ? PasswordHasher.Verify(matKhauCu, account.MatKhau)
            : account.MatKhau == matKhauCu;

        if (!ok) throw new InvalidOperationException("Mật khẩu cũ không đúng.");

        account.MatKhau = PasswordHasher.Hash(matKhauMoi);
        await _db.SaveChangesAsync();
    }
}