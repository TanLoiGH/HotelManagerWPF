using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class AuthService
{
    private readonly QuanLyKhachSanContext _db;
    public AuthService(QuanLyKhachSanContext db) => _db = db;

    public async Task<LoginResult?> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var tk = await _db.TaiKhoans
            .Include(t => t.MaNhanVienNavigation)
                .ThenInclude(nv => nv.MaTrangThaiNavigation)
            .Include(t => t.MaQuyenNavigation)
            .FirstOrDefaultAsync(t =>
                t.TenDangNhap == tenDangNhap &&
                t.MatKhau == matKhau &&
                t.IsActive == true);

        if (tk == null) return null;

        var nv = tk.MaNhanVienNavigation;
        if (nv.MaTrangThai != "TT01") return null;

        var quyen = await _db.TaiKhoans
            .Where(t => t.MaNhanVien == nv.MaNhanVien && t.IsActive == true)
            .Select(t => t.MaQuyen)
            .ToListAsync();

        return new LoginResult
        {
            MaNhanVien = nv.MaNhanVien,
            TenNhanVien = nv.TenNhanVien,
            ChucVu = nv.ChucVu ?? "",
            Quyen = quyen
        };
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