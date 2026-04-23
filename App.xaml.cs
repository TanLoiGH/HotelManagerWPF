using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Properties;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views;
using QuanLyKhachSan_PhamTanLoi.Views.Pages;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        public static LoginResult CurrentUser { get; internal set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Áp dụng theme đã lưu (dùng IsDarkMode)
            ApplySavedTheme();

            // 2. Khởi tạo DI Container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);

            // 3. Cấu hình hệ thống
            QuestPDF.Settings.License = LicenseType.Community;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 4. Mở màn hình Login
            var login = new LoginWindow();
            login.ShowDialog();

            if (string.IsNullOrEmpty(Helpers.AppSession.MaNhanVien))
            {
                Shutdown();
                return;
            }

            // 5. Mở MainWindow
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = ServiceProvider.GetRequiredService<MainWindow>();
            main.Show();
        }

        private void ApplySavedTheme()
        {
            bool isDark = Settings.Default.IsDarkMode;

            // Xóa theme cũ (Light.xaml hoặc Dark.xaml)
            var oldThemes = Resources.MergedDictionaries
                .Where(d => d.Source != null &&
                            (d.Source.OriginalString.Contains("Light.xaml") ||
                             d.Source.OriginalString.Contains("Dark.xaml")))
                .ToList();

            foreach (var t in oldThemes)
                Resources.MergedDictionaries.Remove(t);

            // Thêm theme mới vào đầu danh sách
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative)
            };
            Resources.MergedDictionaries.Insert(0, newTheme);
        }

        public static void SwitchTheme(bool isDark)
        {
            Settings.Default.IsDarkMode = isDark;
            Settings.Default.Save();

            ((App)Current).ApplySavedTheme();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // ==========================================
            // 1. ĐĂNG KÝ DATABASE & CORE
            // ==========================================
            services.AddDbContext<QuanLyKhachSanContext>();

            // ==========================================
            // 2. ĐĂNG KÝ SERVICES
            // ==========================================
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IHoaDonService, HoaDonService>();
            services.AddScoped<IKhachHangService, KhachHangService>();
            services.AddScoped<IPhongService, PhongService>();
            services.AddScoped<IDichVuService, DichVuService>();
            services.AddScoped<IKhuyenMaiService, KhuyenMaiService>();

            services.AddScoped<IHopThoaiService, HopThoaiServiceWpf>();
            services.AddScoped<IInHoaDonService, InHoaDonServiceWpf>();
            services.AddScoped<IChonDichVuService, ChonDichVuServiceWpf>();

            services.AddSingleton<AppStateService>();
            services.AddScoped<EmployeeService>();
            services.AddTransient<BaoCaoService>();
            services.AddTransient<PhongService>();
            services.AddTransient<DatPhongService>(); // Bổ sung cho SoDoPhong

            // ==========================================
            // 3. ĐĂNG KÝ VIEWMODELS
            // ==========================================
            services.AddTransient<SoDoPhongViewModel>();
            services.AddTransient<BaoCaoPageViewModel>();
            services.AddTransient<HoaDonPageViewModel>();
            // Bổ sung fix lỗi crash của bro

            // (Nếu bro có các ViewModel tương ứng cho các trang bên dưới, cứ add thêm vào đây)
            // services.AddTransient<CaiDatPageViewModel>();
            // services.AddTransient<KhachHangPageViewModel>();

            // ==========================================
            // 4. ĐĂNG KÝ VIEWS / PAGES
            // ==========================================
            services.AddTransient<MainWindow>();

            services.AddTransient<DashboardPage>();
            services.AddTransient<SoDoPhongPage>();
            services.AddTransient<BaoCaoPage>();

            services.AddTransient<HoaDonPage>();
            services.AddTransient<KhachHangPage>();
            services.AddTransient<LoaiKhachPage>();
            services.AddTransient<NhanVienPage>();
            services.AddTransient<NhaCungCapPage>();
            services.AddTransient<KhuyenMaiPage>();
            services.AddTransient<ChiPhiPage>();
            services.AddTransient<LoaiChiPhiPage>();
            services.AddTransient<LoaiPhongPage>();
            services.AddTransient<DichVuPage>();
            services.AddTransient<CaiDatPage>();
            services.AddTransient<QuanTriPhongPage>();
            services.AddTransient<PhuongThucThanhToanPage>();
            services.AddTransient<TienNghiPage>();
            services.AddTransient<TienNghiTrangThaiPage>();
            services.AddTransient<TienNghiDanhMucPage>();
        }


        private void GlobalWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem sender có đúng là Window không và chuột trái có đang giữ không
            if (sender is Window window && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    // Cho phép kéo dời Window
                    window.DragMove();
                }
                catch
                {
                    // Try-catch ở mức Global là Best Practice.
                    // Đề phòng trường hợp click vào các control đặc biệt (như ScrollBar, TextBox) 
                    // mà DragMove bị conflict thì ứng dụng vẫn không bị crash.
                }
            }
        }
    }
}