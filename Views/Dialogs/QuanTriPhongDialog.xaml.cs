using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class QuanTriPhongRow
{
    public string MaPhong { get; set; } = "";
    public string MaLoaiPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string MaTrangThaiPhong { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public bool IsUsed { get; set; }
    public string UsedText => IsUsed ? "Có" : "Không";
}

public partial class QuanTriPhongDialog : Window
{
    private List<QuanTriPhongRow> _all = [];
    private QuanTriPhongRow? _selected;
    private bool _isNew;

    public QuanTriPhongDialog()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();

        var loaiPhongs = await db.LoaiPhongs
            .OrderBy(l => l.TenLoaiPhong)
            .ToListAsync();
        CboLoaiPhong.ItemsSource = loaiPhongs;
        CboLoaiPhong.DisplayMemberPath = "TenLoaiPhong";
        CboLoaiPhong.SelectedValuePath = "MaLoaiPhong";

        var trangThais = await db.PhongTrangThais
            .OrderByDescending(t => t.TenTrangThai)
            .ToListAsync();
        CboTrangThai.ItemsSource = trangThais;
        CboTrangThai.DisplayMemberPath = "TenTrangThai";
        CboTrangThai.SelectedValuePath = "MaTrangThaiPhong";

        _all = await db.Phongs
            .Include(p => p.MaLoaiPhongNavigation)
            .Include(p => p.MaTrangThaiPhongNavigation)
            .Select(p => new QuanTriPhongRow
            {
                MaPhong = p.MaPhong,
                MaLoaiPhong = p.MaLoaiPhong,
                TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                MaTrangThaiPhong = p.MaTrangThaiPhong ?? "PTT01",
                TenTrangThai = p.MaTrangThaiPhongNavigation != null ? (p.MaTrangThaiPhongNavigation.TenTrangThai ?? "") : "",
                IsUsed = p.DatPhongChiTiets.Any() || p.TienNghiPhongs.Any() || p.ChiPhis.Any(),
            })
            .OrderBy(p => p.MaPhong)
            .ToListAsync();

        TxtTong.Text = $"{_all.Count} phòng";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        PhongGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(p =>
                p.MaPhong.ToLower().Contains(kw) ||
                p.TenLoaiPhong.ToLower().Contains(kw) ||
                p.TenTrangThai.ToLower().Contains(kw)).ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void PhongGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PhongGrid.SelectedItem is not QuanTriPhongRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        PhongGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(QuanTriPhongRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;

        if (row == null)
        {
            TxtFormTitle.Text = "Thêm phòng mới";
            TxtMaPhong.IsEnabled = true;
            TxtMaPhong.Text = "";
            CboLoaiPhong.SelectedIndex = 0;
            CboTrangThai.SelectedValue = "PTT01";
            TxtHint.Text = "Gợi ý: nhập mã phòng theo chuẩn của khách sạn (VD: 101, 102, P101...).";
            BtnXoa.IsEnabled = false;
        }
        else
        {
            TxtFormTitle.Text = "Sửa phòng";
            TxtMaPhong.Text = row.MaPhong;
            TxtMaPhong.IsEnabled = false;
            CboLoaiPhong.SelectedValue = row.MaLoaiPhong;
            CboTrangThai.SelectedValue = row.MaTrangThaiPhong;
            TxtHint.Text = row.IsUsed
                ? "Phòng đã phát sinh dữ liệu (đặt phòng/tiện nghi/chi phí) → không cho xóa."
                : "Phòng chưa phát sinh dữ liệu → có thể xóa.";
            BtnXoa.IsEnabled = !row.IsUsed;
        }
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string maPhong = TxtMaPhong.Text.Trim();
        if (string.IsNullOrWhiteSpace(maPhong))
        {
            MessageBox.Show("Vui lòng nhập mã phòng.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CboLoaiPhong.SelectedValue is not string maLoaiPhong)
        {
            MessageBox.Show("Vui lòng chọn loại phòng.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string maTrangThai = CboTrangThai.SelectedValue as string ?? "PTT01";

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                bool exists = await db.Phongs.AnyAsync(p => p.MaPhong == maPhong);
                if (exists)
                    throw new InvalidOperationException("Mã phòng đã tồn tại.");

                db.Phongs.Add(new Phong
                {
                    MaPhong = maPhong,
                    MaLoaiPhong = maLoaiPhong,
                    MaTrangThaiPhong = maTrangThai
                });
            }
            else if (_selected != null)
            {
                var p = await db.Phongs.FindAsync(_selected.MaPhong);
                if (p != null)
                {
                    p.MaLoaiPhong = maLoaiPhong;
                    p.MaTrangThaiPhong = maTrangThai;
                }
            }

            await db.SaveChangesAsync();
            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
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

        if (MessageBox.Show($"Xóa phòng \"{_selected.MaPhong}\"?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();

            bool used = await db.Phongs
                .Where(p => p.MaPhong == _selected.MaPhong)
                .AnyAsync(p => p.DatPhongChiTiets.Any() || p.TienNghiPhongs.Any() || p.ChiPhis.Any());

            if (used)
                throw new InvalidOperationException("Phòng đã phát sinh dữ liệu, không thể xóa.");

            var p = await db.Phongs.FindAsync(_selected.MaPhong);
            if (p != null) db.Phongs.Remove(p);
            await db.SaveChangesAsync();

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

