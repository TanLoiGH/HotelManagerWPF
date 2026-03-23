using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhaCungCapRow
{
    public string MaNcc { get; set; } = "";
    public string TenNcc { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string NguoiLienHe { get; set; } = "";
    public bool? IsActive { get; set; }
    public string IsActiveText => IsActive == true ? "✓ Có" : "✗ Không";
    public SolidColorBrush IsActiveColor => IsActive == true
        ? new SolidColorBrush(Color.FromRgb(0, 184, 148))
        : new SolidColorBrush(Color.FromRgb(200, 200, 200));
}

public partial class NhaCungCapPage : Page
{
    private List<NhaCungCapRow> _all = [];
    private NhaCungCapRow? _selected;
    private bool _isNew;

    public NhaCungCapPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _all = await db.NhaCungCaps
            .OrderBy(n => n.TenNcc)
            .Select(n => new NhaCungCapRow
            {
                MaNcc = n.MaNcc,
                TenNcc = n.TenNcc,
                DienThoai = n.DienThoai ?? "",
                Email = n.Email ?? "",
                DiaChi = n.DiaChi ?? "",
                NguoiLienHe = n.NguoiLienHe ?? "",
                IsActive = n.IsActive,
            })
            .ToListAsync();

        NCCGrid.ItemsSource = _all;
        TxtTongNCC.Text = $"{_all.Count} nhà cung cấp";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        NCCGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(n =>
                n.TenNcc.ToLower().Contains(kw) ||
                n.DienThoai.Contains(kw) ||
                n.Email.ToLower().Contains(kw)).ToList();
    }

    private void NCCGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NCCGrid.SelectedItem is not NhaCungCapRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        NCCGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(NhaCungCapRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm nhà cung cấp mới" : "Sửa nhà cung cấp";

        TxtTen.Text = row?.TenNcc ?? "";
        TxtDienThoai.Text = row?.DienThoai ?? "";
        TxtEmail.Text = row?.Email ?? "";
        TxtNguoiLienHe.Text = row?.NguoiLienHe ?? "";
        TxtDiaChi.Text = row?.DiaChi ?? "";
        ChkActive.IsChecked = row?.IsActive ?? true;
        BtnXoa.IsEnabled = row != null;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên nhà cung cấp.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.NhaCungCaps
                    .OrderByDescending(n => n.MaNcc)
                    .Select(n => n.MaNcc)
                    .FirstOrDefaultAsync();

                db.NhaCungCaps.Add(new NhaCungCap
                {
                    MaNcc = MaHelper.Next("NCC", lastMa),
                    TenNcc = ten,
                    DienThoai = TxtDienThoai.Text.Trim(),
                    Email = TxtEmail.Text.Trim(),
                    NguoiLienHe = TxtNguoiLienHe.Text.Trim(),
                    DiaChi = TxtDiaChi.Text.Trim(),
                    IsActive = ChkActive.IsChecked ?? true,
                });
            }
            else if (_selected != null)
            {
                var ncc = await db.NhaCungCaps.FindAsync(_selected.MaNcc);
                if (ncc != null)
                {
                    ncc.TenNcc = ten;
                    ncc.DienThoai = TxtDienThoai.Text.Trim();
                    ncc.Email = TxtEmail.Text.Trim();
                    ncc.NguoiLienHe = TxtNguoiLienHe.Text.Trim();
                    ncc.DiaChi = TxtDiaChi.Text.Trim();
                    ncc.IsActive = ChkActive.IsChecked ?? true;
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

        if (MessageBox.Show($"Vô hiệu hóa \"{_selected.TenNcc}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var ncc = await db.NhaCungCaps.FindAsync(_selected.MaNcc);
            if (ncc != null)
            {
                // Kiểm tra có tiện nghi/chi phí liên quan
                bool coLienQuan = await db.TienNghis.AnyAsync(t => t.MaNcc == ncc.MaNcc)
                               || await db.ChiPhis.AnyAsync(c => c.MaNcc == ncc.MaNcc);

                if (coLienQuan)
                {
                    ncc.IsActive = false;
                    MessageBox.Show("NCC đã có dữ liệu liên quan — đã vô hiệu hóa thay vì xóa.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else db.NhaCungCaps.Remove(ncc);

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


