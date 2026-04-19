using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Net;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class AppSession
{
    public static string? MaNhanVien { get; set; }
    public static string? TenNhanVien { get; set; }
    public static string? MaQuyen { get; set; }
    public static string? TenDangNhap { get; set; }
    public static string? IpAddress { get; set; }
    public static LoginResult? CurrentUser { get; set; }
    public static bool IsLoggedIn => !string.IsNullOrEmpty(MaNhanVien);

    public static void Clear()
    {
        MaNhanVien = null;
        TenNhanVien = null;
        MaQuyen = null;
        TenDangNhap = null;
        IpAddress = null;
        CurrentUser = null;
    }
}


