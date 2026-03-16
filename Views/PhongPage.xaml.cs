using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

// ViewModel cho một card phòng
public class PhongCardViewModel
{
    public string MaPhong        { get; set; } = "";
    public string TenLoaiPhong   { get; set; } = "";
    public string TenTrangThai   { get; set; } = "";
    public string MaTrangThaiPhong { get; set; } = "";
    public int    SoNguoiToiDa   { get; set; }
    public decimal GiaPhong      { get; set; }
    public string GiaPhongText   => GiaPhong.ToString("N0", new CultureInfo("vi-VN")) + " ₫";

    public SolidColorBrush CardBackground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(0,  184, 148)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(225,112, 85)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(243,156,  18)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(127,140, 141)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(108, 92, 231)),
        _       => new SolidColorBrush(Color.FromRgb(99, 110, 114)),
    };

    public Color ShadowColor => MaTrangThaiPhong switch
    {
        "PTT01" => Color.FromRgb(0,  184, 148),
        "PTT02" => Color.FromRgb(225,112, 85),
        "PTT03" => Color.FromRgb(243,156,  18),
        "PTT05" => Color.FromRgb(108, 92, 231),
        _       => Color.FromRgb(0, 120, 212),
    };
}

public class TienNghiItem
{
    public string TenTienNghi { get; set; } = "";
}

public partial class PhongPage : Page
{
    private List<PhongCardViewModel> _allPhong = new();
    private string _currentFilter = "all";
    private PhongCardViewModel? _selectedPhong;

    public PhongPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadPhongAsync();
    }

    // ── Load dữ liệu từ DB ───────────────────────────────────────────────
    private async Task LoadPhongAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();

            _allPhong = await db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Include(p => p.MaTrangThaiPhongNavigation)
                .OrderBy(p => p.MaPhong)
                .Select(p => new PhongCardViewModel
                {
                    MaPhong           = p.MaPhong,
                    TenLoaiPhong      = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                    TenTrangThai      = p.MaTrangThaiPhongNavigation != null
                                        ? p.MaTrangThaiPhongNavigation.TenTrangThai ?? "" : "",
                    MaTrangThaiPhong  = p.MaTrangThaiPhong ?? "PTT01",
                    SoNguoiToiDa      = p.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0,
                    GiaPhong          = p.MaLoaiPhongNavigation.GiaPhong,
                })
                .ToListAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không tải được danh sách phòng: {ex.Message}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ── Filter ────────────────────────────────────────────────────────────
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _currentFilter = btn.Tag?.ToString() ?? "all";
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void ApplyFilter()
    {
        var keyword = TxtSearch?.Text?.Trim().ToLower() ?? "";

        var filtered = _allPhong.AsEnumerable();

        if (_currentFilter != "all")
            filtered = filtered.Where(p => p.MaTrangThaiPhong == _currentFilter);

        if (!string.IsNullOrEmpty(keyword))
            filtered = filtered.Where(p =>
                p.MaPhong.ToLower().Contains(keyword) ||
                p.TenLoaiPhong.ToLower().Contains(keyword));

        var list = filtered.ToList();
        PhongList.ItemsSource = list;
        TxtRoomCount.Text = $"{list.Count} phòng";
    }

    // ── Click card phòng → hiện detail ───────────────────────────────────
    private async void PhongCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border b || b.Tag is not PhongCardViewModel vm) return;
        _selectedPhong = vm;
        await ShowDetailAsync(vm);
    }

    private async Task ShowDetailAsync(PhongCardViewModel vm)
    {
        // Header card
        DetailMaPhong.Text    = vm.MaPhong;
        DetailLoaiPhong.Text  = vm.TenLoaiPhong;
        DetailTrangThai.Text  = vm.TenTrangThai;

        // Info rows
        InfoLoaiPhong.Text = vm.TenLoaiPhong;
        InfoSoNguoi.Text   = $"{vm.SoNguoiToiDa} người";
        InfoGia.Text       = vm.GiaPhongText + " / đêm";

        // Nút đặt phòng chỉ hiện khi phòng trống
        BtnDatPhong.IsEnabled  = vm.MaTrangThaiPhong == "PTT01";
        BtnDatPhong.Opacity    = BtnDatPhong.IsEnabled ? 1.0 : 0.4;

        // Tiện nghi
        try
        {
            using var db = new QuanLyKhachSanContext();
            var tienNghis = await db.TienNghiPhongs
                .Include(tnp => tnp.MaTienNghiNavigation)
                .Where(tnp => tnp.MaPhong == vm.MaPhong)
                .Select(tnp => new TienNghiItem
                {
                    TenTienNghi = tnp.MaTienNghiNavigation.TenTienNghi
                })
                .ToListAsync();

            TienNghiList.ItemsSource = tienNghis;
            TxtNoTienNghi.Visibility = tienNghis.Any()
                ? Visibility.Collapsed : Visibility.Visible;
        }
        catch
        {
            TxtNoTienNghi.Visibility = Visibility.Visible;
        }

        // Toggle panel
        PanelEmpty.Visibility  = Visibility.Collapsed;
        PanelDetail.Visibility = Visibility.Visible;
    }

    // ── Nút Đặt Phòng ────────────────────────────────────────────────────
    private void BtnDatPhong_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPhong == null) return;

        // TODO: Navigate to DatPhongPage với phòng đã chọn
        MessageBox.Show($"Chuyển sang trang Đặt Phòng cho {_selectedPhong.MaPhong}",
            "Đặt phòng", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
