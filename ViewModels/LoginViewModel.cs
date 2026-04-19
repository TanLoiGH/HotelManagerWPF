using System;
using System.Linq;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authSvc;
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

    public event Action? LoginSuccess;

    // Chỉ giữ một constructor duy nhất, nhận IAuthService (đã đăng ký trong DI)
    public LoginViewModel(IAuthService authService)
    {
        _authSvc = authService;
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
            var result = await _authSvc.DangNhapAsync(TenDangNhap, password);

            if (result == null)
            {
                ErrorMessage = "Sai tài khoản, mật khẩu, hoặc tài khoản bị khóa.";
                return;
            }

            // Lưu session toàn cục
            App.CurrentUser = result;
            AppSession.CurrentUser = result;
            AppSession.MaNhanVien = result.MaNhanVien;
            AppSession.TenNhanVien = result.TenNhanVien;
            AppSession.MaQuyen = result.Quyen.FirstOrDefault();
            AppSession.TenDangNhap = TenDangNhap;

            LoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi đăng nhập", ex);
            ErrorMessage = $"Lỗi kết nối: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}