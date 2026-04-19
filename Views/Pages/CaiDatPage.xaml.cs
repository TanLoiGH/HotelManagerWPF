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

            // Lấy các Service từ DI Container
            var employeeService = App.ServiceProvider.GetRequiredService<EmployeeService>();
            var authService = App.ServiceProvider.GetRequiredService<IAuthService>();
            var hoaDonService = App.ServiceProvider.GetRequiredService<IHoaDonService>();

            // Gán vào duy nhất 1 Constructor của ViewModel
            DataContext = new CaiDatViewModel(employeeService, authService, hoaDonService);
        }
    }
}