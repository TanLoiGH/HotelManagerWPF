using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views.Pages;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class MainWindow : Window
{
    private Border? _activeNavItem;

    public string WelcomeMessage => $"{AppSession.TenNhanVien ?? "Admin"}";
    public string MaQuyen => AppSession.MaQuyen ?? "";
    public string NgayHienTai => TimeHelper.GetVietnamTime().ToString("dddd, dd/MM/yyyy");

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        var mq = (AppSession.MaQuyen ?? "").Trim().ToUpper();
        bool isAdmin = mq == "ADMIN" || mq == "GIAM_DOC";

        if (AdminSection != null)
            AdminSection.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

        Nav_Dashboard(BtnDashboard, null!);
    }

    private void NavigateByService<TPage>(string title, Border? activeBtn) where TPage : Page
    {
        try
        {
            var page = App.ServiceProvider.GetRequiredService<TPage>();
            ContentFrame.Navigate(page);
            TxtPageTitle.Text = title;
            SetActiveNav(activeBtn);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chi tiết lỗi DI: {ex.Message}\n\nInnerException: {ex.InnerException?.Message}",
                "Lỗi hệ thống");
        }
    }

    private void SetActiveNav(Border? btn)
    {
        if (_activeNavItem != null) _activeNavItem.Background = Brushes.Transparent;
        _activeNavItem = btn;
        if (_activeNavItem != null)
            _activeNavItem.Background = (Brush)FindResource("Brush.NavSelected");
    }

    // Nav Events
    private void Nav_Dashboard(object sender, MouseButtonEventArgs e) =>
        NavigateByService<DashboardPage>("Dashboard", BtnDashboard);

    private void Nav_Phong(object sender, MouseButtonEventArgs e) =>
        NavigateByService<SoDoPhongPage>("Quản lý Phòng", sender as Border);

    private void Nav_QuanTriPhong(object sender, MouseButtonEventArgs e) =>
        NavigateByService<QuanTriPhongPage>("Thiết lập Phòng", sender as Border);

    private void Nav_KhachHang(object sender, MouseButtonEventArgs e) =>
        NavigateByService<KhachHangPage>("Khách Hàng", sender as Border);

    private void Nav_HoaDon(object sender, MouseButtonEventArgs e) =>
        NavigateByService<HoaDonPage>("Hoá Đơn", sender as Border);

    private void Nav_NhanVien(object sender, MouseButtonEventArgs e) =>
        NavigateByService<NhanVienPage>("Nhân Viên", sender as Border);

    private void Nav_NhaCungCap(object sender, MouseButtonEventArgs e) =>
        NavigateByService<NhaCungCapPage>("Nhà Cung Cấp", sender as Border);


    private void Nav_KhuyenMai(object sender, MouseButtonEventArgs e) =>
        NavigateByService<KhuyenMaiPage>("Khuyến Mãi", sender as Border);

    private void Nav_BaoCao(object sender, MouseButtonEventArgs e) =>
        NavigateByService<BaoCaoPage>("Báo Cáo", sender as Border);

    private void Nav_ChiPhi(object sender, MouseButtonEventArgs e) =>
        NavigateByService<ChiPhiPage>("Chi Phí", sender as Border);

    private void Nav_LoaiPhong(object sender, MouseButtonEventArgs e) =>
        NavigateByService<LoaiPhongPage>("Loại Phòng", sender as Border);

    private void Nav_DichVu(object sender, MouseButtonEventArgs e) =>
        NavigateByService<DichVuPage>("Dịch vụ", sender as Border);

    private void Nav_CaiDat(object sender, MouseButtonEventArgs e) =>
        NavigateByService<CaiDatPage>("Cài đặt", sender as Border);


    private void Nav_LoaiKhach(object sender, MouseButtonEventArgs e) =>
        NavigateByService<LoaiKhachPage>("Loại Khách", sender as Border);

    private void Nav_TienNghi(object sender, MouseButtonEventArgs e) =>
        NavigateByService<TienNghiPage>("Tiện Nghi", sender as Border);

    private void Nav_TienNghiDanhMuc(object sender, MouseButtonEventArgs e) =>
        NavigateByService<TienNghiDanhMucPage>("Danh Mục Tiện Nghi", sender as Border);

    private void Nav_TrangThaiTienNghi(object sender, MouseButtonEventArgs e) =>
        NavigateByService<TienNghiTrangThaiPage>("Trạng Thái Tiện Nghi", sender as Border);

    private void Nav_PhuongThucThanhToan(object sender, MouseButtonEventArgs e) =>
        NavigateByService<PhuongThucThanhToanPage>("Phương Thức Thanh Toán", sender as Border);

    private void Nav_LoaiChiPhi(object sender, MouseButtonEventArgs e) =>
        NavigateByService<LoaiChiPhiPage>("Loại Chi Phí", sender as Border);

    private void Nav_AboutPage(object sender, MouseButtonEventArgs e) =>
        NavigateByService<AboutPage>("About", sender as Border);

    #region Close/

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        var xacNhan = MessageBox.Show(
            "Bạn có chắc chắn muốn thoát ứng dụng không?",
            "Xác nhận thoát",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (xacNhan == MessageBoxResult.No)
        {
            // Hủy lệnh đóng cửa sổ (App vẫn sống nhăn răng)
            e.Cancel = true;
        }
        else
        {
            // (Tùy chọn) Thêm code dọn dẹp ở đây nếu cần trước khi app tắt hẳn
            // Ví dụ: AppSession.Clear(); 
        }
    }

    private void CloseWindow_Click(object sender, MouseButtonEventArgs e) => this.Close();

    #endregion


    private void TitleBar_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Bạn có muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
            MessageBoxResult.Yes)
        {
            AppSession.Clear();
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Application.Current.MainWindow?.Close();
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true)
            {
                var newMain = App.ServiceProvider.GetRequiredService<MainWindow>();

                // Gán Main mới và trả lại chế độ Shutdown mặc định
                Application.Current.MainWindow = newMain;
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // 7. Hiển thị
                newMain.Show();
            }
            else
            {
                // Nếu user tắt form Login mà không đăng nhập -> Tắt hẳn app
                Application.Current.Shutdown();
            }
        }
    }

    private void NavItem_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _activeNavItem) b.Background = (Brush)FindResource("Brush.NavHover");
    }

    private void NavItem_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _activeNavItem) b.Background = Brushes.Transparent;
    }
}