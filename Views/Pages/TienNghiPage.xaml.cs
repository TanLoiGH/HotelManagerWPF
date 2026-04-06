// TienNghiPage.xaml.cs — thay toàn bộ
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class TienNghiPage : Page
{
    public TienNghiPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadComboboxesAsync();
    }

    private async Task LoadComboboxesAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var roomSvc = new RoomService(db);
        var tnSvc = new TienNghiService(db);

        // Load CboPhong
        var phongs = await roomSvc.LayDanhSachMaPhongAsync();
        CboPhong.ItemsSource = phongs;

        // Load CboTrangThai
        var trangThais = await tnSvc.LayTrangThaiTienNghiAsync();
        CboTrangThai.ItemsSource = trangThais;
        CboTrangThai.DisplayMemberPath = "TenTrangThai";
        CboTrangThai.SelectedValuePath = "MaTrangThai";
        CboTrangThai.SelectedIndex = 0;
    }

    private async void CboPhong_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboPhong.SelectedValue is not string maPhong) return;

        using var db = new QuanLyKhachSanContext();
        var svc = new TienNghiService(db);
        var items = await svc.GetTienNghiPhongAsync(maPhong);
        TienNghiGrid.ItemsSource = items;

        int canBT = items.Count(i => i.CanBaoTri);
        TxtAlert.Text = canBT > 0 ? $"⚠ {canBT} tiện nghi cần bảo trì/sửa chữa" : "";
        TxtAlert.Visibility = canBT > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnCapNhat_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;
        if (CboTrangThai.SelectedValue is not string maTrangThai) return;

        if (!ConfirmHelper.Confirm($"Bạn có chắc chắn muốn cập nhật trạng thái tiện nghi \"{item.TenTienNghi}\" tại phòng {maPhong}?", "Xác nhận cập nhật"))
            return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var svc = new TienNghiService(db);
            await svc.CapNhatTrangThaiAsync(maPhong, item.MaTienNghi, maTrangThai);
            CboPhong_SelectionChanged(CboPhong, null!);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGhiSuaChua_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;

        if (!decimal.TryParse(TxtChiPhiSuaChua.Text, out decimal cp) || cp <= 0)
        {
            ConfirmHelper.ShowWarning("Nhập số tiền chi phí hợp lệ.");
            return;
        }

        if (!ConfirmHelper.Confirm($"Bạn có muốn ghi nhận chi phí sửa chữa {cp:N0} ₫ cho tiện nghi \"{item.TenTienNghi}\" tại phòng {maPhong} không?", "Xác nhận ghi chi phí"))
            return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var chiPhiSvc = new ChiPhiService(db);
            var tienNghiSvc = new TienNghiService(db);

            await chiPhiSvc.GhiChiPhiAsync(
                maLoaiCP: "LCP003",
                tenChiPhi: $"Sửa {item.TenTienNghi} – Phòng {maPhong}",
                soTien: cp,
                maNhanVien: App.CurrentUser?.MaNhanVien,
                maPhong: maPhong);

            await tienNghiSvc.CapNhatTrangThaiAsync(maPhong, item.MaTienNghi, "TNTT03");

            TxtChiPhiSuaChua.Text = "";
            CboPhong_SelectionChanged(CboPhong, null!);

            MessageBox.Show("Đã ghi chi phí và cập nhật trạng thái.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}



