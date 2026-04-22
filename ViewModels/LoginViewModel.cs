using System;
using System.Linq;
using System.Threading.Tasks;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authSvc;
    private string _tenDangNhap = "";
    private string _matKhau = ""; // Dùng property thay vì truyền Control
    private string _errorMessage = "";
    private bool _isLoading;

    public string TenDangNhap
    {
        get => _tenDangNhap;
        set => SetProperty(ref _tenDangNhap, value);
    }

    /// <summary>
    /// Được bind qua PasswordBoxBinding helper trong XAML
    /// </summary>
    public string MatKhau
    {
        get => _matKhau;
        set => SetProperty(ref _matKhau, value);
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

    // Senior Suggestion: Nếu dự án có AsyncRelayCommand, nên chuyển sang dùng nó.
    // Hiện tại tôi giữ RelayCommand nhưng xử lý Task an toàn bên trong.
    public RelayCommand LoginCommand { get; }

    public event Action? LoginSuccess;

    public LoginViewModel(IAuthService authService)
    {
        _authSvc = authService;
        // Trở lại chuẩn: Không nhận object parameter lạ
        LoginCommand = new RelayCommand(_ => _ = ExecuteLoginAsync(), _ => !IsLoading);
    }

    private async Task ExecuteLoginAsync()
    {
        // Lúc này MatKhau đã được Code-behind gán sẵn
        if (string.IsNullOrWhiteSpace(TenDangNhap) || string.IsNullOrWhiteSpace(MatKhau))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var result = await _authSvc.DangNhapAsync(TenDangNhap, MatKhau);

            if (result == null)
            {
                ErrorMessage = "Sai tài khoản, mật khẩu, hoặc tài khoản bị khóa.";
                return;
            }

            SaveSession(result);
            LoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Lỗi đăng nhập user: {TenDangNhap}", ex);
            ErrorMessage = $"Lỗi kết nối hệ thống. Vui lòng thử lại.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SaveSession(LoginResult result)
    {
        App.CurrentUser = result;

        // Cập nhật AppSession (Dữ liệu tĩnh toàn cục)
        AppSession.CurrentUser = result;
        AppSession.MaNhanVien = result.MaNhanVien;
        AppSession.TenNhanVien = result.TenNhanVien;
        AppSession.MaQuyen = result.Quyen.FirstOrDefault() ?? "";
        AppSession.TenDangNhap = TenDangNhap;
    }
}