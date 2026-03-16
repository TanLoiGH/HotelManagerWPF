using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Views;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class MainWindow : Window
{
    private Border? _activeNavItem;

    public MainWindow()
    {
        InitializeComponent();
        // Load Dashboard mặc định
        NavigateTo(new DashboardPage(), "Dashboard", BtnDashboard);
    }

    // ── Kéo cửa sổ ──────────────────────────────────────────────────────────
    private void TitleBar_DragMove(object sender, MouseButtonEventArgs e) => DragMove();

    private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Bạn có muốn đóng ứng dụng?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Application.Current.Shutdown();
    }

    // ── Đăng xuất ───────────────────────────────────────────────────────────
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        AppSession.Clear();
        var login = new LoginWindow();
        login.Show();
        Close();
    }

    // ── Navigation helpers ───────────────────────────────────────────────────
    private void NavigateTo(Page page, string title, Border? activeBtn = null)
    {
        ContentFrame.Navigate(page);
        TxtPageTitle.Text = title;
        SetActiveNav(activeBtn);
    }

    private void SetActiveNav(Border? btn)
    {
        // Reset màu item cũ
        if (_activeNavItem != null)
            _activeNavItem.Background = new SolidColorBrush(Colors.Transparent);

        _activeNavItem = btn;

        // Highlight item mới
        if (_activeNavItem != null)
            _activeNavItem.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1E4D8C")!);
    }

    // ── Nav events ──────────────────────────────────────────────────────────
    private void Nav_Dashboard(object sender, MouseButtonEventArgs e)
        => NavigateTo(new DashboardPage(), "Dashboard", BtnDashboard);

    private void Nav_Phong(object sender, MouseButtonEventArgs e)
        => NavigateTo(new PhongPage(), "Quản lý Phòng", sender as Border);

    private void Nav_DatPhong(object sender, MouseButtonEventArgs e)
        => NavigateTo(new DatPhongPage(), "Đặt Phòng", sender as Border);

    private void Nav_KhachHang(object sender, MouseButtonEventArgs e)
        => NavigateTo(new KhachHangPage(), "Khách Hàng", sender as Border);

    private void Nav_HoaDon(object sender, MouseButtonEventArgs e)
        => NavigateTo(new HoaDonPage(), "Hoá Đơn", sender as Border);

    private void Nav_ThanhToan(object sender, MouseButtonEventArgs e)
        => NavigateTo(new ThanhToanPage(), "Thanh Toán", sender as Border);

    private void Nav_DichVu(object sender, MouseButtonEventArgs e)
        => NavigateTo(new DichVuPage(), "Dịch Vụ", sender as Border);

    private void Nav_NhanVien(object sender, MouseButtonEventArgs e)
        => NavigateTo(new NhanVienPage(), "Nhân Viên", sender as Border);

    private void Nav_NhaCungCap(object sender, MouseButtonEventArgs e)
        => NavigateTo(new NhaCungCapPage(), "Nhà Cung Cấp", sender as Border);

    // ── Hover effects cho nav items ─────────────────────────────────────────
    private void NavItem_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _activeNavItem)
            b.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#142E5C")!);
    }

    private void NavItem_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _activeNavItem)
            b.Background = new SolidColorBrush(Colors.Transparent);
    }
}
