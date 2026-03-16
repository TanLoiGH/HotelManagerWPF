using System.Windows;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        var vm = new LoginViewModel();
        vm.LoginSuccess += OnLoginSuccess;

        DataContext = vm;
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