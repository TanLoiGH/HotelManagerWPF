using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views;
using QuestPDF.Infrastructure;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi
{
    public partial class App : Application
    {
        // Session toàn cục – dùng thay cho AppSession ở mọi nơi
        public static LoginResult? CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Không tự shutdown khi LoginWindow đóng
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            QuestPDF.Settings.License = LicenseType.Community;
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


        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Singleton (Duy trì trạng thái UI)
            services.AddSingleton<AppStateService>();

            // 2. Scoped (Các Service nghiệp vụ)
            services.AddScoped<IPhongService, PhongService>();
            services.AddScoped<IKhachHangService, KhachHangService>();
            services.AddScoped<IDatPhongService, DatPhongService>();
            services.AddScoped<IDichVuService, DichVuService>();
            services.AddScoped<IHoaDonService, HoaDonService>();

            // 3. Transient (Các ViewModels - tạo mới mỗi khi mở trang)
            services.AddTransient<SoDoPhongViewModel>();
            services.AddTransient<HoaDonChiTietViewModel>();
        }

    }
}




