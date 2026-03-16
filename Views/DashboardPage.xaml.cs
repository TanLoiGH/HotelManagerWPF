using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

namespace QuanLyKhachSan_PhamTanLoi.Views;

// ViewModel cho room card trên dashboard
public class RoomCardViewModel
{
    public string MaPhong      { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string TrangThaiCode{ get; set; } = "";

    // Màu card theo trạng thái
    public SolidColorBrush CardBackground => TrangThaiCode switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(0, 184, 148)),   // Trống → xanh lá
        "PTT02" => new SolidColorBrush(Color.FromRgb(225, 112, 85)),  // Đang ở → cam đỏ
        "PTT03" => new SolidColorBrush(Color.FromRgb(253, 203, 110)), // Dọn dẹp → vàng
        "PTT04" => new SolidColorBrush(Color.FromRgb(150, 150, 150)), // Bảo trì → xám
        "PTT05" => new SolidColorBrush(Color.FromRgb(108, 92, 231)),  // Đã đặt → tím
        _       => new SolidColorBrush(Color.FromRgb(99, 110, 114)),
    };

    public Color ShadowColor => TrangThaiCode switch
    {
        "PTT01" => Color.FromRgb(0, 184, 148),
        "PTT02" => Color.FromRgb(225, 112, 85),
        "PTT03" => Color.FromRgb(253, 203, 110),
        _       => Color.FromRgb(0, 120, 212),
    };
}

// ViewModel cho booking item
public class BookingItemViewModel
{
    public string MaDatPhong    { get; set; } = "";
    public string TenKhachHang  { get; set; } = "";
    public string TrangThai     { get; set; } = "";

    public SolidColorBrush StatusColor => TrangThai switch
    {
        "Chờ nhận phòng" => new SolidColorBrush(Color.FromRgb(108, 92, 231)),
        "Đang ở"         => new SolidColorBrush(Color.FromRgb(0, 184, 148)),
        "Đã trả phòng"   => new SolidColorBrush(Color.FromRgb(99, 110, 114)),
        _                => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
    };
}

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();

            // ── Stats ──────────────────────────────────────────────────
            int tongPhong = await db.Phongs.CountAsync();
            int phongTrong = await db.Phongs
                .CountAsync(p => p.MaTrangThaiPhong == "PTT01");
            int phongOccupied = await db.Phongs
                .CountAsync(p => p.MaTrangThaiPhong == "PTT02");

            decimal doanhThu = await db.ThanhToans
                .Where(t => t.NgayThanhToan.HasValue
                         && t.NgayThanhToan.Value.Month == DateTime.Now.Month
                         && t.NgayThanhToan.Value.Year  == DateTime.Now.Year)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            TxtTongPhong.Text    = tongPhong.ToString();
            TxtPhongTrong.Text   = phongTrong.ToString();
            TxtPhongOccupied.Text = phongOccupied.ToString();
            TxtDoanhThu.Text     = FormatVnd(doanhThu);

            // ── Room grid ─────────────────────────────────────────────
            var phongs = await db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .OrderBy(p => p.MaPhong)
                .Select(p => new RoomCardViewModel
                {
                    MaPhong       = p.MaPhong,
                    TenLoaiPhong  = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                    TrangThaiCode = p.MaTrangThaiPhong ?? "PTT01",
                })
                .ToListAsync();

            RoomGrid.ItemsSource = phongs;

            // ── Recent Bookings ───────────────────────────────────────
            var bookings = await db.DatPhongs
                .Include(d => d.MaKhachHangNavigation)
                .OrderByDescending(d => d.NgayDat)
                .Take(8)
                .Select(d => new BookingItemViewModel
                {
                    MaDatPhong   = d.MaDatPhong,
                    TenKhachHang = d.MaKhachHangNavigation != null
                                   ? d.MaKhachHangNavigation.TenKhachHang : "(Không có KH)",
                    TrangThai    = d.TrangThai ?? "",
                })
                .ToListAsync();

            RecentBookings.ItemsSource = bookings;
            TxtNoBookings.Visibility   = bookings.Any()
                ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dashboard: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string FormatVnd(decimal amount)
    {
        if (amount >= 1_000_000_000)
            return (amount / 1_000_000_000m).ToString("N1", CultureInfo.InvariantCulture) + " tỷ ₫";
        if (amount >= 1_000_000)
            return (amount / 1_000_000m).ToString("N0", CultureInfo.InvariantCulture) + " tr ₫";
        return amount.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    }
}
