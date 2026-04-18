using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        var roomSvc = new PhongService(db);
        var tnSvc = new TienNghiService(db);

        var phongs = await roomSvc.LayDanhSachMaPhongAsync();
        CboPhong.ItemsSource = phongs;

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
        TxtAlert.Text = canBT > 0 ? $"⚠ Phát hiện {canBT} tiện nghi đang cần được bảo trì, sửa chữa hoặc thay mới ngay." : "";
        TxtAlert.Visibility = canBT > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TienNghiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is TienNghiPhongViewModel item)
        {
            CboTrangThai.SelectedValue = item.MaTrangThai;

            // LOGIC UI: Chỉ bật ComboBox nếu tiện nghi đang "Hoạt động tốt" (TNTT01)
            bool isHoatDongTot = item.MaTrangThai == "TNTT01";
            CboTrangThai.IsEnabled = isHoatDongTot;

            if (item.CanBaoTri)
            {
                TxtChiPhi.Focus();
            }
        }
    }

    private async void BtnCapNhat_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;
        if (CboTrangThai.SelectedValue is not string maTrangThai) return;

        // Chặn lưu cập nhật nếu trạng thái hiện tại không phải là "Hoạt động tốt"
        if (item.MaTrangThai != "TNTT01")
        {
            ConfirmHelper.ShowWarning("Chỉ được điều chỉnh trạng thái khi tiện nghi đang 'Hoạt động tốt'.\nNếu tiện nghi đang hỏng/bảo trì, vui lòng Ghi chi phí xử lý (Sửa chữa/Thay mới) để hệ thống tự động phục hồi.");
            return;
        }

        if (!ConfirmHelper.Confirm($"Đổi trạng thái tiện nghi \"{item.TenTienNghi}\" tại phòng {maPhong}?", "Xác nhận"))
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
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TienNghiGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;

        // Chặn lật trạng thái nhanh nếu tiện nghi không phải là "Hoạt động tốt"
        if (item.MaTrangThai != "TNTT01")
        {
            ConfirmHelper.ShowWarning("Tiện nghi này đang có sự cố. Vui lòng sử dụng chức năng Ghi chi phí để hoàn tất sửa chữa/thay mới.");
            return;
        }

        // Tự động lật trạng thái sang Cần bảo trì (TNTT02)
        using var db = new QuanLyKhachSanContext();
        var svc = new TienNghiService(db);
        await svc.CapNhatTrangThaiAsync(maPhong, item.MaTienNghi, "TNTT02");
        CboPhong_SelectionChanged(CboPhong, null!);
    }

    private async void BtnGhiXuLy_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item)
        {
            ConfirmHelper.ShowWarning("Vui lòng chọn một tiện nghi trong danh sách (click vào lưới bên dưới) để xử lý.");
            return;
        }
        if (CboPhong.SelectedValue is not string maPhong) return;

        if (!decimal.TryParse(TxtChiPhi.Text, out decimal cp) || cp <= 0)
        {
            ConfirmHelper.ShowWarning("Vui lòng nhập số tiền chi phí hợp lệ lớn hơn 0.");
            return;
        }

        string loaiXuLy = (CboLoaiXuLy.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Sửa chữa";
        string actionText = loaiXuLy.ToLower();

        if (!ConfirmHelper.Confirm($"Xác nhận ghi chi phí {actionText} tiện nghi \"{item.TenTienNghi}\" tại phòng {maPhong} với số tiền {cp:N0} VNĐ?", "Xác nhận chi phí"))
            return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var chiPhiSvc = new ChiPhiService(db);
            var tienNghiSvc = new TienNghiService(db);

            // Ghi vào module Chi phí (LCP003: Chi phí bảo trì, mua sắm)
            await chiPhiSvc.GhiChiPhiAsync(
                maLoaiCP: "LCP003",
                tenChiPhi: $"[{loaiXuLy}] {item.TenTienNghi} – Phòng {maPhong}",
                soTien: cp,
                maNhanVien: AppSession.MaNhanVien,
                maPhong: maPhong,
                maNCC: item.MaNcc);

            // Sau khi sửa chữa / thay mới thành công -> Đưa trạng thái về TNTT01 (Hoạt động tốt)
            await tienNghiSvc.CapNhatTrangThaiAsync(maPhong, item.MaTienNghi, "TNTT01");

            TxtChiPhi.Text = "";
            CboPhong_SelectionChanged(CboPhong, null!);

            MessageBox.Show($"Đã ghi nhận chi phí và cập nhật trạng thái tiện nghi hoàn tất.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}