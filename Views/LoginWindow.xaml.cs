using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        var vm = new LoginViewModel();
        vm.LoginSuccess += () => DialogResult = true;
        DataContext = vm;
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
        if (DataContext is LoginViewModel vm &&
            vm.LoginCommand.CanExecute(PbPassword))
        {
            vm.LoginCommand.Execute(PbPassword);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}