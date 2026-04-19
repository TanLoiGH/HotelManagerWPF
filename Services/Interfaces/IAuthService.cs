using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult?> DangNhapAsync(string tenDangNhap, string matKhau);
    Task<List<NhanVienViewModel>> GetNhanViensAsync();
    Task DoiMatKhauAsync(string maNhanVien, string? tenDangNhap, string matKhauCu, string matKhauMoi);
}