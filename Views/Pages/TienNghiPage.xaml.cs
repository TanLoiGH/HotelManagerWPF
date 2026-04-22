using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class TienNghiRow
{
    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";

    // ĐÃ THÊM: Properties cho Danh Mục
    public string? MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = "—";

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

public partial class TienNghiPage : Page
{
    private List<TienNghiRow> _all = [];
    private TienNghiRow? _selected;
    private bool _isNew;

    private static bool IsAdminRole()
    {
        var mq = (AppSession.MaQuyen ?? "").Trim();
        return mq == "ADMIN" || mq == "GIAM_DOC";
    }

    public TienNghiPage()
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
        var tnSvc = new TienNghiService(db);

        // 1. Load NCC combo
        var nccs = await tnSvc.LayNhaCungCapDangHoatDongAsync();
        var nccSource = new List<object> { new { MaNcc = "", TenNcc = "— Không chọn —" } };
        nccSource.AddRange(nccs.Select(n => new { n.MaNcc, n.TenNcc }));
        CboNCC.ItemsSource = nccSource;
        CboNCC.DisplayMemberPath = "TenNcc";
        CboNCC.SelectedValuePath = "MaNcc";
        CboNCC.SelectedIndex = 0;

        // 2. ĐÃ THÊM: Load Danh Mục combo
        var danhMucs = await tnSvc.LayTatCaDanhMucAsync();
        var dmSource = new List<object> { new { MaDanhMuc = "", TenDanhMuc = "— Không phân loại —" } };
        dmSource.AddRange(danhMucs.Select(d => new { d.MaDanhMuc, d.TenDanhMuc }));
        CboDanhMuc.ItemsSource = dmSource;
        CboDanhMuc.DisplayMemberPath = "TenDanhMuc";
        CboDanhMuc.SelectedValuePath = "MaDanhMuc";
        CboDanhMuc.SelectedIndex = 0;

        // 3. Lấy Data đổ vào lưới
        var tienNghis = await tnSvc.LayTienNghiAsync();
        _all = tienNghis.Select(t => new TienNghiRow
        {
            MaTienNghi = t.MaTienNghi,
            TenTienNghi = t.TenTienNghi,

            // ĐÃ THÊM: Map dữ liệu Danh mục
            MaDanhMuc = t.MaDanhMuc,
            TenDanhMuc = t.MaDanhMucNavigation != null ? t.MaDanhMucNavigation.TenDanhMuc : "— Không phân loại —",

            MaNcc = t.MaNcc,
            TenNCC = t.MaNccNavigation != null ? t.MaNccNavigation.TenNcc : "—",
            HanBaoHanh = t.HanBaoHanh,
            TongSoLuong = t.TongSoLuong ?? 0,
            DonViTinh = t.DonViTinh ?? "",
            IsActive = t.IsActive == true,
            SoPhong = t.TienNghiPhongs.Count,
        }).ToList();

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
                t.TenNCC.ToLower().Contains(kw) ||
                t.TenDanhMuc.ToLower().Contains(kw)).ToList(); // ĐÃ THÊM: Hỗ trợ search theo Danh mục
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
            CboDanhMuc.SelectedIndex = 0; // ĐÃ THÊM
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
            CboDanhMuc.SelectedValue = row.MaDanhMuc ?? ""; // ĐÃ THÊM
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

        // ĐÃ THÊM: Lấy mã Danh mục từ ComboBox
        string? maDanhMuc = (CboDanhMuc.SelectedValue as string) ?? "";
        if (string.IsNullOrWhiteSpace(maDanhMuc)) maDanhMuc = null;

        string? maNcc = (CboNCC.SelectedValue as string) ?? "";
        if (string.IsNullOrWhiteSpace(maNcc)) maNcc = null;

        DateOnly? hanBaoHanh = DpHanBaoHanh.SelectedDate.HasValue
            ? DateOnly.FromDateTime(DpHanBaoHanh.SelectedDate.Value)
            : null;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var tnSvc = new TienNghiService(db);

            if (_isNew)
            {
                await tnSvc.TaoMoiTienNghiAsync(
                    ten,
                    maDanhMuc, // TRUYỀN THAM SỐ MỚI
                    maNcc,
                    hanBaoHanh,
                    soLuong,
                    TxtDonVi.Text.Trim(),
                    ChkActive.IsChecked == true);
            }
            else if (_selected != null)
            {
                await tnSvc.CapNhatTienNghiAsync(
                    _selected.MaTienNghi,
                    ten,
                    maDanhMuc, // XÓA TODO VÀ TRUYỀN THAM SỐ MỚI
                    maNcc,
                    hanBaoHanh,
                    soLuong,
                    TxtDonVi.Text.Trim(),
                    ChkActive.IsChecked == true);
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
            var tnSvc = new TienNghiService(db);
            await tnSvc.XoaTienNghiAsync(_selected.MaTienNghi);

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
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}