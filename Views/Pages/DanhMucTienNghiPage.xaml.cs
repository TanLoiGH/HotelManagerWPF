using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class TienNghiRow
{
    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";
    public string? MaNcc { get; set; }
    public string TenNCC { get; set; } = "—";
    public DateOnly? HanBaoHanh { get; set; }
    public string HanBaoHanhText => HanBaoHanh?.ToString("dd/MM/yyyy") ?? "—";
    public int TongSoLuong { get; set; }
    public string DonViTinh { get; set; } = "";
    public bool IsActive { get; set; }
    public string ActiveText => IsActive ? "On" : "Off";
    public int SoPhong { get; set; }
}

public partial class DanhMucTienNghiPage : Page
{
    private List<TienNghiRow> _all = [];
    private TienNghiRow? _selected;
    private bool _isNew;

    private static bool IsAdminRole()
    {
        var mq = (AppSession.MaQuyen ?? "").Trim();
        return mq == "ADMIN" || mq == "GIAM_DOC";
    }

    public DanhMucTienNghiPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        bool canCrud = IsAdminRole();
        BtnThem.IsEnabled = canCrud;
        BtnLuu.IsEnabled = canCrud;
        BtnXoa.IsEnabled = canCrud;

        using var db = new QuanLyKhachSanContext();

        // NCC combo (thêm option "không chọn")
        var nccs = await db.NhaCungCaps
            .Where(n => n.IsActive == true)
            .OrderBy(n => n.TenNcc)
            .Select(n => new { n.MaNcc, n.TenNcc })
            .ToListAsync();

        var nccSource = new List<object> { new { MaNcc = "", TenNcc = "— Không chọn —" } };
        nccSource.AddRange(nccs);
        CboNCC.ItemsSource = nccSource;
        CboNCC.DisplayMemberPath = "TenNcc";
        CboNCC.SelectedValuePath = "MaNcc";
        CboNCC.SelectedIndex = 0;

        _all = await db.TienNghis
            .Include(t => t.MaNccNavigation)
            .Select(t => new TienNghiRow
            {
                MaTienNghi = t.MaTienNghi,
                TenTienNghi = t.TenTienNghi,
                MaNcc = t.MaNcc,
                TenNCC = t.MaNccNavigation != null ? t.MaNccNavigation.TenNcc : "—",
                HanBaoHanh = t.HanBaoHanh,
                TongSoLuong = t.TongSoLuong ?? 0,
                DonViTinh = t.DonViTinh ?? "",
                IsActive = t.IsActive == true,
                SoPhong = t.TienNghiPhongs.Count(),
            })
            .OrderBy(t => t.MaTienNghi)
            .ToListAsync();

        TxtTong.Text = $"{_all.Count} tiện nghi";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        TnGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(t =>
                t.MaTienNghi.ToLower().Contains(kw) ||
                t.TenTienNghi.ToLower().Contains(kw) ||
                t.TenNCC.ToLower().Contains(kw)).ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void TnGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TnGrid.SelectedItem is not TienNghiRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        TnGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(TienNghiRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;

        bool canCrud = IsAdminRole();
        BtnLuu.IsEnabled = canCrud;

        if (row == null)
        {
            TxtFormTitle.Text = "Thêm tiện nghi mới";
            TxtTen.Text = "";
            CboNCC.SelectedIndex = 0;
            DpHanBaoHanh.SelectedDate = null;
            TxtSoLuong.Text = "0";
            TxtDonVi.Text = "";
            ChkActive.IsChecked = true;
            TxtHint.Text = "Mã tiện nghi sẽ được tự sinh (TN001, TN002...).";
            BtnXoa.IsEnabled = false;
        }
        else
        {
            TxtFormTitle.Text = "Sửa tiện nghi";
            TxtTen.Text = row.TenTienNghi;
            CboNCC.SelectedValue = row.MaNcc ?? "";
            DpHanBaoHanh.SelectedDate = row.HanBaoHanh.HasValue
                ? row.HanBaoHanh.Value.ToDateTime(TimeOnly.MinValue)
                : null;
            TxtSoLuong.Text = row.TongSoLuong.ToString();
            TxtDonVi.Text = row.DonViTinh;
            ChkActive.IsChecked = row.IsActive;
            TxtHint.Text = $"Đã gán {row.SoPhong} lần vào phòng.";
            BtnXoa.IsEnabled = canCrud && row.SoPhong == 0;
        }
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdminRole())
        {
            MessageBox.Show("Bạn không có quyền quản trị danh mục tiện nghi.", "Không đủ quyền",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên tiện nghi.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(TxtSoLuong.Text.Trim(), out int soLuong);
        if (soLuong < 0) soLuong = 0;

        string? maNcc = (CboNCC.SelectedValue as string) ?? "";
        if (string.IsNullOrWhiteSpace(maNcc)) maNcc = null;

        DateOnly? hanBaoHanh = DpHanBaoHanh.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DpHanBaoHanh.SelectedDate.Value)
            : null;

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                var lastMa = await db.TienNghis
                    .OrderByDescending(t => t.MaTienNghi)
                    .Select(t => t.MaTienNghi)
                    .FirstOrDefaultAsync();

                db.TienNghis.Add(new TienNghi
                {
                    MaTienNghi = MaHelper.Next("TN", lastMa),
                    TenTienNghi = ten,
                    MaNcc = maNcc,
                    HanBaoHanh = hanBaoHanh,
                    TongSoLuong = soLuong,
                    DonViTinh = TxtDonVi.Text.Trim(),
                    IsActive = ChkActive.IsChecked == true,
                });
            }
            else if (_selected != null)
            {
                var item = await db.TienNghis.FindAsync(_selected.MaTienNghi);
                if (item != null)
                {
                    item.TenTienNghi = ten;
                    item.MaNcc = maNcc;
                    item.HanBaoHanh = hanBaoHanh;
                    item.TongSoLuong = soLuong;
                    item.DonViTinh = TxtDonVi.Text.Trim();
                    item.IsActive = ChkActive.IsChecked == true;
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
        if (!IsAdminRole())
        {
            MessageBox.Show("Bạn không có quyền quản trị danh mục tiện nghi.", "Không đủ quyền",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selected == null) return;

        if (MessageBox.Show($"Xóa tiện nghi \"{_selected.TenTienNghi}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            bool used = await db.TienNghis
                .Where(t => t.MaTienNghi == _selected.MaTienNghi)
                .AnyAsync(t => t.TienNghiPhongs.Any());

            if (used)
                throw new InvalidOperationException("Tiện nghi đã được gán vào phòng, không thể xóa. Hãy tắt Hoạt động (Off).");

            var item = await db.TienNghis.FindAsync(_selected.MaTienNghi);
            if (item != null) db.TienNghis.Remove(item);
            await db.SaveChangesAsync();

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Không thể xóa",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

