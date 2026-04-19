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

    // Thêm 3 hàm này vào class HoaDonPage
    private bool KiemTraQuyenQuanTri()
    {
        // Giả sử Q_ADMIN và Q_KETOAN là mã quyền có thể sửa/xóa
        if (AppSession.MaQuyen == "Q_ADMIN" || AppSession.MaQuyen == "Q_KETOAN")
            return true;

        ConfirmHelper.ShowWarning("Bạn không có quyền thực hiện thao tác này!");
        return false;
    }

    private void BtnSua_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true; // Ngăn không cho kích hoạt sự kiện click dòng (mở dialog chi tiết)

        if (!KiemTraQuyenQuanTri()) return;

        if (sender is Button { DataContext: HoaDonDongViewModel item })
        {
            // TODO: Mở form/dialog sửa hóa đơn tại đây
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
                using var db = new QuanLyKhachSanContext();
                var khachHangSvc = new KhachHangService(db);
                var hdSvc = new HoaDonService(db, khachHangSvc);

                await hdSvc.HuyHoaDonAsync(item.MaHoaDon);

                ConfirmHelper.ShowInfo("Đã hủy hóa đơn thành công!");
                await _viewModel.TaiDuLieuAsync(buocTaiMoi: true); // Load lại bảng
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi xóa hóa đơn", ex);
                ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
            }
        }
    }



}
