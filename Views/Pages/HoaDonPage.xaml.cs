using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class HoaDonPage : Page
{
    private readonly HoaDonPageViewModel _viewModel;

    public HoaDonPage()
    {
        InitializeComponent();

        var db = new QuanLyKhachSanContext();
        var khachHangSvc = new KhachHangService(db);
        var hoaDonSvc = new HoaDonService(db, khachHangSvc);

        _viewModel = new HoaDonPageViewModel(hoaDonSvc);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.TaiDuLieuAsync();
    }

    private void BtnTestPrint_Click(object sender, RoutedEventArgs e)
    {
        PrintHelper.TestPrint();
    }

    private async void HoaDonRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row) return;
        if (row.Item is not HoaDonDongViewModel item) return;

        var dialog = new HoaDonChiTietDialog(item.MaHoaDon, taiLaiTrangHoaDonAsync: () => _viewModel.TaiDuLieuAsync(buocTaiMoi: true))
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
            await _viewModel.TaiDuLieuAsync(buocTaiMoi: true);
    }
}
