using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class LoaiChiPhiRow
{
    public string MaLoaiCp { get; set; } = "";
    public string TenLoaiCp { get; set; } = "";
    public int SoPhieu { get; set; }
}

public partial class LoaiChiPhiPage : Page
{
    private List<LoaiChiPhiRow> _all = [];
    private LoaiChiPhiRow? _selected;
    private bool _isNew;

    public LoaiChiPhiPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var cpSvc = new ChiPhiService(db);
        var items = await cpSvc.LayDanhSachLoaiChiPhiAsync();
        _all = items.Select(l => new LoaiChiPhiRow
        {
            MaLoaiCp = l.MaLoaiCp,
            TenLoaiCp = l.TenLoaiCp,
            SoPhieu = l.ChiPhis.Count,
        }).ToList();

        TxtTong.Text = $"{_all.Count} loại chi phí";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        LoaiCpGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(l =>
                l.MaLoaiCp.ToLower().Contains(kw) ||
                l.TenLoaiCp.ToLower().Contains(kw)).ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void LoaiCpGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LoaiCpGrid.SelectedItem is not LoaiChiPhiRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        LoaiCpGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(LoaiChiPhiRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm loại chi phí mới" : "Sửa loại chi phí";
        TxtTen.Text = row?.TenLoaiCp ?? "";

        TxtHint.Text = row != null
            ? $"Có {row.SoPhieu} phiếu chi phí thuộc loại này"
            : "";

        BtnXoa.IsEnabled = row != null && row.SoPhieu == 0;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên loại chi phí.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();
            var cpSvc = new ChiPhiService(db);

            if (_isNew)
            {
                await cpSvc.TaoMoiLoaiChiPhiAsync(ten);
            }
            else if (_selected != null)
            {
                await cpSvc.CapNhatLoaiChiPhiAsync(_selected.MaLoaiCp, ten);
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

        if (MessageBox.Show($"Xóa loại chi phí \"{_selected.TenLoaiCp}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var cpSvc = new ChiPhiService(db);
            await cpSvc.XoaHoacVoHieuHoaLoaiChiPhiAsync(_selected.MaLoaiCp);

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
