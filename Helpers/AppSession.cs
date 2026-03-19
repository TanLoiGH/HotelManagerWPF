namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class AppSession
{
    public static string? MaNhanVien    { get; set; }
    public static string? TenNhanVien   { get; set; }
    public static string? MaQuyen       { get; set; }
    public static string? TenDangNhap   { get; set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(MaNhanVien);

    public static void Clear()
    {
        MaNhanVien  = null;
        TenNhanVien = null;
        MaQuyen     = null;
        TenDangNhap = null;
    }
}


