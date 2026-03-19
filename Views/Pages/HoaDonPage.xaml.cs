using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class HoaDonRowViewModel
{
    public string MaHoaDon { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayLapText { get; set; } = "";
    public string TienPhongText { get; set; } = "";
    public string TienDichVuText { get; set; } = "";
    public string TongThanhToanText { get; set; } = "";
    public string? TrangThai { get; set; }
    public string MaPTTT { get; set; } = "";

    public SolidColorBrush StatusColor => TrangThai switch
    {
        "Đã thanh toán" => new SolidColorBrush(Color.FromRgb(0, 184, 148)),
        "Chưa thanh toán" => new SolidColorBrush(Color.FromRgb(225, 112, 85)),
        "Đã hủy" => new SolidColorBrush(Color.FromRgb(150, 150, 150)),
        _ => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
    };
}

public partial class HoaDonPage : Page
{
    private List<HoaDonRowViewModel> _allHoaDon = [];
    private string _filterStatus = "";
    private HoaDonRowViewModel? _selected;

    public HoaDonPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();
            _allHoaDon = await db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new HoaDonRowViewModel
                {
                    MaHoaDon = h.MaHoaDon,
                    TenKhachHang = h.MaDatPhongNavigation != null &&
                                   h.MaDatPhongNavigation.MaKhachHangNavigation != null
                                   ? h.MaDatPhongNavigation.MaKhachHangNavigation.TenKhachHang
                                   : "(Không có KH)",
                    NgayLapText = h.NgayLap.HasValue
                                  ? h.NgayLap.Value.ToString("dd/MM/yyyy") : "",
                    TienPhongText = (h.TienPhong ?? 0).ToString("N0") + " ₫",
                    TienDichVuText = (h.TienDichVu ?? 0).ToString("N0") + " ₫",
                    TongThanhToanText = (h.TongThanhToan ?? 0).ToString("N0") + " ₫",
                    TrangThai = h.TrangThai,
                })
                .ToListAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải hóa đơn: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyFilter()
    {
        var kw = TxtSearch?.Text.Trim().ToLower() ?? "";
        var filtered = _allHoaDon.AsEnumerable();

        if (!string.IsNullOrEmpty(_filterStatus))
            filtered = filtered.Where(h => h.TrangThai == _filterStatus);

        if (!string.IsNullOrEmpty(kw))
            filtered = filtered.Where(h =>
                h.MaHoaDon.ToLower().Contains(kw) ||
                h.TenKhachHang.ToLower().Contains(kw));

        HoaDonGrid.ItemsSource = filtered.ToList();
    }

    private void FilterStatus_Click(object sender, RoutedEventArgs e)
    {
        _filterStatus = (sender as Button)?.Tag?.ToString() ?? "";
        ApplyFilter();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();



    // Cập nhật HoaDonGrid_SelectionChanged
    private void HoaDonGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = HoaDonGrid.SelectedItem as HoaDonRowViewModel;
        bool chuaTT = _selected?.TrangThai == "Chưa thanh toán";

        BtnThemDichVu.IsEnabled = chuaTT;
        BtnThanhToan.IsEnabled = chuaTT;
    }

    // Thêm handler mới
    private async void BtnXuatHD_Click(object sender, RoutedEventArgs e)
    {
        // Ở đây: mở dialog chọn mã đặt phòng chưa có hóa đơn
        await XuatHoaDonTuDatPhongAsync();
    }

    private async Task XuatHoaDonTuDatPhongAsync()
    {
        using var db = new QuanLyKhachSanContext();

        // Lấy các đặt phòng "Đang ở" chưa có hóa đơn active
        var dps = await db.DatPhongs
            .Include(d => d.MaKhachHangNavigation)
            .Where(d => d.TrangThai == "Đang ở"
                     && !db.HoaDons.Any(h => h.MaDatPhong == d.MaDatPhong
                                          && h.TrangThai != "Đã hủy"))
            .ToListAsync();

        if (dps.Count == 0)
        {
            MessageBox.Show("Không có đặt phòng nào đang ở mà chưa có hóa đơn.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var khuyenMais = await db.KhuyenMais
            .Where(k => k.IsActive == true &&
                        k.NgayBatDau <= DateTime.Now &&
                        k.NgayKetThuc >= DateTime.Now)
            .OrderBy(k => k.TenKhuyenMai)
            .ToListAsync();

        // Hiện dialog chọn đặt phòng
        var dialog = new ChonDatPhongDialog(dps, khuyenMais) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true || dialog.SelectedMaDatPhong == null) return;

        try
        {
            var hdSvc = new HoaDonService(db, new KhachHangService(db));
            var hd = await hdSvc.XuatHoaDonAsync(
                dialog.SelectedMaDatPhong,
                App.CurrentUser?.MaNhanVien ?? "",
                dialog.SelectedMaKhuyenMai);

            MessageBox.Show($"Đã xuất hóa đơn {hd.MaHoaDon} thành công!",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất hóa đơn: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnThemDichVu_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;
        using var db = new QuanLyKhachSanContext();
        var dvSvc = new DichVuService(db);
        var dichVus = await dvSvc.GetAllDichVuAsync();

        var dialog = new ThemDichVuDialog(dichVus) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.SelectedResult.HasValue)
        {
            var (maDichVu, soLuong) = dialog.SelectedResult.Value;
            // Cần MaDatPhong và MaPhong — lấy từ DB
            var hd = await db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .FirstOrDefaultAsync(h => h.MaHoaDon == _selected.MaHoaDon);

            var ct = hd?.MaDatPhongNavigation?.DatPhongChiTiets.FirstOrDefault();
            if (ct != null)
            {
                await dvSvc.UpsertDichVuAsync(
                    _selected.MaHoaDon, ct.MaDatPhong, ct.MaPhong,
                    maDichVu, soLuong);
                await LoadAsync();
            }
        }
    }

    private async void BtnThanhToan_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;
        using var db = new QuanLyKhachSanContext();
        var hdSvc = new HoaDonService(db, new KhachHangService(db));
        var ptttList = await hdSvc.GetPTTTAsync();

        var dialog = new ThanhToanDialog(_selected.MaHoaDon, ptttList)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }
}



