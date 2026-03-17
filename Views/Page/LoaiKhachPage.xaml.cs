using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class LoaiKhachRow
{
    public string MaLoaiKhach { get; set; } = "";
    public string TenLoaiKhach { get; set; } = "";
    public decimal NguongTichLuy { get; set; }
    public int SoKhachHang { get; set; }
    public string NguongText => NguongTichLuy.ToString("N0") + " ₫";
}

public partial class LoaiKhachPage : Page
{
    private List<LoaiKhachRow> _all = [];
    private LoaiKhachRow? _selected;
    private bool _isNew;

    public LoaiKhachPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _all = await db.LoaiKhaches
            .Select(lk => new LoaiKhachRow
            {
                MaLoaiKhach = lk.MaLoaiKhach,
                TenLoaiKhach = lk.TenLoaiKhach ?? "",
                NguongTichLuy = lk.NguongTichLuy ?? 0,
                SoKhachHang = lk.KhachHangs.Count(),
            })
            .OrderBy(lk => lk.NguongTichLuy)
            .ToListAsync();

        LkGrid.ItemsSource = _all;
        TxtTong.Text = $"{_all.Count} hạng khách";
    }

    private void LkGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LkGrid.SelectedItem is not LoaiKhachRow row) return;
        _selected = row;
        _isNew = false;
        _ = ShowFormAsync(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        LkGrid.SelectedItem = null;
        ShowFormNew();
    }

    private void ShowFormNew()
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = "Thêm hạng khách mới";
        TxtTen.Text = "";
        TxtNguong.Text = "0";
        KmList.ItemsSource = null;
        TxtNoKm.Visibility = Visibility.Visible;
        BtnXoa.IsEnabled = false;
    }

    private async Task ShowFormAsync(LoaiKhachRow row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = "Sửa hạng khách";
        TxtTen.Text = row.TenLoaiKhach;
        TxtNguong.Text = row.NguongTichLuy.ToString("N0");
        BtnXoa.IsEnabled = row.SoKhachHang == 0;

        // Load khuyến mãi liên kết
        using var db = new QuanLyKhachSanContext();
        var kms = await db.KhuyenMais
            .Where(k => k.MaLoaiKhach == row.MaLoaiKhach
                     && k.IsActive == true
                     && k.NgayKetThuc >= DateTime.Now)
            .ToListAsync();

        KmList.ItemsSource = kms;
        TxtNoKm.Visibility = kms.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên hạng.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtNguong.Text.Replace(",", "").Replace(".", ""),
            out decimal nguong);

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.LoaiKhaches
                    .OrderByDescending(lk => lk.MaLoaiKhach)
                    .Select(lk => lk.MaLoaiKhach)
                    .FirstOrDefaultAsync();

                db.LoaiKhaches.Add(new LoaiKhach
                {
                    MaLoaiKhach = MaHelper.Next("LK", lastMa),
                    TenLoaiKhach = ten,
                    NguongTichLuy = nguong,
                });
            }
            else if (_selected != null)
            {
                var lk = await db.LoaiKhaches.FindAsync(_selected.MaLoaiKhach);
                if (lk != null)
                {
                    lk.TenLoaiKhach = ten;
                    lk.NguongTichLuy = nguong;
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

        if (MessageBox.Show($"Xóa hạng \"{_selected.TenLoaiKhach}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var lk = await db.LoaiKhaches.FindAsync(_selected.MaLoaiKhach);
            if (lk != null) db.LoaiKhaches.Remove(lk);
            await db.SaveChangesAsync();

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