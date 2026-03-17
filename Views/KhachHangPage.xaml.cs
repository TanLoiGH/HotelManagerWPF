using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class KhachHangPage : Page
{
    private List<KhachHangViewModel> _allKhach = [];

    public KhachHangPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();
            _allKhach = await db.KhachHangs
                .Include(k => k.MaLoaiKhachNavigation)
                .OrderByDescending(k => k.TongTichLuy)
                .Select(k => new KhachHangViewModel
                {
                    MaKhachHang = k.MaKhachHang,
                    TenKhachHang = k.TenKhachHang,
                    DienThoai = k.DienThoai ?? "",
                    Cccd = k.Cccd ?? "",
                    TenLoaiKhach = k.MaLoaiKhachNavigation != null
                                   ? k.MaLoaiKhachNavigation.TenLoaiKhach ?? "" : "",
                    TongTichLuy = k.TongTichLuy ?? 0
                })
                .ToListAsync();

            KhachGrid.ItemsSource = _allKhach;
            TxtTongKhach.Text = _allKhach.Count.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải khách hàng: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(kw)
            ? _allKhach
            : _allKhach.Where(k =>
                k.TenKhachHang.ToLower().Contains(kw) ||
                k.DienThoai.Contains(kw) ||
                k.Cccd.Contains(kw)).ToList();

        KhachGrid.ItemsSource = filtered;
        TxtTongKhach.Text = filtered.Count.ToString();
    }

    private void KhachGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
}