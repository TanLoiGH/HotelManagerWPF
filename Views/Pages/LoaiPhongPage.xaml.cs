using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class LoaiPhongRow
{
    public string MaLoaiPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public int? SoNguoiToiDa { get; set; }
    public decimal GiaPhong { get; set; }
    public int SoPhong { get; set; }
    public string SoNguoiText => $"{SoNguoiToiDa ?? 0} người";
    public string GiaPhongText => GiaPhong.ToString("N0") + " ₫";
}

public partial class LoaiPhongPage : Page
{
    private List<LoaiPhongRow> _all = [];
    private LoaiPhongRow? _selected;
    private bool _isNew;

    public LoaiPhongPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _all = await db.LoaiPhongs
            .Select(lp => new LoaiPhongRow
            {
                MaLoaiPhong = lp.MaLoaiPhong,
                TenLoaiPhong = lp.TenLoaiPhong ?? "",
                SoNguoiToiDa = lp.SoNguoiToiDa,
                GiaPhong = lp.GiaPhong,
                SoPhong = lp.Phongs.Count(),
            })
            .OrderBy(lp => lp.MaLoaiPhong)
            .ToListAsync();

        LpGrid.ItemsSource = _all;
        TxtTong.Text = $"{_all.Count} loại phòng";
    }

    private void LpGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LpGrid.SelectedItem is not LoaiPhongRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        LpGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(LoaiPhongRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm loại phòng mới" : "Sửa loại phòng";
        TxtTen.Text = row?.TenLoaiPhong ?? "";
        TxtSoNguoi.Text = row?.SoNguoiToiDa?.ToString() ?? "2";
        TxtGia.Text = row?.GiaPhong.ToString("N0") ?? "";
        TxtPhongList.Text = row != null
            ? $"Có {row.SoPhong} phòng thuộc loại này" : "";
        BtnXoa.IsEnabled = row != null && row.SoPhong == 0;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên loại phòng.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string giaRaw = TxtGia.Text.Replace(",", "").Replace(".", "").Trim();
        if (!decimal.TryParse(giaRaw, out decimal gia) || gia <= 0)
        {
            MessageBox.Show("Giá phòng không hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(TxtSoNguoi.Text, out int soNguoi);

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.LoaiPhongs
                    .OrderByDescending(lp => lp.MaLoaiPhong)
                    .Select(lp => lp.MaLoaiPhong)
                    .FirstOrDefaultAsync();

                db.LoaiPhongs.Add(new LoaiPhong
                {
                    MaLoaiPhong = MaHelper.Next("LP", lastMa),
                    TenLoaiPhong = ten,
                    SoNguoiToiDa = soNguoi > 0 ? soNguoi : null,
                    GiaPhong = gia,
                });
            }
            else if (_selected != null)
            {
                var lp = await db.LoaiPhongs.FindAsync(_selected.MaLoaiPhong);
                if (lp != null)
                {
                    lp.TenLoaiPhong = ten;
                    lp.SoNguoiToiDa = soNguoi > 0 ? soNguoi : null;
                    lp.GiaPhong = gia;
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

        if (MessageBox.Show($"Xóa loại phòng \"{_selected.TenLoaiPhong}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var lp = await db.LoaiPhongs.FindAsync(_selected.MaLoaiPhong);
            if (lp != null) db.LoaiPhongs.Remove(lp);
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


