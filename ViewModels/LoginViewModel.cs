using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _tenDangNhap = "";
    private string _errorMessage = "";
    private bool _isLoading;

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
        set
        {
            if (SetProperty(ref _isLoading, value))
                LoginCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand LoginCommand { get; }

    // LoginWindow subscribe event này để đóng cửa sổ
    public event Action? LoginSuccess;

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(ExecuteLogin, _ => !IsLoading);
    }

    private async void ExecuteLogin(object? parameter)
    {
        string password = parameter is PasswordBox pb ? pb.Password : "";

        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";

        try
        {
            using var db = new QuanLyKhachSanContext();
            var authSvc = new AuthService(db);
            var result = await authSvc.DangNhapAsync(TenDangNhap, password);

            if (result == null)
            {
                ErrorMessage = "Sai tài khoản, mật khẩu, hoặc tài khoản bị khóa.";
                return;
            }

            // Lưu session toàn cục (App.CurrentUser thay thế AppSession)
            App.CurrentUser = result;

            // Sync sang AppSession nếu còn dùng ở chỗ khác
            AppSession.MaNhanVien = result.MaNhanVien;
            AppSession.TenNhanVien = result.TenNhanVien;
            AppSession.MaQuyen = result.Quyen.FirstOrDefault();
            AppSession.TenDangNhap = TenDangNhap;

            LoginSuccess?.Invoke();
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




