// ===========================================================================
// DashboardPage.xaml.cs  –  Tổng hợp từ TẤT CẢ bảng quan trọng
// ===========================================================================

using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardService _dashboardSvc;
    private readonly ChiPhiService _chiPhiSvc;

    public DashboardPage()
    {
        InitializeComponent();
        using var db = new QuanLyKhachSanContext();
        _dashboardSvc = new DashboardService(db);
        _chiPhiSvc = new ChiPhiService(db);
        Loaded += async (_, _) => await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        var tuNgay = DpTuNgay.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var denNgay = DpDenNgay.SelectedDate ?? DateTime.Today;

        try
        {
            var data = await _dashboardSvc.GetDashboardAsync(tuNgay, denNgay);

            // KPIs
            TxtDoanhThu.Text = data.DoanhThuText;
            TxtChiPhi.Text = data.TongChiPhiText;
            TxtLoiNhuan.Text = data.LoiNhuanText;
            TxtLoiNhuan.Foreground = data.LoiNhuan >= 0
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            // Biểu đồ trạng thái phòng (binding sang ItemsControl)
            PhongStatusItems.ItemsSource = data.PhongStats
                .Select(kv => new { TenTT = kv.Key, SoPhong = kv.Value })
                .ToList();

            // Hạng khách hàng
            KhachHangItems.ItemsSource = data.KhachStats
                .Select(kv => new { Hang = kv.Key, SoKhach = kv.Value })
                .ToList();

            // Top dịch vụ
            TopDichVuItems.ItemsSource = data.TopDichVu
                .Select(kv => new { TenDV = kv.Key, SoLan = kv.Value })
                .ToList();

            // Chi phí theo loại
            ChiPhiItems.ItemsSource = data.ChiPhiByLoai;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dashboard: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await LoadDashboardAsync();

    // Ghi chi phí mới (CHI_PHI + LOAI_CHI_PHI)
    private async void BtnGhiChiPhi_Click(object sender, RoutedEventArgs e)
    {
        var loaiCPs = await _chiPhiSvc.GetLoaiChiPhiAsync();
        var dialog = new GhiChiPhiDialog(loaiCPs)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var r = dialog.Result.Value;
            await _chiPhiSvc.GhiChiPhiAsync(
                r.MaLoaiCP, r.TenChiPhi, r.SoTien,
                App.CurrentUser?.MaNhanVien,
                r.MaNCC, r.MaPhong, r.GhiChu);

            await LoadDashboardAsync();
        }
    }
}