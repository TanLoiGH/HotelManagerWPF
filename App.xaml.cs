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

            // ✅ Không tự shutdown khi LoginWindow đóng
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new LoginWindow();
            login.ShowDialog();

            if (CurrentUser == null)
            {
                Shutdown();
                return;
            }

            // ✅ Đổi lại OnMainWindowClose sau khi đã có MainWindow
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var main = new MainWindow();
            main.Show();
        }
    }
}
