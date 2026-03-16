using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class MainViewModel : BaseViewModel
{
    public string WelcomeMessage => $"Xin chào, {AppSession.TenNhanVien ?? "Admin"}";
    public string MaQuyen       => AppSession.MaQuyen ?? "";
    public string NgayHienTai   => DateTime.Now.ToString("dddd, dd/MM/yyyy");
}
