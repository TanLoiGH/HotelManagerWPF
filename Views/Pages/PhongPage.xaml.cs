using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.ComponentModel;
using System.IO.Packaging;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.Views;

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

    private static int GetFloorNumber(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong)) return 0;

        // Common formats: "101" -> 1, "P101" -> 1
        var s = maPhong.Trim();

        if (s.Length >= 1 && char.IsDigit(s[0]) && int.TryParse(s[0].ToString(), out int floor0))
            return floor0;

        if (s.Length >= 2 && char.IsDigit(s[1]) && int.TryParse(s[1].ToString(), out int floor1))
            return floor1;

        return 0;
    }


    // ── Load phòng ───────────────────────────────────────────────────────
    private async Task LoadPhongAsync()
    {
        try
        {
            BtnQuanTriPhong.Visibility = IsAdminRole() ? Visibility.Visible : Visibility.Collapsed;

            using var db = new QuanLyKhachSanContext();

            // Get rooms with basic info
            var rooms = await db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Include(p => p.MaTrangThaiPhongNavigation)
                .OrderBy(p => p.MaPhong)
                .ToListAsync();

            // Get active bookings to show guest names on cards
            var activeBookings = await db.DatPhongChiTiets
                .Include(c => c.MaDatPhongNavigation)
                    .ThenInclude(dp => dp.MaKhachHangNavigation)
                .Where(c => c.MaDatPhongNavigation.TrangThai == "Đang ở" ||
                            c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng")
                .ToListAsync();

            _allPhong = rooms.Select(p =>
            {
                var booking = activeBookings.FirstOrDefault(b => b.MaPhong == p.MaPhong);
                return new PhongCardViewModel
                {
                    MaPhong = p.MaPhong,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                    TenTrangThai = p.MaTrangThaiPhongNavigation != null
                                        ? p.MaTrangThaiPhongNavigation.TenTrangThai ?? "" : "",
                    MaTrangThaiPhong = p.MaTrangThaiPhong ?? "PTT01",
                    SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0,
                    GiaPhong = p.MaLoaiPhongNavigation.GiaPhong,
                    Tang = GetFloorNumber(p.MaPhong),
                    GuestName = booking?.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang
                };
            }).ToList();

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

        var view = (ListCollectionView)CollectionViewSource.GetDefaultView(list);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PhongCardViewModel.Tang)));
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.Tang), ListSortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.SoPhongSort), ListSortDirection.Ascending));

        PhongList.ItemsSource = view;
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
        DetailHeader.Background = vm.BadgeForeground;

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
        TxtTenKH.Text = "";
        TxtDienThoai.Text = "";
        TxtCCCD.Text = "";
        TxtKhachInfo.Visibility = Visibility.Collapsed;
        ListKhach.Visibility = Visibility.Collapsed;
        ListKhach.ItemsSource = null;
    }

    // ── Tìm khách hàng ───────────────────────────────────────────────────
    private async void TxtTenKH_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtTenKH.Text.Trim();
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
        TxtTenKH.Text = kh.TenKhachHang;
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

        if (_selectedKhach == null && string.IsNullOrWhiteSpace(TxtTenKH.Text))
        {
            MessageBox.Show("Vui lòng chọn hoặc nhập thông tin khách hàng.",
                "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ngayNhan = DpNgayNhan.SelectedDate;
        var ngayTra = DpNgayTra.SelectedDate;
        if (!ngayNhan.HasValue || !ngayTra.HasValue || ngayTra <= ngayNhan)
        {
            ConfirmHelper.ShowWarning("Ngày nhận/trả không hợp lệ.");
            return;
        }

        if (!ConfirmHelper.Confirm("Bạn có chắc chắn muốn thực hiện đặt phòng này không?", "Xác nhận đặt phòng"))
            return;

        try
        {
            BtnDatPhong.IsEnabled = false;

            using var db = new QuanLyKhachSanContext();
            var khSvc = new KhachHangService(db);
            var dpSvc = new DatPhongService(db);

            if (_selectedKhach == null)
            {
                _selectedKhach = await khSvc.TimHoacTaoAsync(
                    TxtTenKH.Text.Trim(),
                    TxtDienThoai.Text.Trim(),
                    TxtCCCD.Text.Trim(),
                    TxtDiaChi.Text.Trim(),
                    TxtPassport.Text.Trim(),
                    TxtVisa.Text.Trim(),
                    TxtQuocTich.Text.Trim());
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
            var ct = await db.DatPhongChiTiets
                .Include(c => c.MaDatPhongNavigation)
                .Where(c => c.MaPhong == _selectedPhong.MaPhong &&
                            c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng") // ← fix
                .OrderByDescending(c => c.MaDatPhongNavigation.NgayDat)
                .FirstOrDefaultAsync();

            if (ct is null) throw new Exception("Không tìm thấy thông tin đặt phòng.");

            var dpSvc = new DatPhongService(db);
            await dpSvc.CheckInAsync(ct.MaDatPhong, App.CurrentUser?.MaNhanVien ?? "NV001");

            var hdSvc = new HoaDonService(db, new KhachHangService(db));
            var hoaDon = await hdSvc.XuatHoaDonAsync(
                ct.MaDatPhong,
                App.CurrentUser?.MaNhanVien ?? "NV001");

            int soDem = Math.Max(1, (int)(ct.NgayTra - ct.NgayNhan).TotalDays);
            decimal tienPhong = soDem * _selectedPhong.GiaPhong;
            decimal tienDichVu = 0m;
            decimal tongThanhToan = (tienPhong + tienDichVu) * 1.10m;           // ← fix: (P+DV)*1.1

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




