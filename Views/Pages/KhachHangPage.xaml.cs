using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class KhachHangPage : Page
{
    private List<KhachHangViewModel> _all = [];
    private KhachHangViewModel? _selected;
    private bool _isNew;

    public KhachHangPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var khachHangSvc = new KhachHangService(db);

        var loaiKhach = await khachHangSvc.LayLoaiKhachAsync();
        CboLoaiKhach.ItemsSource = loaiKhach;
        CboLoaiKhach.DisplayMemberPath = "TenLoaiKhach";
        CboLoaiKhach.SelectedValuePath = "MaLoaiKhach";

        _all = await khachHangSvc.GetListAsync();

        KhachGrid.ItemsSource = _all;
        TxtTongKhach.Text = $"{_all.Count} khách hàng";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(k =>
                k.TenKhachHang.ToLower().Contains(kw) ||
                k.DienThoai.Contains(kw) ||
                k.Cccd.Contains(kw)).ToList();
        KhachGrid.ItemsSource = filtered;
        TxtTongKhach.Text = $"{filtered.Count} khách hàng";
    }

    private void KhachGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KhachGrid.SelectedItem is not KhachHangViewModel row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        KhachGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(KhachHangViewModel? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm khách hàng mới" : "Sửa khách hàng";
        TxtTen.Text = row?.TenKhachHang ?? "";
        TxtDienThoai.Text = row?.DienThoai ?? "";
        TxtCCCD.Text = row?.Cccd ?? "";
        TxtEmail.Text = "";
        TxtDiaChi.Text = "";
        TxtPassport.Text = row?.Passport ?? "";
        TxtVisa.Text = row?.Visa ?? "";
        TxtQuocTich.Text = row?.QuocTich ?? "";
        BtnXoa.IsEnabled = row != null;

        // Load thêm email/diachi nếu sửa
        if (row != null) _ = LoadExtraAsync(row.MaKhachHang);
        else CboLoaiKhach.SelectedIndex = 0;
    }

    private async Task LoadExtraAsync(string maKH)
    {
        using var db = new QuanLyKhachSanContext();
        var khachHangSvc = new KhachHangService(db);
        var kh = await khachHangSvc.LayTheoMaAsync(maKH);
        if (kh == null) return;
        TxtEmail.Text = kh.Email ?? "";
        TxtDiaChi.Text = kh.DiaChi ?? "";
        CboLoaiKhach.SelectedValue = kh.MaLoaiKhach;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            ConfirmHelper.ShowWarning("Vui lòng nhập họ tên.");
            return;
        }

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var khachHangSvc = new KhachHangService(db);

            if (_isNew)
            {
                await khachHangSvc.TaoMoiAsync(
                    ten,
                    TxtDienThoai.Text.Trim(),
                    TxtCCCD.Text.Trim(),
                    TxtEmail.Text.Trim(),
                    TxtDiaChi.Text.Trim(),
                    TxtPassport.Text.Trim(),
                    TxtVisa.Text.Trim(),
                    TxtQuocTich.Text.Trim(),
                    CboLoaiKhach.SelectedValue as string ?? "LK001");
            }
            else if (_selected != null)
            {
                await khachHangSvc.CapNhatAsync(
                    _selected.MaKhachHang,
                    ten,
                    TxtDienThoai.Text.Trim(),
                    TxtCCCD.Text.Trim(),
                    TxtEmail.Text.Trim(),
                    TxtDiaChi.Text.Trim(),
                    TxtPassport.Text.Trim(),
                    TxtVisa.Text.Trim(),
                    TxtQuocTich.Text.Trim(),
                    CboLoaiKhach.SelectedValue as string);
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

        if (!ConfirmHelper.ConfirmDelete(_selected.TenKhachHang)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var khachHangSvc = new KhachHangService(db);
            bool coDatPhong = await khachHangSvc.CoLichSuDatPhongAsync(_selected.MaKhachHang);
            if (coDatPhong)
            {
                ConfirmHelper.ShowWarning("Không thể xóa — khách hàng đã có lịch sử đặt phòng.");
                return;
            }

            await khachHangSvc.XoaAsync(_selected.MaKhachHang);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}


