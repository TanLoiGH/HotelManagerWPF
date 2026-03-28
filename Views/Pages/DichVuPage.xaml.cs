using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class ServiceRow
{
    public string MaDichVu { get; set; } = "";
    public string TenDichVu { get; set; } = "";
    public decimal Gia { get; set; }
    public string DonViTinh { get; set; } = "";
    public bool? IsActive { get; set; }
    public string GiaText => Gia.ToString("N0") + " ₫";
    public string ActiveText => IsActive == true ? "Hoạt động" : "Tắt";
    public SolidColorBrush ActiveColor => IsActive == true
        ? new SolidColorBrush(Color.FromRgb(0, 184, 148))
        : new SolidColorBrush(Color.FromRgb(180, 180, 180));
}

public partial class DichVuPage : Page
{
    private List<ServiceRow> _allDv = [];
    private ServiceRow? _selected;
    private bool _isNew;

    public DichVuPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _allDv = await db.DichVus
            .OrderBy(d => d.TenDichVu)
            .Select(d => new ServiceRow
            {
                MaDichVu = d.MaDichVu,
                TenDichVu = d.TenDichVu,
                Gia = d.Gia ?? 0,
                DonViTinh = d.DonViTinh ?? "",
                IsActive = d.IsActive,
            })
            .ToListAsync();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        DvGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _allDv
            : _allDv.Where(d => d.TenDichVu.ToLower().Contains(kw)).ToList();
        TxtTongDV.Text = $"{_allDv.Count} dịch vụ";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void DvGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DvGrid.SelectedItem is not ServiceRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        DvGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(ServiceRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;

        TxtFormTitle.Text = row == null ? "Thêm dịch vụ mới" : "Sửa dịch vụ";
        TxtTen.Text = row?.TenDichVu ?? "";
        TxtGia.Text = row?.Gia.ToString("N0") ?? "";
        TxtDonVi.Text = row?.DonViTinh ?? "";
        ChkActive.IsChecked = row?.IsActive ?? true;
        BtnXoa.IsEnabled = row != null;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên dịch vụ.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string giaRaw = TxtGia.Text.Replace(",", "").Replace(".", "").Trim();
        if (!decimal.TryParse(giaRaw, out decimal gia) || gia < 0)
        {
            ConfirmHelper.ShowWarning("Giá không hợp lệ.");
            return;
        }

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.DichVus
                    .OrderByDescending(d => d.MaDichVu)
                    .Select(d => d.MaDichVu)
                    .FirstOrDefaultAsync();

                db.DichVus.Add(new DichVu
                {
                    MaDichVu = MaHelper.Next("DV", lastMa),
                    TenDichVu = ten,
                    Gia = gia,
                    DonViTinh = TxtDonVi.Text.Trim(),
                    IsActive = ChkActive.IsChecked ?? true,
                });
            }
            else if (_selected != null)
            {
                var dv = await db.DichVus.FindAsync(_selected.MaDichVu);
                if (dv != null)
                {
                    dv.TenDichVu = ten;
                    dv.Gia = gia;
                    dv.DonViTinh = TxtDonVi.Text.Trim();
                    dv.IsActive = ChkActive.IsChecked ?? true;
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

        if (!ConfirmHelper.ConfirmDelete(_selected.TenDichVu)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var dv = await db.DichVus.FindAsync(_selected.MaDichVu);
            if (dv != null)
            {
                // Nếu đã có giao dịch → chỉ tắt, không xóa
                bool coGiaoDich = await db.DichVuChiTiets
                    .AnyAsync(d => d.MaDichVu == dv.MaDichVu);

                if (coGiaoDich)
                {
                    dv.IsActive = false;
                    ConfirmHelper.ShowInfo("Dịch vụ đã có giao dịch — đã chuyển sang trạng thái Tắt thay vì xóa.");
                }
                else
                {
                    db.DichVus.Remove(dv);
                }

                await db.SaveChangesAsync();
            }

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi xóa: {ex.Message}");
        }
    }
}


