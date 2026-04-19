using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // Lấy AuthService từ ServiceProvider
        var authService = App.ServiceProvider.GetRequiredService<IAuthService>();

        // Tạo ViewModel
        var viewModel = new LoginViewModel(authService);

        // BẮT SỰ KIỆN: Khi đăng nhập thành công thì tự động đóng cửa sổ lại
        viewModel.LoginSuccess += () =>
        {
            this.DialogResult = true; // Lệnh này sẽ tự động đóng ShowDialog() lại
        };

        // Gán ViewModel cho DataContext
        DataContext = viewModel;
    }

    private void Window_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ✅ Enter ở bất kỳ đâu trong Window → login
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TriggerLogin();
    }

    // ✅ Enter trong từng TextBox/PasswordBox
    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true; // tránh event bubble lên Window
            TriggerLogin();
        }
    }

    private void TriggerLogin()
    {
        if (DataContext is LoginViewModel vm && vm.LoginCommand != null && vm.LoginCommand.CanExecute(PbPassword))
        {
            vm.LoginCommand.Execute(PbPassword);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Tắt cưỡng chế toàn bộ ứng dụng khi bấm nút X
        Application.Current.Shutdown();
    }

}