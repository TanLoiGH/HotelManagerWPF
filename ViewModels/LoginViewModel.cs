using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authSvc;
    private string _tenDangNhap = "";
    private string _errorMessage = "";
    private bool _isLoading;
    private IAuthService authService;

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

    public LoginViewModel(AuthService authSvc)
    {
        _authSvc = authSvc;
        LoginCommand = new RelayCommand(ExecuteLogin, _ => !IsLoading);
    }

    public LoginViewModel(IAuthService authService)
    {
        this.authService = authService;
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
            var result = await _authSvc.DangNhapAsync(TenDangNhap, password);

            if (result == null)
            {
                ErrorMessage = "Sai tài khoản, mật khẩu, hoặc tài khoản bị khóa.";
                return;
            }

            // Lưu session toàn cục (App.CurrentUser thay thế AppSession)
            App.CurrentUser = result;

            // Sync sang AppSession nếu còn dùng ở chỗ khác
            AppSession.CurrentUser = result;
            AppSession.MaNhanVien = result.MaNhanVien;
            AppSession.TenNhanVien = result.TenNhanVien;
            AppSession.MaQuyen = result.Quyen.FirstOrDefault();
            AppSession.TenDangNhap = TenDangNhap;

            LoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ErrorMessage = $"Lỗi kết nối: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}




