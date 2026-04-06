using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views.Pages
{
    /// <summary>
    /// Interaction logic for CaiDatPage.xaml
    /// </summary>
    public partial class CaiDatPage : Page
    {
        private readonly QuanLyKhachSanContext _db;

        public CaiDatPage()
        {
            InitializeComponent();
            _db = new QuanLyKhachSanContext();
            DataContext = new CaiDatViewModel(
                new EmployeeService(_db),
                new AuthService(_db),
                new HoaDonService(_db, new KhachHangService(_db)));
            Unloaded += (_, _) => _db.Dispose();
        }

    }
}
