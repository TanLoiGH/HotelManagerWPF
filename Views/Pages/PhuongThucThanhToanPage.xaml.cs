using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;

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

    private static string NextPttt(string? lastMa)
    {
        const string prefix = "PTTT";
        if (string.IsNullOrWhiteSpace(lastMa) || !lastMa.StartsWith(prefix))
            return "PTTT01";

        var numeric = lastMa[prefix.Length..];
        int pad = Math.Max(2, numeric.Length);

        if (int.TryParse(numeric, out int n))
            return $"{prefix}{(n + 1).ToString($"D{pad}")}";

        return "PTTT01";
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _all = await db.PhuongThucThanhToans
            .Select(p => new PhuongThucRow
            {
                MaPttt = p.MaPttt,
                TenPhuongThuc = p.TenPhuongThuc,
                SoGiaoDich = p.ThanhToans.Count(),
            })
            .OrderBy(p => p.MaPttt)
            .ToListAsync();

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

            if (_isNew)
            {
                var lastMa = await db.PhuongThucThanhToans
                    .OrderByDescending(p => p.MaPttt)
                    .Select(p => p.MaPttt)
                    .FirstOrDefaultAsync();

                db.PhuongThucThanhToans.Add(new PhuongThucThanhToan
                {
                    MaPttt = NextPttt(lastMa),
                    TenPhuongThuc = ten
                });
            }
            else if (_selected != null)
            {
                var item = await db.PhuongThucThanhToans.FindAsync(_selected.MaPttt);
                if (item != null) item.TenPhuongThuc = ten;
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

        if (MessageBox.Show($"Xóa phương thức \"{_selected.TenPhuongThuc}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var item = await db.PhuongThucThanhToans.FindAsync(_selected.MaPttt);
            if (item != null) db.PhuongThucThanhToans.Remove(item);
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

