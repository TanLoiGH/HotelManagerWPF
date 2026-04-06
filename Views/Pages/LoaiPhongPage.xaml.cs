using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

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
        var loaiPhongSvc = new LoaiPhongService(db);
        var items = await loaiPhongSvc.LayDanhSachAsync();

        _all = items.Select(lp => new LoaiPhongRow
        {
            MaLoaiPhong = lp.MaLoaiPhong,
            TenLoaiPhong = lp.TenLoaiPhong,
            SoNguoiToiDa = lp.SoNguoiToiDa,
            GiaPhong = lp.GiaPhong,
            SoPhong = lp.SoPhong,
        }).ToList();

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

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var loaiPhongSvc = new LoaiPhongService(db);

            if (_isNew)
            {
                await loaiPhongSvc.TaoMoiAsync(
                    ten,
                    soNguoi > 0 ? soNguoi : null,
                    gia);
            }
            else if (_selected != null)
            {
                await loaiPhongSvc.CapNhatAsync(
                    _selected.MaLoaiPhong,
                    ten,
                    soNguoi > 0 ? soNguoi : null,
                    gia);
            }
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

        if (!ConfirmHelper.ConfirmDelete(_selected.TenLoaiPhong)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var loaiPhongSvc = new LoaiPhongService(db);
            await loaiPhongSvc.XoaAsync(_selected.MaLoaiPhong);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}


