using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class MainWindow : Window
{
    private Border? _activeNavItem;
    public string WelcomeMessage => $"Xin chào, {AppSession.TenNhanVien ?? "Admin"}";
    public string MaQuyen => AppSession.MaQuyen ?? "";
    public string NgayHienTai => DateTime.Now.ToString("dddd, dd/MM/yyyy");

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        NavigateTo(new DashboardPage(), "Dashboard", BtnDashboard);
    }
    private void CloseWindow_Click(object sender, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Bạn có muốn đóng ứng dụng?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Application.Current.Shutdown();
    }
    // ── Kéo / đóng cửa sổ ───────────────────────────────────────────────────
    private void TitleBar_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }



    // ── Đăng xuất ───────────────────────────────────────────────────────────
    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        // Xác nhận trước
        if (MessageBox.Show("Bạn có muốn đăng xuất?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        // Xóa session
        AppSession.Clear();
        App.CurrentUser = null;

        // Chuyển sang OnExplicitShutdown để tránh app tắt khi MainWindow đóng
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Đóng MainWindow trước
        Close();

        // Mở LoginWindow
        var login = new LoginWindow();
        bool? result = login.ShowDialog();

        if (App.CurrentUser != null)
        {
            // Đăng nhập lại thành công → mở MainWindow mới
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var main = new MainWindow();
            main.Show();
        }
        else
        {
            // Đóng dialog mà không login → tắt app
            Application.Current.Shutdown();
        }
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
        if (_activeNavItem != null)
            _activeNavItem.Background = new SolidColorBrush(Colors.Transparent);
        _activeNavItem = btn;
        if (_activeNavItem != null)
            _activeNavItem.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1E4D8C")!);
    }

    // ── Nav events — Page navigation ────────────────────────────────────────
    private void Nav_Dashboard(object sender, MouseButtonEventArgs e)
        => NavigateTo(new DashboardPage(), "Dashboard", BtnDashboard);

    private void Nav_Phong(object sender, MouseButtonEventArgs e)
        => NavigateTo(new PhongPage(), "Quản lý Phòng", sender as Border);

    private void Nav_KhachHang(object sender, MouseButtonEventArgs e)
        => NavigateTo(new KhachHangPage(), "Khách Hàng", sender as Border);

    private void Nav_HoaDon(object sender, MouseButtonEventArgs e)
        => NavigateTo(new HoaDonPage(), "Hoá Đơn", sender as Border);

    private void Nav_NhanVien(object sender, MouseButtonEventArgs e)
        => NavigateTo(new NhanVienPage(), "Nhân Viên", sender as Border);

    private void Nav_NhaCungCap(object sender, MouseButtonEventArgs e)
        => NavigateTo(new NhaCungCapPage(), "Nhà Cung Cấp", sender as Border);

    private void Nav_TienNghi(object sender, MouseButtonEventArgs e)
        => NavigateTo(new TienNghiPage(), "Tiện Nghi", sender as Border);

    private void Nav_KhuyenMai(object sender, MouseButtonEventArgs e)
        => NavigateTo(new KhuyenMaiPage(), "Khuyến Mãi", sender as Border);

    private void Nav_ChiPhi(object sender, MouseButtonEventArgs e)
        => NavigateTo(new ChiPhiPage(), "Chi Phí", sender as Border);

    private void Nav_LoaiPhong(object sender, MouseButtonEventArgs e)
    => NavigateTo(new LoaiPhongPage(), "Loại Phòng", sender as Border);

    private void Nav_LoaiKhach(object sender, MouseButtonEventArgs e)
        => NavigateTo(new LoaiKhachPage(), "Hạng Khách Hàng", sender as Border);
    // ── Nav events — Dialog popup (cần tham số, không navigate trực tiếp) ───
    // Các menu này mở PhongPage trước, user chọn phòng rồi dialog tự mở
    private void Nav_DatPhong(object sender, MouseButtonEventArgs e)
        => NavigateTo(new PhongPage(), "Quản lý Phòng — Chọn phòng để đặt", sender as Border);

    private void Nav_ThanhToan(object sender, MouseButtonEventArgs e)
        => NavigateTo(new HoaDonPage(), "Hoá Đơn — Chọn hóa đơn để thanh toán", sender as Border);

    private void Nav_DichVu(object sender, MouseButtonEventArgs e)
        => NavigateTo(new HoaDonPage(), "Hoá Đơn — Chọn hóa đơn để thêm dịch vụ", sender as Border);

    // ── Hover effects ────────────────────────────────────────────────────────
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