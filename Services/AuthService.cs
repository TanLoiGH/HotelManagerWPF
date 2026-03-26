using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class AuthService
{
    private readonly QuanLyKhachSanContext _db;
    public AuthService(QuanLyKhachSanContext db) => _db = db;

    public async Task<LoginResult?> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        try
        {
            var tk = await _db.TaiKhoans
                .Include(t => t.MaNhanVienNavigation)
                    .ThenInclude(nv => nv.MaTrangThaiNavigation)
                .Include(t => t.MaQuyenNavigation)
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap && t.IsActive == true);

            if (tk == null)
            {
                System.Diagnostics.Debug.WriteLine("LOGIN_FAIL: User not found or inactive");
                return null;
            }

            bool authenticated = false;
            string stored = tk.MatKhau ?? "";

            System.Diagnostics.Debug.WriteLine($"LOGIN_DEBUG: StoredPassword = {stored}");

            if (!string.IsNullOrEmpty(stored) && stored.StartsWith("HASH2:"))
            {
                try
                {
                    authenticated = PasswordHasher.Verify(matKhau, stored);
                    System.Diagnostics.Debug.WriteLine($"LOGIN_DEBUG: Verify HASH2 result = {authenticated}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HASH_VERIFY_ERROR: {ex.Message}");
                    return null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("LOGIN_DEBUG: Plaintext password detected");

                if (stored == matKhau)
                {
                    authenticated = true;

                    System.Diagnostics.Debug.WriteLine("LOGIN_DEBUG: Plaintext match → upgrading to HASH2");

                    tk.MatKhau = PasswordHasher.Hash(matKhau);
                    await _db.SaveChangesAsync();
                }
            }

            if (!authenticated)
            {
                System.Diagnostics.Debug.WriteLine("LOGIN_FAIL: Password incorrect");
                return null;
            }

            var nv = tk.MaNhanVienNavigation;

            if (nv == null)
            {
                System.Diagnostics.Debug.WriteLine("LOGIN_FAIL: Employee record missing");
                return null;
            }

            if (tk.MaNhanVienNavigation?.MaTrangThaiNavigation?.AllowLogin != true)
            {
                System.Diagnostics.Debug.WriteLine($"LOGIN_FAIL: Employee AllowLogin? invalid = {nv.MaTrangThai}");
                return null;
            }

            var quyen = await _db.TaiKhoans
                .Where(t => t.MaNhanVien == nv.MaNhanVien && t.IsActive == true)
                .Select(t => t.MaQuyen)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine("LOGIN_SUCCESS");

            return new LoginResult
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "",
                Quyen = quyen
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LOGIN_EXCEPTION: {ex}");
            throw;
        }
    }
    public async Task<List<NhanVienViewModel>> GetNhanViensAsync()
        => await _db.NhanViens
            .Include(nv => nv.MaTrangThaiNavigation)
            .Include(nv => nv.TaiKhoans)
                .ThenInclude(tk => tk.MaQuyenNavigation)
            .Select(nv => new NhanVienViewModel
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "",
                DienThoai = nv.DienThoai ?? "",
                TenTrangThai = nv.MaTrangThaiNavigation != null
                               ? nv.MaTrangThaiNavigation.TenTrangThai ?? "" : "",
                Quyen = nv.TaiKhoans
                        .Where(t => t.IsActive == true)
                        .Select(t => t.MaQuyenNavigation.TenQuyen ?? "")
                        .ToList()
            })
            .ToListAsync();
}




