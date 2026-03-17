using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhaCungCapRow
{
    public string MaNcc { get; set; } = "";
    public string TenNcc { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string NguoiLienHe { get; set; } = "";
    public bool? IsActive { get; set; }
    public string IsActiveText => IsActive == true ? "✓ Có" : "✗ Không";
    public SolidColorBrush IsActiveColor => IsActive == true
        ? new SolidColorBrush(Color.FromRgb(0, 184, 148))
        : new SolidColorBrush(Color.FromRgb(200, 200, 200));
}

public partial class NhaCungCapPage : Page
{
    private List<NhaCungCapRow> _allNCC = [];

    public NhaCungCapPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();
            _allNCC = await db.NhaCungCaps
                .OrderBy(n => n.TenNcc)
                .Select(n => new NhaCungCapRow
                {
                    MaNcc = n.MaNcc,
                    TenNcc = n.TenNcc,
                    DienThoai = n.DienThoai ?? "",
                    Email = n.Email ?? "",
                    NguoiLienHe = n.NguoiLienHe ?? "",
                    IsActive = n.IsActive,
                })
                .ToListAsync();

            NCCGrid.ItemsSource = _allNCC;
            TxtTongNCC.Text = $"{_allNCC.Count} nhà cung cấp";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        NCCGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _allNCC
            : _allNCC.Where(n =>
                n.TenNcc.ToLower().Contains(kw) ||
                n.DienThoai.Contains(kw) ||
                n.Email.ToLower().Contains(kw)).ToList();
    }
}