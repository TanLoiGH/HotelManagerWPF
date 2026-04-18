using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class PhuongThucRow
{
    public string MaPttt { get; set; } = "";
    public string TenPhuongThuc { get; set; } = "";
    public int SoGiaoDich { get; set; }
}

public partial class PhuongThucThanhToanPage : Page
{
    private List<PhuongThucRow> _all = [];
    private PhuongThucRow? _selected;
    private bool _isNew;

    public PhuongThucThanhToanPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var ptttSvc = new PhuongThucThanhToanService(db);
        var items = await ptttSvc.LayDanhSachAsync();
        _all = items.Select(p => new PhuongThucRow
        {
            MaPttt = p.MaPttt,
            TenPhuongThuc = p.TenPhuongThuc,
            SoGiaoDich = p.SoGiaoDich,
        }).ToList();

        TxtTong.Text = $"{_all.Count} phương thức";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        PtttGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(p =>
                p.MaPttt.ToLower().Contains(kw) ||
                p.TenPhuongThuc.ToLower().Contains(kw)).ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void PtttGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PtttGrid.SelectedItem is not PhuongThucRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        PtttGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(PhuongThucRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm phương thức mới" : "Sửa phương thức";
        TxtTen.Text = row?.TenPhuongThuc ?? "";

        TxtHint.Text = row != null
            ? $"Có {row.SoGiaoDich} giao dịch đã dùng phương thức này"
            : "";

        BtnXoa.IsEnabled = row != null && row.SoGiaoDich == 0;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên phương thức.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();
            var ptttSvc = new PhuongThucThanhToanService(db);

            if (_isNew)
            {
                await ptttSvc.TaoMoiAsync(ten);
            }
            else if (_selected != null)
            {
                await ptttSvc.CapNhatAsync(_selected.MaPttt, ten);
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

        if (MessageBox.Show($"Xóa phương thức \"{_selected.TenPhuongThuc}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var ptttSvc = new PhuongThucThanhToanService(db);
            await ptttSvc.XoaHoacVoHieuHoaAsync(_selected.MaPttt);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
