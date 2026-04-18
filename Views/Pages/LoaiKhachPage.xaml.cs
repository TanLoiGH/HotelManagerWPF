using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

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
        var loaiKhachSvc = new LoaiKhachService(db);
        var items = await loaiKhachSvc.LayDanhSachAsync();
        _all = items.Select(lk => new LoaiKhachRow
        {
            MaLoaiKhach = lk.MaLoaiKhach,
            TenLoaiKhach = lk.TenLoaiKhach,
            NguongTichLuy = lk.NguongTichLuy,
            SoKhachHang = lk.SoKhach,
        }).ToList();

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
        var kmSvc = new KhuyenMaiService(db);
        var kms = await kmSvc.LayKhuyenMaiConHieuLucTheoLoaiKhachAsync(row.MaLoaiKhach);

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

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var loaiKhachSvc = new LoaiKhachService(db);

            if (_isNew)
            {
                await loaiKhachSvc.TaoMoiAsync(ten, nguong);
            }
            else if (_selected != null)
            {
                await loaiKhachSvc.CapNhatAsync(_selected.MaLoaiKhach, ten, nguong);
            }
            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnXoa_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        if (!ConfirmHelper.ConfirmDelete(_selected.TenLoaiKhach)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var loaiKhachSvc = new LoaiKhachService(db);
            await loaiKhachSvc.XoaAsync(_selected.MaLoaiKhach);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}


