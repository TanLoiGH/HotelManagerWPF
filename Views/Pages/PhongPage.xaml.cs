using System;
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
    private readonly QuanLyKhachSanContext _dbPhong;
    private readonly QuanLyKhachSanContext _dbKhachHang;
    private readonly QuanLyKhachSanContext _dbDatPhong;

    public PhongPage()
    {
        InitializeComponent();

        // Manual DI for now, since we don't have a full DI container set up yet
        _dbPhong = new QuanLyKhachSanContext();
        _dbKhachHang = new QuanLyKhachSanContext();
        _dbDatPhong = new QuanLyKhachSanContext();

        var roomService = new RoomService(_dbPhong);
        var khachHangService = new KhachHangService(_dbKhachHang);
        var datPhongService = new DatPhongService(_dbDatPhong);

        _viewModel = new SoDoPhongViewModel(roomService, khachHangService, datPhongService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.TaiDuLieuAsync();
        Unloaded += (_, _) =>
        {
            _dbPhong.Dispose();
            _dbKhachHang.Dispose();
            _dbDatPhong.Dispose();
        };
    }

    private void PhongCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PhongCardViewModel vm)
        {
            _viewModel.SelectedRoom = vm;
        }
    }
}
