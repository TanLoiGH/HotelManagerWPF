using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;

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
        set => SetProperty(ref _isLoading, value);
    }

    public RelayCommand LoginCommand { get; }

    public event Action? LoginSuccess;

    public LoginViewModel()
    {
        _authService = new AuthService();
        LoginCommand = new RelayCommand(ExecuteLogin);
    }

    private void ExecuteLogin(object? parameter)
    {
        string password = "";

        if (parameter is PasswordBox pb)
            password = pb.Password;

        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";

        var taiKhoan = _authService.Login(TenDangNhap, password);

        if (taiKhoan == null)
        {
            ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            IsLoading = false;
            return;
        }

        LoginSuccess?.Invoke();
        IsLoading = false;
    }
}