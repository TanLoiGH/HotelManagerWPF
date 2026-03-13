using System.Windows;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Views;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _tenDangNhap = string.Empty;
    private string _errorMessage = string.Empty;
    private bool   _isLoading;

    public string TenDangNhap
    {
        get => _tenDangNhap;
        set => SetProperty(ref _tenDangNhap, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public RelayCommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(ExecuteLogin, _ => !IsLoading);
    }

    private void ExecuteLogin(object? parameter)
    {
        // parameter là PasswordBox (truyền qua CommandParameter)
        string matKhau = string.Empty;
        if (parameter is System.Windows.Controls.PasswordBox pb)
            matKhau = pb.Password;

        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
        {
            ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            using var db = new QuanLyKhachSanContext();

            var taiKhoan = db.TaiKhoans
                .FirstOrDefault(tk => tk.TenDangNhap == TenDangNhap
                                   && tk.MatKhau     == matKhau
                                   && tk.IsActive    == true);

            if (taiKhoan == null)
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return;
            }

            var nhanVien = db.NhanViens.Find(taiKhoan.MaNhanVien);

            // Lưu session
            AppSession.MaNhanVien  = taiKhoan.MaNhanVien;
            AppSession.TenNhanVien = nhanVien?.TenNhanVien ?? taiKhoan.MaNhanVien;
            AppSession.MaQuyen     = taiKhoan.MaQuyen;
            AppSession.TenDangNhap = taiKhoan.TenDangNhap;

            // Mở MainWindow, đóng LoginWindow
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi kết nối: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
