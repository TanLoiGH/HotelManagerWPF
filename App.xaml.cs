using QuanLyKhachSan_PhamTanLoi.Views;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi
{
    public partial class App : Application
    {
        // Session toàn cục – dùng thay cho AppSession ở mọi nơi
        public static ViewModels.LoginResult? CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var login = new LoginWindow();
            login.ShowDialog();

            // Nếu đóng cửa sổ login mà chưa đăng nhập → thoát app
            if (CurrentUser == null)
            {
                Shutdown();
                return;
            }

            var main = new Views.MainWindow();
            main.Show();
        }
    }
}
