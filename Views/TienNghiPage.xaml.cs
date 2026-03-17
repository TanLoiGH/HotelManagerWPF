// ===========================================================================
// TienNghiPage.xaml.cs  –  Quản lý TIEN_NGHI_PHONG + cập nhật trạng thái
// ===========================================================================
using QuanLyKhachSan_PhamTanLoi;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;
using System.Windows.Controls;

public partial class TienNghiPage : Page
{
    private readonly TienNghiService _tienNghiSvc;

    public TienNghiPage()
    {
        InitializeComponent();
        using var db = new QuanLyKhachSanContext();
        _tienNghiSvc = new TienNghiService(db);
    }

    // Load tiện nghi của phòng được chọn
    private async void CboPhong_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboPhong.SelectedValue is not string maPhong) return;

        // TIEN_NGHI_PHONG + TIEN_NGHI + NHA_CUNG_CAP + TIEN_NGHI_TRANG_THAI
        var items = await _tienNghiSvc.GetTienNghiPhongAsync(maPhong);
        TienNghiGrid.ItemsSource = items;

        // Cảnh báo cần bảo trì
        int canBT = items.Count(i => i.CanBaoTri);
        TxtAlert.Text = canBT > 0 ? $"⚠ {canBT} tiện nghi cần bảo trì/sửa chữa" : "";
        TxtAlert.Visibility = canBT > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Cập nhật trạng thái tiện nghi (TIEN_NGHI_PHONG – chỉ UPDATE, không INSERT lại)
    private async void BtnCapNhat_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;
        if (CboTrangThai.SelectedValue is not string maTrangThai) return;

        try
        {
            await _tienNghiSvc.CapNhatTrangThaiAsync(
                maPhong, item.MaTienNghi, maTrangThai);

            // Refresh
            CboPhong_SelectionChanged(CboPhong, null!);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Ghi chi phí sửa chữa liên kết với phòng (CHI_PHI + LOAI_CHI_PHI)
    private async void BtnGhiSuaChua_Click(object sender, RoutedEventArgs e)
    {
        if (TienNghiGrid.SelectedItem is not TienNghiPhongViewModel item) return;
        if (CboPhong.SelectedValue is not string maPhong) return;

        using var db = new QuanLyKhachSanContext();
        var chiPhiSvc = new ChiPhiService(db);

        await chiPhiSvc.GhiChiPhiAsync(
            maLoaiCP: "LCP003",  // Sửa chữa - Bảo trì
            tenChiPhi: $"Sửa {item.TenTienNghi} – Phòng {maPhong}",
            soTien: decimal.TryParse(TxtChiPhiSuaChua.Text, out var cp) ? cp : 0,
            maNhanVien: App.CurrentUser?.MaNhanVien,
            maNCC: item.TenNCC != "—" ? null : null,  // TODO: bind MaNCC
            maPhong: maPhong);

        // Cập nhật trạng thái tiện nghi → Đang sửa chữa
        await _tienNghiSvc.CapNhatTrangThaiAsync(maPhong, item.MaTienNghi, "TNTT03");
        CboPhong_SelectionChanged(CboPhong, null!);
    }
}