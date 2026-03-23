using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class KhachHangPage : Page
{
    private List<KhachHangViewModel> _all = [];
    private KhachHangViewModel? _selected;
    private bool _isNew;

    public KhachHangPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();

        var loaiKhach = await db.LoaiKhaches.ToListAsync();
        CboLoaiKhach.ItemsSource = loaiKhach;
        CboLoaiKhach.DisplayMemberPath = "TenLoaiKhach";
        CboLoaiKhach.SelectedValuePath = "MaLoaiKhach";

        _all = await db.KhachHangs
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
                TongTichLuy = k.TongTichLuy ?? 0,
            })
            .ToListAsync();

        KhachGrid.ItemsSource = _all;
        TxtTongKhach.Text = $"{_all.Count} khách hàng";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(k =>
                k.TenKhachHang.ToLower().Contains(kw) ||
                k.DienThoai.Contains(kw) ||
                k.Cccd.Contains(kw)).ToList();
        KhachGrid.ItemsSource = filtered;
        TxtTongKhach.Text = $"{filtered.Count} khách hàng";
    }

    private void KhachGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KhachGrid.SelectedItem is not KhachHangViewModel row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        KhachGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(KhachHangViewModel? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm khách hàng mới" : "Sửa khách hàng";
        TxtTen.Text = row?.TenKhachHang ?? "";
        TxtDienThoai.Text = row?.DienThoai ?? "";
        TxtCCCD.Text = row?.Cccd ?? "";
        TxtEmail.Text = "";
        TxtDiaChi.Text = "";
        BtnXoa.IsEnabled = row != null;

        // Load thêm email/diachi nếu sửa
        if (row != null) _ = LoadExtraAsync(row.MaKhachHang);
        else CboLoaiKhach.SelectedIndex = 0;
    }

    private async Task LoadExtraAsync(string maKH)
    {
        using var db = new QuanLyKhachSanContext();
        var kh = await db.KhachHangs.FindAsync(maKH);
        if (kh == null) return;
        TxtEmail.Text = kh.Email ?? "";
        TxtDiaChi.Text = kh.DiaChi ?? "";
        CboLoaiKhach.SelectedValue = kh.MaLoaiKhach;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập họ tên.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.KhachHangs
                    .OrderByDescending(k => k.MaKhachHang)
                    .Select(k => k.MaKhachHang)
                    .FirstOrDefaultAsync();

                db.KhachHangs.Add(new KhachHang
                {
                    MaKhachHang = MaHelper.Next("KH", lastMa),
                    TenKhachHang = ten,
                    DienThoai = TxtDienThoai.Text.Trim(),
                    Cccd = TxtCCCD.Text.Trim(),
                    Email = TxtEmail.Text.Trim(),
                    DiaChi = TxtDiaChi.Text.Trim(),
                    MaLoaiKhach = CboLoaiKhach.SelectedValue as string ?? "LK001",
                    TongTichLuy = 0,
                });
            }
            else if (_selected != null)
            {
                var kh = await db.KhachHangs.FindAsync(_selected.MaKhachHang);
                if (kh != null)
                {
                    kh.TenKhachHang = ten;
                    kh.DienThoai = TxtDienThoai.Text.Trim();
                    kh.Cccd = TxtCCCD.Text.Trim();
                    kh.Email = TxtEmail.Text.Trim();
                    kh.DiaChi = TxtDiaChi.Text.Trim();
                    kh.MaLoaiKhach = CboLoaiKhach.SelectedValue as string;
                }
            }

            await db.SaveChangesAsync();
            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnXoa_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        if (MessageBox.Show($"Xóa khách hàng \"{_selected.TenKhachHang}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var kh = await db.KhachHangs.FindAsync(_selected.MaKhachHang);
            if (kh != null)
            {
                bool coDatPhong = await db.DatPhongs.AnyAsync(d => d.MaKhachHang == kh.MaKhachHang);
                if (coDatPhong)
                {
                    MessageBox.Show("Không thể xóa — khách hàng đã có lịch sử đặt phòng.",
                        "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                db.KhachHangs.Remove(kh);
                await db.SaveChangesAsync();
            }

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}


