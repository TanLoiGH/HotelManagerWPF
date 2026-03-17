using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class DatPhongDialog : Window
{
    private readonly PhongCardViewModel _phong;
    private KhachHang? _selectedKhach;
    private List<KhuyenMai> _khuyenMais = [];

    public DatPhongDialog(PhongCardViewModel phong)
    {
        InitializeComponent();
        _phong = phong;
        TxtPhong.Text = $"{phong.MaPhong} – {phong.TenLoaiPhong} ({phong.GiaPhongText}/đêm)";
        DpNgayNhan.SelectedDate = DateTime.Today;
        DpNgayTra.SelectedDate = DateTime.Today.AddDays(1);
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await LoadKhuyenMaiAsync();
    }

    // Load KHUYEN_MAI còn hiệu lực
    private async Task LoadKhuyenMaiAsync()
    {
        using var db = new QuanLyKhachSanContext();
        _khuyenMais = await db.KhuyenMais
            .Where(k => k.IsActive == true &&
                        k.NgayBatDau <= DateTime.Now &&
                        k.NgayKetThuc >= DateTime.Now)
            .ToListAsync();

        CboKhuyenMai.ItemsSource = _khuyenMais;
        CboKhuyenMai.DisplayMemberPath = "TenKhuyenMai";
        CboKhuyenMai.SelectedValuePath = "MaKhuyenMai";

        // Thêm option "Không áp dụng"
        _khuyenMais.Insert(0, new KhuyenMai
        {
            MaKhuyenMai = "",
            TenKhuyenMai = "— Không áp dụng —",
            GiaTriKm = 0
        });
        CboKhuyenMai.ItemsSource = _khuyenMais;
        CboKhuyenMai.SelectedIndex = 0;
    }

    // Tìm kiếm KHACH_HANG theo tên / SĐT / CCCD
    private async void TxtTimKhach_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = TxtTimKhach.Text.Trim();
        if (keyword.Length < 2) return;

        using var db = new QuanLyKhachSanContext();
        var results = await db.KhachHangs
            .Include(k => k.MaLoaiKhachNavigation)  // LOAI_KHACH
            .Where(k => k.TenKhachHang.Contains(keyword) ||
                        (k.DienThoai != null && k.DienThoai.Contains(keyword)) ||
                        (k.Cccd != null && k.Cccd.Contains(keyword)))
            .Take(10)
            .ToListAsync();

        ListKhach.ItemsSource = results;
        ListKhach.Visibility = results.Any() ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ListKhach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListKhach.SelectedItem is not KhachHang kh) return;
        _selectedKhach = kh;
        TxtTimKhach.Text = kh.TenKhachHang;
        ListKhach.Visibility = Visibility.Collapsed;
        TxtKhachInfo.Text = $"Hạng: {kh.MaLoaiKhachNavigation?.TenLoaiKhach} " +
                            $"| Tích lũy: {(kh.TongTichLuy ?? 0):N0} ₫";
        TxtKhachInfo.Visibility = Visibility.Visible;
        UpdateTinhGia();
    }

    // Tính tiền tự động khi thay đổi ngày
    private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        => UpdateTinhGia();

    private void CboKhuyenMai_Changed(object sender, SelectionChangedEventArgs e)
        => UpdateTinhGia();

    private void UpdateTinhGia()
    {
        var ngayNhan = DpNgayNhan.SelectedDate;
        var ngayTra = DpNgayTra.SelectedDate;
        if (!ngayNhan.HasValue || !ngayTra.HasValue) return;
        if (ngayTra <= ngayNhan)
        {
            TxtTinhGia.Text = "⚠ Ngày trả phải sau ngày nhận";
            return;
        }

        int soDem = (int)(ngayTra.Value - ngayNhan.Value).TotalDays;
        decimal tienPhong = soDem * _phong.GiaPhong;
        decimal kmPct = 0;

        var km = CboKhuyenMai.SelectedItem as KhuyenMai;
        if (km is { MaKhuyenMai: not "" })
        {
            kmPct = km.LoaiKhuyenMai == "Phần trăm"
                ? km.GiaTriKm ?? 0
                : tienPhong > 0 ? (km.GiaTriKm ?? 0) / tienPhong * 100 : 0;
        }

        decimal tong = tienPhong * (1 + 10m / 100) * (1 - kmPct / 100);
        TxtTinhGia.Text = $"{soDem} đêm × {_phong.GiaPhong:N0} ₫ " +
                          $"+ VAT 10% {(kmPct > 0 ? $"- KM {kmPct:N0}%" : "")} " +
                          $"= {tong:N0} ₫";
    }

    // Xác nhận đặt phòng
    private async void BtnXacNhan_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (_selectedKhach == null && string.IsNullOrWhiteSpace(TxtTimKhach.Text))
        {
            MessageBox.Show("Vui lòng chọn hoặc nhập thông tin khách hàng.",
                "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ngayNhan = DpNgayNhan.SelectedDate;
        var ngayTra = DpNgayTra.SelectedDate;
        if (!ngayNhan.HasValue || !ngayTra.HasValue || ngayTra <= ngayNhan)
        {
            MessageBox.Show("Ngày nhận/trả không hợp lệ.",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            BtnXacNhan.IsEnabled = false;
            BtnXacNhan.Content = "Đang xử lý…";

            using var db = new QuanLyKhachSanContext();
            var khSvc = new KhachHangService(db);
            var dpSvc = new DatPhongService(db);

            // Tạo mới hoặc lấy khách hàng hiện có (KHACH_HANG)
            if (_selectedKhach == null)
            {
                _selectedKhach = await khSvc.TimHoacTaoAsync(
                    TxtTimKhach.Text.Trim(),
                    TxtDienThoai.Text.Trim(),
                    TxtCCCD.Text.Trim());
            }

            var km = CboKhuyenMai.SelectedItem as KhuyenMai;

            // Tạo DAT_PHONG + DAT_PHONG_CHI_TIET + cập nhật PHONG
            await dpSvc.TaoDatPhongAsync(
                _selectedKhach.MaKhachHang,
                [(_phong.MaPhong, ngayNhan.Value, ngayTra.Value)]);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đặt phòng:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            BtnXacNhan.IsEnabled = true;
            BtnXacNhan.Content = "Xác nhận đặt phòng";
        }
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}