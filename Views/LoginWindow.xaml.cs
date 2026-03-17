using System.Windows;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // DataContext đã set trong XAML (vm:LoginViewModel), chỉ cần wire event
        if (DataContext is LoginViewModel vm)
            vm.LoginSuccess += () => DialogResult = true;
    }

    private void OnLoginSuccess()
    {
        var main = new MainWindow();
        main.Show();
        this.Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}