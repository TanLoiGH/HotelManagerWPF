using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class SoDoPhongPage : Page
{
    private readonly SoDoPhongViewModel _viewModel;
    private readonly QuanLyKhachSanContext _db;

    public SoDoPhongPage()
    {
        InitializeComponent();

        _db = new QuanLyKhachSanContext();

        var roomService = new PhongService(_db);
        var khachHangService = new KhachHangService(_db);
        var datPhongService = new DatPhongService(_db);

        _viewModel = new SoDoPhongViewModel(roomService, khachHangService, datPhongService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.TaiDuLieuAsync();
        Unloaded += (_, _) =>
        {
            _db.Dispose();
        };
    }

    private void PhongCard_Click(object sender, MouseButtonEventArgs e)
    {
        var card = sender as FrameworkElement;
        var vm = card?.Tag as PhongCardViewModel;
        if (vm == null) return;

        // Single-click: chỉ chọn phòng để load panel chi tiết, không clear multi-select.
        _viewModel.SelectedRoom = vm;

        // Double-click vào vùng trống của card (không phải checkbox): toggle chọn nhiều cho phòng Trống.
        if (e.ClickCount >= 2 && !IsClickFrom<CheckBox>(e.OriginalSource) && vm.MaTrangThaiPhong == PhongTrangThaiCodes.Trong)
        {
            vm.IsSelected = !vm.IsSelected;
            e.Handled = true;
        }
    }

    private static bool IsClickFrom<T>(object? originalSource) where T : DependencyObject
    {
        var current = originalSource as DependencyObject;
        while (current != null)
        {
            if (current is T) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedBtn && FilterPanel != null)
        {
            foreach (var child in FilterPanel.Children)
            {
                if (child is Button btn)
                {
                    // Nút được click sẽ sáng lên và viền dày hơn, các nút khác mờ đi
                    btn.Opacity = (btn == clickedBtn) ? 1.0 : 0.4;
                    btn.BorderThickness = (btn == clickedBtn) ? new Thickness(2) : new Thickness(1);
                }
            }
        }
    }


}
