using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views.Pages
{
    public partial class CaiDatPage : Page
    {
        public CaiDatPage()
        {
            InitializeComponent();

            // Lấy các Service đã được DI Container cấu hình sẵn
            // Không dùng 'new' để tránh lỗi thiếu tham số auditService
            var employeeService = App.ServiceProvider.GetRequiredService<EmployeeService>();
            var authService = App.ServiceProvider.GetRequiredService<IAuthService>();
            var hoaDonService = App.ServiceProvider.GetRequiredService<IHoaDonService>();

            DataContext = new CaiDatViewModel(employeeService, authService, hoaDonService);
        }
    }
}