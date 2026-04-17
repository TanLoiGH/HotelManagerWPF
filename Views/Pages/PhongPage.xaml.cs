using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class PhongPage : Page
{
    private readonly SoDoPhongViewModel _viewModel;
    private readonly QuanLyKhachSanContext _db;

    public PhongPage()
    {
        InitializeComponent();

        _db = new QuanLyKhachSanContext();

        var roomService = new PhongService(_db);
        var khachHangService = new KhachHangService(_db);
        var datPhongService = new DatPhongService(_db);

        _viewModel = new SoDoPhongViewModel(roomService, khachHangService, datPhongService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.TaiDuLieuAsync();
        Unloaded += (_, _) =>
        {
            _db.Dispose();
            _db.Dispose();
            _db.Dispose();
        };
    }

    private void PhongCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PhongCardViewModel vm)
        {
            _viewModel.SelectedRoom = vm;
            if (!vm.IsSelected) vm.IsSelected = true;
        }
    }
}
