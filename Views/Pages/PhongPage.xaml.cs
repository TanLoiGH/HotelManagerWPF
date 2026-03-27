using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using System.Globalization;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class PhongCardViewModel
{
    public string MaPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public string MaTrangThaiPhong { get; set; } = "";
    public int SoNguoiToiDa { get; set; }
    public decimal GiaPhong { get; set; }
    public string GiaPhongText => GiaPhong.ToString("N0", new CultureInfo("vi-VN")) + " ₫";

    public SolidColorBrush CardBackground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(0, 184, 148)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(225, 112, 85)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(127, 140, 141)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(108, 92, 231)),
        _ => new SolidColorBrush(Color.FromRgb(99, 110, 114)),
    };

    public Color ShadowColor => MaTrangThaiPhong switch
    {
        "PTT01" => Color.FromRgb(0, 184, 148),
        "PTT02" => Color.FromRgb(225, 112, 85),
        "PTT03" => Color.FromRgb(243, 156, 18),
        "PTT05" => Color.FromRgb(108, 92, 231),
        _ => Color.FromRgb(0, 120, 212),
    };
}

public class TienNghiItem { public string TenTienNghi { get; set; } = ""; }

public partial class PhongPage : Page
{
    private List<PhongCardViewModel> _allPhong = [];
    private string _currentFilter = "all";
    private PhongCardViewModel? _selectedPhong;
    private KhachHang? _selectedKhach;

    private static bool IsAdminRole()
    {
        var mq = (AppSession.MaQuyen ?? "").Trim();
        return mq == "ADMIN" || mq == "GIAM_DOC";
    }

    public PhongPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadPhongAsync();
    }

    // ── Load phòng ───────────────────────────────────────────────────────
    private async Task LoadPhongAsync()
    {
        try
        {
            BtnQuanTriPhong.Visibility = IsAdminRole() ? Visibility.Visible : Visibility.Collapsed;

            using var db = new QuanLyKhachSanContext();
            _allPhong = await db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Include(p => p.MaTrangThaiPhongNavigation)
                .OrderBy(p => p.MaPhong)
                .Select(p => new PhongCardViewModel
                {
                    MaPhong = p.MaPhong,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                    TenTrangThai = p.MaTrangThaiPhongNavigation != null
                                        ? p.MaTrangThaiPhongNavigation.TenTrangThai ?? "" : "",
                    MaTrangThaiPhong = p.MaTrangThaiPhong ?? "PTT01",
                    SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0,
                    GiaPhong = p.MaLoaiPhongNavigation.GiaPhong,
                })
                .ToListAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải phòng: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnQuanTriPhong_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdminRole())
        {
            MessageBox.Show("Bạn không có quyền quản trị phòng.", "Không đủ quyền",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new QuanTriPhongDialog { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
        await LoadPhongAsync();
    }

    // ── Load thông tin đặt trước (PTT05) ────────────────────────────────────
    private async Task LoadDatPhongInfoAsync(string maPhong)
    {
        try
        {
            using var db = new QuanLyKhachSanContext();

            var ct = await db.DatPhongChiTiets
                .Include(c => c.MaDatPhongNavigation)
                    .ThenInclude(dp => dp.MaKhachHangNavigation)
                .Where(c => c.MaPhong == maPhong &&
                            c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng") // ← fix
                .OrderByDescending(c => c.MaDatPhongNavigation.NgayDat)
                .FirstOrDefaultAsync();

            if (ct is null)
            {
                LblTenKhachDat.Text = "Không tìm thấy";
                LblTienCoc.Text = "—";
                LblNgayNhanDat.Text = "—";
                LblNgayTraDat.Text = "—";
                return;
            }

            var dp = ct.MaDatPhongNavigation;
            LblTenKhachDat.Text = dp.MaKhachHangNavigation?.TenKhachHang ?? "—";
            LblTienCoc.Text = "Chưa thu";                           // DB không có cột TienCoc
            LblNgayNhanDat.Text = ct.NgayNhan.ToString("dd/MM/yyyy");
            LblNgayTraDat.Text = ct.NgayTra.ToString("dd/MM/yyyy");
        }
        catch (Exception ex)
        {
            LblTenKhachDat.Text = $"Lỗi: {ex.Message}";
        }
    }
    // ── Filter ───────────────────────────────────────────────────────────
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        _currentFilter = (sender as Button)?.Tag?.ToString() ?? "all";
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text?.Trim().ToLower() ?? "";
        var q = _allPhong.AsEnumerable();

        if (_currentFilter != "all")
            q = q.Where(p => p.MaTrangThaiPhong == _currentFilter);

        if (!string.IsNullOrEmpty(kw))
            q = q.Where(p =>
                p.MaPhong.ToLower().Contains(kw) ||
                p.TenLoaiPhong.ToLower().Contains(kw));

        var list = q.ToList();
        PhongList.ItemsSource = list;
        TxtRoomCount.Text = $"{list.Count} phòng";
    }

    // ── Click card phòng ─────────────────────────────────────────────────
    private async void PhongCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element) return;

        var vm = element.Tag as PhongCardViewModel
            ?? element.DataContext as PhongCardViewModel;

        if (vm is null) return;

        _selectedPhong = vm;
        await ShowDetailAsync(vm);
    }

    private async Task ShowDetailAsync(PhongCardViewModel vm)
    {
        // Header
        DetailMaPhong.Text = vm.MaPhong;
        DetailLoaiPhong.Text = vm.TenLoaiPhong;
        DetailTrangThai.Text = vm.TenTrangThai;
        DetailHeader.Background = vm.CardBackground;

        // Info
        InfoSoNguoi.Text = $"{vm.SoNguoiToiDa} người";
        InfoGia.Text = vm.GiaPhongText + " / đêm";

        // Tiện nghi
        try
        {
            using var db = new QuanLyKhachSanContext();
            var tienNghis = await db.TienNghiPhongs
                .Include(t => t.MaTienNghiNavigation)
                .Where(t => t.MaPhong == vm.MaPhong)
                .OrderBy(t => EF.Functions.Collate(
                                    t.MaTienNghiNavigation.TenTienNghi,
                                    "Vietnamese_CI_AI"))
                .Select(t => new TienNghiItem
                {
                    TenTienNghi = t.MaTienNghiNavigation.TenTienNghi
                })
                .ToListAsync();

            TienNghiList.ItemsSource = tienNghis;
            TxtNoTienNghi.Visibility = tienNghis.Count > 0
                ? Visibility.Collapsed : Visibility.Visible;
        }
        catch { TxtNoTienNghi.Visibility = Visibility.Visible; }

        // Phân luồng hiển thị theo trạng thái
        bool laTrong = vm.MaTrangThaiPhong == "PTT01";
        bool laDaDat = vm.MaTrangThaiPhong == "PTT05";

        PanelDatPhong.Visibility = laTrong ? Visibility.Visible : Visibility.Collapsed;
        PanelDaDatTruoc.Visibility = laDaDat ? Visibility.Visible : Visibility.Collapsed;
        PanelKhongTrong.Visibility = (!laTrong && !laDaDat) ? Visibility.Visible : Visibility.Collapsed;

        if (!laTrong && !laDaDat)
            TxtKhongTrong.Text =
                $"Phòng đang ở trạng thái \"{vm.TenTrangThai}\"\nKhông thể đặt phòng lúc này.";

        if (laDaDat)
            await LoadDatPhongInfoAsync(vm.MaPhong);
        // Reset form
        ResetForm();

        // Default ngày
        DpNgayNhan.SelectedDate = DateTime.Today;
        DpNgayTra.SelectedDate = DateTime.Today.AddDays(1);
        UpdateTinhGia();

        // Hiện panel
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelDetail.Visibility = Visibility.Visible;
    }

    private void ResetForm()
    {
        _selectedKhach = null;
        TxtTimKhach.Text = "";
        TxtDienThoai.Text = "";
        TxtCCCD.Text = "";
        TxtKhachInfo.Visibility = Visibility.Collapsed;
        ListKhach.Visibility = Visibility.Collapsed;
        ListKhach.ItemsSource = null;
    }

    // ── Tìm khách hàng ───────────────────────────────────────────────────
    private async void TxtTimKhach_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtTimKhach.Text.Trim();
        if (kw.Length < 2) { ListKhach.Visibility = Visibility.Collapsed; return; }

        using var db = new QuanLyKhachSanContext();
        var results = await db.KhachHangs
            .Include(k => k.MaLoaiKhachNavigation)
            .Where(k => k.TenKhachHang.Contains(kw) ||
                        (k.DienThoai != null && k.DienThoai.Contains(kw)) ||
                        (k.Cccd != null && k.Cccd.Contains(kw)))
            .Take(8)
            .ToListAsync();

        ListKhach.ItemsSource = results;
        ListKhach.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ListKhach_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListKhach.SelectedItem is not KhachHang kh) return;
        _selectedKhach = kh;
        TxtTimKhach.Text = kh.TenKhachHang;
        ListKhach.Visibility = Visibility.Collapsed;
        TxtKhachInfo.Text = $"Hạng: {kh.MaLoaiKhachNavigation?.TenLoaiKhach}  " +
                                   $"| Tích lũy: {(kh.TongTichLuy ?? 0):N0} ₫";
        TxtKhachInfo.Visibility = Visibility.Visible;
        UpdateTinhGia();
    }

    // ── Tính giá ─────────────────────────────────────────────────────────
    private void DatePicker_Changed(object sender, SelectionChangedEventArgs e) => UpdateTinhGia();

    private void UpdateTinhGia()
    {
        if (_selectedPhong == null) return;
        var ngayNhan = DpNgayNhan?.SelectedDate;
        var ngayTra = DpNgayTra?.SelectedDate;
        if (!ngayNhan.HasValue || !ngayTra.HasValue) return;

        if (ngayTra <= ngayNhan)
        {
            TxtTinhGia.Text = "⚠ Ngày trả phải sau ngày nhận";
            return;
        }

        int soDem = (int)(ngayTra.Value - ngayNhan.Value).TotalDays;
        decimal tienPhong = soDem * _selectedPhong.GiaPhong;
        decimal tong = tienPhong * 1.10m;
        TxtTinhGia.Text = $"{soDem} đêm × {_selectedPhong.GiaPhong:N0} ₫" +
                          $" + VAT 10%" +
                          $" = {tong:N0} ₫";
    }

    // ── Xác nhận đặt phòng ───────────────────────────────────────────────
    private async void BtnDatPhong_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPhong == null) return;

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
            BtnDatPhong.IsEnabled = false;

            using var db = new QuanLyKhachSanContext();
            var khSvc = new KhachHangService(db);
            var dpSvc = new DatPhongService(db);

            if (_selectedKhach == null)
            {
                _selectedKhach = await khSvc.TimHoacTaoAsync(
                    TxtTimKhach.Text.Trim(),
                    TxtDienThoai.Text.Trim(),
                    TxtCCCD.Text.Trim());
            }

            await dpSvc.TaoDatPhongAsync(
                _selectedKhach.MaKhachHang,
                [(_selectedPhong.MaPhong, ngayNhan.Value, ngayTra.Value)]);

            MessageBox.Show($"Đặt phòng {_selectedPhong.MaPhong} thành công!",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh
            await LoadPhongAsync();
            PanelEmpty.Visibility = Visibility.Visible;
            PanelDetail.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đặt phòng:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnDatPhong.IsEnabled = true;
        }
    }
    // ── Xác nhận khách nhận phòng (PTT05 → PTT02) ───────────────────────────
    private async void BtnKhachNhanPhong_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPhong is null) return;

        var confirm = MessageBox.Show(
            $"Xác nhận khách đã nhận phòng {_selectedPhong.MaPhong}?",
            "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            BtnKhachNhanPhong.IsEnabled = false;

            using var db = new QuanLyKhachSanContext();

            // 1. Phòng → Đang ở
            var phong = await db.Phongs.FindAsync(_selectedPhong.MaPhong);
            if (phong is null) throw new Exception("Không tìm thấy phòng trong DB.");
            phong.MaTrangThaiPhong = "PTT02";

            // 2. Lấy chi tiết đặt phòng
            var ct = await db.DatPhongChiTiets
                .Include(c => c.MaDatPhongNavigation)
                .Where(c => c.MaPhong == _selectedPhong.MaPhong &&
                            c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng") // ← fix
                .OrderByDescending(c => c.MaDatPhongNavigation.NgayDat)
                .FirstOrDefaultAsync();

            if (ct is null) throw new Exception("Không tìm thấy thông tin đặt phòng.");

            var dp = ct.MaDatPhongNavigation;
            dp.TrangThai = "Đang ở";                                             // ← fix

            await db.SaveChangesAsync();

            // 3. Tự động tạo hóa đơn
            int soDem = Math.Max(1, (int)(ct.NgayTra - ct.NgayNhan).TotalDays);
            decimal tienPhong = soDem * _selectedPhong.GiaPhong;
            decimal tienDichVu = 0m;
            decimal tongThanhToan = (tienPhong + tienDichVu) * 1.10m;           // ← fix: (P+DV)*1.1

            var lastMaHd = await db.HoaDons
                .OrderByDescending(h => h.MaHoaDon)
                .Select(h => h.MaHoaDon)
                .FirstOrDefaultAsync();

            var hoaDon = new HoaDon
            {
                MaHoaDon = MaHelper.Next("HD", lastMaHd),
                MaDatPhong = dp.MaDatPhong,
                MaNhanVien = App.CurrentUser?.MaNhanVien,
                NgayLap = DateTime.Now,
                TienPhong = tienPhong,
                TienDichVu = tienDichVu,
                Vat = 10m,                                             // ← fix: lưu % không phải tiền
                MaKhuyenMai = null,
                TongThanhToan = tongThanhToan,
                TrangThai = "Chưa thanh toán",
            };

            db.HoaDons.Add(hoaDon);
            await db.SaveChangesAsync();

            MessageBox.Show(
                $"✅ Phòng {_selectedPhong.MaPhong} đã nhận khách!\n\n" +
                $"🧾 Hóa đơn {hoaDon.MaHoaDon} tự động được tạo\n" +
                $"   {soDem} đêm × {_selectedPhong.GiaPhong:N0} ₫ + VAT 10%\n" +
                $"   Tổng tạm tính: {tongThanhToan:N0} ₫",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi xác nhận khách nhận phòng:\n{ex.Message}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnKhachNhanPhong.IsEnabled = true;
            await LoadPhongAsync();
            PanelEmpty.Visibility = Visibility.Visible;
            PanelDetail.Visibility = Visibility.Collapsed;
        }
        }
}




