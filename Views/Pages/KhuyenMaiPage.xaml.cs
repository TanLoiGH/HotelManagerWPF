using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class KhuyenMaiRow
{
    public string MaKhuyenMai { get; set; } = "";
    public string TenKhuyenMai { get; set; } = "";
    public string LoaiKhuyenMai { get; set; } = "";
    public decimal GiaTri { get; set; }
    public decimal GiaTriToiThieu { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public bool? IsActive { get; set; }
    public string? MaLoaiKhach { get; set; }

    public string GiaTriText => LoaiKhuyenMai == "Phần trăm"
        ? $"{GiaTri:N0}%" : $"{GiaTri:N0} ₫";
    public string TuNgayText => TuNgay?.ToString("dd/MM/yy") ?? "";
    public string DenNgayText => DenNgay?.ToString("dd/MM/yy") ?? "";

    private bool ConHieuLuc => IsActive == true
        && TuNgay <= TimeHelper.GetVietnamTime() && DenNgay >= TimeHelper.GetVietnamTime();

    public string StatusText => ConHieuLuc ? "Đang chạy"
        : IsActive == false ? "Tắt"
        : DenNgay < TimeHelper.GetVietnamTime() ? "Hết hạn" : "Chưa bắt đầu";

    public SolidColorBrush StatusColor => StatusText switch
    {
        "Đang chạy" => new SolidColorBrush(Color.FromRgb(0, 184, 148)),
        "Tắt" => new SolidColorBrush(Color.FromRgb(180, 180, 180)),
        "Hết hạn" => new SolidColorBrush(Color.FromRgb(225, 112, 85)),
        _ => new SolidColorBrush(Color.FromRgb(108, 92, 231)),
    };
}

public partial class KhuyenMaiPage : Page
{
    private List<KhuyenMaiRow> _all = [];
    private KhuyenMaiRow? _selected;
    private bool _isNew;

    public KhuyenMaiPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var kmSvc = new KhuyenMaiService(db);
        var loaiKhach = await kmSvc.LayLoaiKhachAsync();
        // Thêm option "Tất cả hạng"
        CboLoaiKhach.ItemsSource = loaiKhach;
        CboLoaiKhach.DisplayMemberPath = "TenLoaiKhach";
        CboLoaiKhach.SelectedValuePath = "MaLoaiKhach";

        var kms = await kmSvc.LayDanhSachAsync();
        _all = kms.Select(k => new KhuyenMaiRow
        {
            MaKhuyenMai = k.MaKhuyenMai,
            TenKhuyenMai = k.TenKhuyenMai ?? "",
            LoaiKhuyenMai = k.LoaiKhuyenMai ?? "Phần trăm",
            GiaTri = k.GiaTriKm ?? 0,
            GiaTriToiThieu = k.GiaTriToiThieu ?? 0,
            TuNgay = k.NgayBatDau,
            DenNgay = k.NgayKetThuc,
            IsActive = k.IsActive,
            MaLoaiKhach = k.MaLoaiKhach,
        }).ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        KmGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(k => k.TenKhuyenMai.ToLower().Contains(kw)).ToList();
        TxtTong.Text = $"{_all.Count} khuyến mãi";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void KmGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KmGrid.SelectedItem is not KhuyenMaiRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        KmGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void CboLoai_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (TxtLabelGiaTri == null) return;
        TxtLabelGiaTri.Text = CboLoai.SelectedIndex == 0
            ? "Giá trị (%) *" : "Giá trị (₫) *";
    }

    private void ShowForm(KhuyenMaiRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm khuyến mãi mới" : "Sửa khuyến mãi";

        TxtTen.Text = row?.TenKhuyenMai ?? "";
        CboLoai.SelectedIndex = row?.LoaiKhuyenMai == "Số tiền" ? 1 : 0;
        TxtGiaTri.Text = row?.GiaTri.ToString("N0") ?? "";
        TxtToiThieu.Text = row?.GiaTriToiThieu.ToString("N0") ?? "0";
        DpTuNgay.SelectedDate = row?.TuNgay ?? DateTime.Today;
        DpDenNgay.SelectedDate = row?.DenNgay ?? DateTime.Today.AddMonths(1);
        ChkActive.IsChecked = row?.IsActive ?? true;
        CboLoaiKhach.SelectedValue = row?.MaLoaiKhach;
        BtnXoa.IsEnabled = row != null;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập tên.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtGiaTri.Text.Replace(",", "").Replace(".", ""),
                out decimal giaTri) || giaTri <= 0)
        {
            MessageBox.Show("Giá trị không hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DpTuNgay.SelectedDate.HasValue || !DpDenNgay.SelectedDate.HasValue
            || DpDenNgay.SelectedDate <= DpTuNgay.SelectedDate)
        {
            MessageBox.Show("Ngày bắt đầu/kết thúc không hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtToiThieu.Text.Replace(",", "").Replace(".", ""),
            out decimal toiThieu);

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        string loai = CboLoai.SelectedIndex == 0 ? "Phần trăm" : "Số tiền";

        try
        {
            using var db = new QuanLyKhachSanContext();
            var kmSvc = new KhuyenMaiService(db);

            if (_isNew)
            {
                await kmSvc.TaoMoiAsync(
                    ten,
                    loai,
                    giaTri,
                    toiThieu,
                    DpTuNgay.SelectedDate!.Value,
                    DpDenNgay.SelectedDate!.Value,
                    CboLoaiKhach.SelectedValue as string,
                    ChkActive.IsChecked ?? true);
            }
            else if (_selected != null)
            {
                await kmSvc.CapNhatAsync(
                    _selected.MaKhuyenMai,
                    ten,
                    loai,
                    giaTri,
                    toiThieu,
                    DpTuNgay.SelectedDate!.Value,
                    DpDenNgay.SelectedDate!.Value,
                    CboLoaiKhach.SelectedValue as string,
                    ChkActive.IsChecked ?? true);
            }

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

        if (!ConfirmHelper.ConfirmDelete(_selected.TenKhuyenMai)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var kmSvc = new KhuyenMaiService(db);
            bool daTat = await kmSvc.XoaHoacTatAsync(_selected.MaKhuyenMai);
            if (daTat)
                ConfirmHelper.ShowInfo("Đã tắt khuyến mãi (có hóa đơn liên quan, không xóa được).");

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}


