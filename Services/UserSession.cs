using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services
{
    public static class UserSession
    {
        public static NhanVien? CurrentUser { get; set; }
        public static bool IsLoggedIn => CurrentUser != null;

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}
