// TienNghiPage.xaml.cs — thay toàn bộ
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

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

        // Load CboPhong
        var phongs = await db.Phongs
            .OrderBy(p => p.MaPhong)
            .Select(p => new { p.MaPhong })
            .ToListAsync();
        CboPhong.ItemsSource = phongs;
        CboPhong.DisplayMemberPath = "MaPhong";
        CboPhong.SelectedValuePath = "MaPhong";

        // Load CboTrangThai
        var trangThais = await db.TienNghiTrangThais.ToListAsync();
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
            MessageBox.Show("Nhập số tiền chi phí hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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