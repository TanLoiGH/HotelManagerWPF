using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class DatPhongDialog : Window
{
    private readonly PhongCardViewModel _phong;
    private KhachHang? _selectedKhach;

    public DatPhongDialog(PhongCardViewModel phong)
    {
        InitializeComponent();
        _phong = phong;
        TxtPhong.Text = $"{phong.MaPhong} – {phong.TenLoaiPhong} ({phong.GiaPhongText}/đêm)";
        DpNgayNhan.SelectedDate = DateTime.Today;
        DpNgayTra.SelectedDate = DateTime.Today.AddDays(1);
    }

    // Tìm kiếm KHACH_HANG theo tên / SĐT / CCCD
    private async void TxtTimKhach_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = TxtTimKhach.Text.Trim();
        if (keyword.Length < 2)
        {
            ListKhach.ItemsSource = null;
            ListKhach.Visibility = Visibility.Collapsed;
            return;
        }

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

    // Tính tiền tự động khi thay đổi ngày hoặc tiền cọc
    private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        => UpdateTinhGia();

    private void TxtTienCoc_TextChanged(object sender, TextChangedEventArgs e)
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
        decimal vat = tienPhong * 0.10m;
        decimal tong = tienPhong + vat;

        decimal.TryParse(TxtTienCoc?.Text, out decimal tienCoc);
        decimal conLai = tong - tienCoc;

        TxtTinhGia.Text = $"{soDem} đêm × {_phong.GiaPhong:N0} ₫ " +
                          $"+ VAT 10% ({vat:N0} ₫) " +
                          $"= {tong:N0} ₫" +
                          (tienCoc > 0 ? $"\n(Đã cọc: {tienCoc:N0} ₫ → Còn lại: {conLai:N0} ₫)" : "");
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

        decimal.TryParse(TxtTienCoc.Text, out decimal tienCoc);
        int.TryParse(TxtSoNguoi.Text, out int soNguoi);
        if (soNguoi <= 0) soNguoi = 1;

        if (!ConfirmHelper.Confirm("Bạn có chắc chắn muốn thực hiện đặt phòng này không?", "Xác nhận đặt phòng"))
            return;

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

            // Tạo DAT_PHONG + DAT_PHONG_CHI_TIET + cập nhật PHONG
            await dpSvc.TaoDatPhongAsync(
                _selectedKhach.MaKhachHang,
                [(_phong.MaPhong, ngayNhan.Value, ngayTra.Value)],
                tienCoc,
                soNguoi);

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


