using System.Collections.ObjectModel;
using System.Linq;
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

public class TienNghiPhongRow
{
    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";
    public string MaTrangThai { get; set; } = "TNTT01";
}

public partial class QuanTriPhongDialog : Window
{
    private List<QuanTriPhongRow> _all = [];
    private QuanTriPhongRow? _selected;
    private bool _isNew;
    private readonly ObservableCollection<TienNghiPhongRow> _tienNghiItems = new();
    private List<TienNghiTrangThai> _tienNghiTrangThais = [];

    public QuanTriPhongDialog()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
        TienNghiGrid.ItemsSource = _tienNghiItems;
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

        _tienNghiTrangThais = await db.TienNghiTrangThais
            .OrderBy(t => t.MaTrangThai)
            .ToListAsync();

        var comboCol = TienNghiGrid.Columns.OfType<DataGridComboBoxColumn>().FirstOrDefault();
        if (comboCol != null)
            comboCol.ItemsSource = _tienNghiTrangThais;

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

    private async void PhongGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PhongGrid.SelectedItem is not QuanTriPhongRow row) return;
        _selected = row;
        _isNew = false;
        await ShowFormAsync(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        PhongGrid.SelectedItem = null;
        _ = ShowFormAsync(null);
    }

    private async Task ShowFormAsync(QuanTriPhongRow? row)
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
            PanelTienNghi.Visibility = Visibility.Collapsed;
            _tienNghiItems.Clear();
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

            PanelTienNghi.Visibility = Visibility.Visible;
            await LoadTienNghiAsync(row.MaPhong);
        }
    }

    private async Task LoadTienNghiAsync(string maPhong)
    {
        using var db = new QuanLyKhachSanContext();
        var items = await db.TienNghiPhongs
            .Include(t => t.MaTienNghiNavigation)
            .Where(t => t.MaPhong == maPhong)
            .OrderBy(t => t.MaTienNghiNavigation.TenTienNghi)
            .Select(t => new TienNghiPhongRow
            {
                MaTienNghi = t.MaTienNghi,
                TenTienNghi = t.MaTienNghiNavigation.TenTienNghi,
                MaTrangThai = t.MaTrangThai ?? "TNTT01",
            })
            .ToListAsync();

        _tienNghiItems.Clear();
        foreach (var i in items) _tienNghiItems.Add(i);
    }

    private async void BtnChonTienNghi_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null || _isNew) return;

        var dlg = new ChonTienNghiPhongDialog(_selected.MaPhong) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var selectedIds = dlg.SelectedMaTienNghi;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var current = await db.TienNghiPhongs
                .Where(t => t.MaPhong == _selected.MaPhong)
                .Select(t => t.MaTienNghi)
                .ToListAsync();

            var toAdd = selectedIds.Except(current).ToList();
            var toRemove = current.Except(selectedIds).ToList();

            foreach (var id in toAdd)
            {
                db.TienNghiPhongs.Add(new TienNghiPhong
                {
                    MaPhong = _selected.MaPhong,
                    MaTienNghi = id,
                    MaTrangThai = "TNTT01"
                });
            }

            if (toRemove.Count > 0)
            {
                var removeItems = await db.TienNghiPhongs
                    .Where(t => t.MaPhong == _selected.MaPhong && toRemove.Contains(t.MaTienNghi))
                    .ToListAsync();
                db.TienNghiPhongs.RemoveRange(removeItems);
            }

            await db.SaveChangesAsync();
            await LoadTienNghiAsync(_selected.MaPhong);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cập nhật tiện nghi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGoTienNghiDaChon_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null || _isNew) return;
        var rows = TienNghiGrid.SelectedItems.Cast<object>()
            .OfType<TienNghiPhongRow>()
            .ToList();

        if (rows.Count == 0) return;

        if (MessageBox.Show($"Gỡ {rows.Count} tiện nghi đã chọn khỏi phòng {_selected.MaPhong}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var ids = rows.Select(r => r.MaTienNghi).ToList();
            var removeItems = await db.TienNghiPhongs
                .Where(t => t.MaPhong == _selected.MaPhong && ids.Contains(t.MaTienNghi))
                .ToListAsync();
            db.TienNghiPhongs.RemoveRange(removeItems);
            await db.SaveChangesAsync();
            await LoadTienNghiAsync(_selected.MaPhong);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi gỡ tiện nghi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnLuuTienNghi_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null || _isNew) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            foreach (var row in _tienNghiItems)
            {
                var item = await db.TienNghiPhongs.FindAsync(_selected.MaPhong, row.MaTienNghi);
                if (item != null) item.MaTrangThai = row.MaTrangThai;
            }

            await db.SaveChangesAsync();
            await LoadTienNghiAsync(_selected.MaPhong);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu tiện nghi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGoTienNghi_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null || _isNew) return;
        if (sender is not Button btn || btn.DataContext is not TienNghiPhongRow row) return;

        if (MessageBox.Show($"Gỡ tiện nghi \"{row.TenTienNghi}\" khỏi phòng {_selected.MaPhong}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var item = await db.TienNghiPhongs.FindAsync(_selected.MaPhong, row.MaTienNghi);
            if (item != null) db.TienNghiPhongs.Remove(item);
            await db.SaveChangesAsync();
            await LoadTienNghiAsync(_selected.MaPhong);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi gỡ tiện nghi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
