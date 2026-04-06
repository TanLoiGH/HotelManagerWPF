using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

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

public partial class QuanTriPhongPage : Page
{
    private List<QuanTriPhongRow> _all = [];
    private QuanTriPhongRow? _selected;
    private bool _isNew;

    private readonly ObservableCollection<TienNghiPhongRow> _tienNghiItems = new();
    private List<TienNghiTrangThai> _tienNghiTrangThais = [];

    public QuanTriPhongPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
        TienNghiGrid.ItemsSource = _tienNghiItems;
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var roomSvc = new RoomService(db);
        var tnSvc = new TienNghiService(db);

        var loaiPhongs = await roomSvc.LayLoaiPhongAsync();
        CboLoaiPhong.ItemsSource = loaiPhongs;
        CboLoaiPhong.DisplayMemberPath = "TenLoaiPhong";
        CboLoaiPhong.SelectedValuePath = "MaLoaiPhong";

        var trangThais = await roomSvc.LayTrangThaiPhongAsync();
        CboTrangThai.ItemsSource = trangThais;
        CboTrangThai.DisplayMemberPath = "TenTrangThai";
        CboTrangThai.SelectedValuePath = "MaTrangThaiPhong";

        _tienNghiTrangThais = await tnSvc.LayTrangThaiTienNghiAsync();

        var comboCol = TienNghiGrid.Columns.OfType<DataGridComboBoxColumn>().FirstOrDefault();
        if (comboCol != null)
            comboCol.ItemsSource = _tienNghiTrangThais;

        var rooms = await roomSvc.LayDanhSachPhongQuanTriAsync();
        _all = rooms.Select(p => new QuanTriPhongRow
        {
            MaPhong = p.MaPhong,
            MaLoaiPhong = p.MaLoaiPhong,
            TenLoaiPhong = p.TenLoaiPhong,
            MaTrangThaiPhong = p.MaTrangThaiPhong,
            TenTrangThai = p.TenTrangThai,
            IsUsed = p.IsUsed,
        }).ToList();

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
        var roomSvc = new RoomService(db);
        var items = await roomSvc.LayTienNghiPhongQuanTriAsync(maPhong);

        _tienNghiItems.Clear();
        foreach (var i in items)
            _tienNghiItems.Add(new TienNghiPhongRow
            {
                MaTienNghi = i.MaTienNghi,
                TenTienNghi = i.TenTienNghi,
                MaTrangThai = i.MaTrangThai,
            });
    }

    private async void BtnChonTienNghi_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null || _isNew) return;

        var dlg = new ChonTienNghiPhongDialog(_selected.MaPhong) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        var selectedIds = dlg.SelectedMaTienNghi;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var roomSvc = new RoomService(db);
            await roomSvc.CapNhatDanhSachTienNghiPhongAsync(_selected.MaPhong, selectedIds);
            await LoadTienNghiAsync(_selected.MaPhong);
            await LoadAsync(); // Reload danh sách phòng để cập nhật IsUsed (Có/Không)
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi cập nhật tiện nghi: {ex.Message}");
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
            var roomSvc = new RoomService(db);
            await roomSvc.GoTienNghiKhoiPhongAsync(_selected.MaPhong, row.MaTienNghi);
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

        if (!ConfirmHelper.ConfirmSave($"phòng {maPhong}")) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var roomSvc = new RoomService(db);

            if (_isNew)
            {
                await roomSvc.TaoPhongAsync(maPhong, maLoaiPhong, maTrangThai);
            }
            else if (_selected != null)
            {
                await roomSvc.CapNhatPhongAsync(_selected.MaPhong, maLoaiPhong, maTrangThai);
            }

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

        if (!ConfirmHelper.ConfirmDelete($"phòng {_selected.MaPhong}")) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var roomSvc = new RoomService(db);
            await roomSvc.XoaPhongAsync(_selected.MaPhong);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (InvalidOperationException ex)
        {
            ConfirmHelper.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi xóa: {ex.Message}");
        }
    }
}
