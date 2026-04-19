using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views;
using QuestPDF.Infrastructure;
using System;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static LoginResult CurrentUser { get; internal set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Khởi tạo và xây dựng DI Container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // 2. Cấu hình hệ thống
            QuestPDF.Settings.License = LicenseType.Community;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 3. Chạy màn hình Login (Có thể lấy từ DI hoặc tạo mới)
            var login = new LoginWindow();
            login.ShowDialog();

            // Kiểm tra đăng nhập thành công từ AppSession (hoặc CurrentUser)
            if (string.IsNullOrEmpty(Helpers.AppSession.MaNhanVien))
            {
                Shutdown();
                return;
            }

            // 4. Mở MainWindow thông qua ServiceProvider để hỗ trợ DI
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = ServiceProvider.GetRequiredService<MainWindow>();
            main.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<QuanLyKhachSanContext>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IHoaDonService, HoaDonService>();
            services.AddScoped<IKhachHangService, KhachHangService>();
            services.AddScoped<IPhongService, PhongService>();
            services.AddScoped<IDichVuService, DichVuService>(); // Đăng ký thêm dòng này

            // Đăng ký các Service hỗ trợ giao diện (Nếu đã có Interface)
            services.AddScoped<IHopThoaiService, HopThoaiServiceWpf>();
            services.AddScoped<IInHoaDonService, InHoaDonServiceWpf>();
            services.AddScoped<IChonDichVuService, ChonDichVuServiceWpf>();

            services.AddSingleton<AppStateService>();
            services.AddScoped<EmployeeService>();
            services.AddSingleton<MainWindow>();

            services.AddTransient<CaiDatViewModel>();
            services.AddTransient<NhatKyHeThongViewModel>();
            services.AddTransient<SoDoPhongViewModel>();
            services.AddTransient<HoaDonPageViewModel>();
        }
    }
}