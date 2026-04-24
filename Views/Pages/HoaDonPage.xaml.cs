using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

namespace QuanLyKhachSan_PhamTanLoi.Views.Pages;

public partial class HoaDonPage : Page
{
    private readonly HoaDonPageViewModel _viewModel = App.ServiceProvider.GetRequiredService<HoaDonPageViewModel>();

    public HoaDonPage()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += async (s, e) => await _viewModel.TaiDuLieuAsync();
    }

    private async void HoaDonRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow row || row.Item is not HoaDonDongViewModel item) return;

        var dialog = new HoaDonChiTietDialog(item.MaHoaDon,
            taiLaiTrangHoaDonAsync: () => _viewModel.TaiDuLieuAsync(buocTaiMoi: true))
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
            await _viewModel.TaiDuLieuAsync(buocTaiMoi: true);
    }

    private bool KiemTraQuyenQuanTri()
    {
        if (AppSession.MaQuyen == "ADMIN" || AppSession.MaQuyen == "KETOAN")
            return true;

        ConfirmHelper.ShowWarning("Bạn không có quyền thực hiện thao tác này!");
        return false;
    }

    private void BtnSua_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!KiemTraQuyenQuanTri()) return;

        if (sender is Button { DataContext: HoaDonDongViewModel item })
        {
            MessageBox.Show($"Mở form sửa cho hóa đơn: {item.MaHoaDon}", "Thông báo");
        }
    }

    private async void BtnXoa_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!KiemTraQuyenQuanTri()) return;

        if (sender is Button { DataContext: HoaDonDongViewModel item })
        {
            if (item.TrangThai == "Đã thanh toán")
            {
                ConfirmHelper.ShowWarning("Không thể xóa hóa đơn đã hoàn tất thanh toán!");
                return;
            }

            if (!ConfirmHelper.ConfirmDelete($"hóa đơn {item.MaHoaDon} (Xóa mềm)")) return;

            try
            {
                // ✅ ĐÚNG: Lấy service từ DI thay vì 'new' thủ công
                var hdSvc = App.ServiceProvider.GetRequiredService<IHoaDonService>();

                await hdSvc.XoaMemHoaDonAsync(item.MaHoaDon);

                ConfirmHelper.ShowInfo("Đã hủy hóa đơn thành công!");
                await _viewModel.TaiDuLieuAsync(buocTaiMoi: true);
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi xóa hóa đơn", ex);
                ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }
    }
}