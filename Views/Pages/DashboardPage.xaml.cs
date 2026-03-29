using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

namespace QuanLyKhachSan_PhamTanLoi.Views;

// ── View models cho chart ────────────────────────────────────────────────
public class BarChartItem
{
    public string ThangText { get; set; } = "";
    public double BarHeight { get; set; }
    public decimal GiaTriGoc { get; set; }
    public string Tooltip { get; set; } = "";
    public SolidColorBrush BarColor { get; set; } = new(Color.FromRgb(0, 120, 212));
}

public class PhongStatusItem
{
    public string TenTT { get; set; } = "";
    public int SoPhong { get; set; }
    public double BarWidth { get; set; }
    public SolidColorBrush MauSac { get; set; } = new(Colors.Gray);
}

public class TopDichVuItem
{
    public string TenDV { get; set; } = "";
    public int SoLan { get; set; }
}

public class ChiPhiItem
{
    public string Loai { get; set; } = "";
    public decimal Tong { get; set; }
    public double BarWidth { get; set; }
    public string TongText => Tong.ToString("N0") + " ₫";
}

public class DashboardBookingItem
{
    public string MaDatPhong { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public SolidColorBrush StatusColor => TrangThai switch
    {
        "Chờ nhận phòng" => new SolidColorBrush(Color.FromRgb(99, 102, 241)),  // Luxury Indigo (#6366F1)
        "Đang ở" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),         // Luxury Emerald (#10B981)
        "Đã trả phòng" => new SolidColorBrush(Color.FromRgb(100, 116, 139)),   // Luxury Slate (#64748B)
        _ => new SolidColorBrush(Color.FromRgb(37, 99, 235)),                 // BrandPrimary (#2563EB)
    };
}

public partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            DpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpDenNgay.SelectedDate = DateTime.Today;
        };
    }

    // ── Bộ lọc nhanh ────────────────────────────────────────────────────
    private void BtnThangNay_Click(object sender, RoutedEventArgs e)
    {
        DpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        DpDenNgay.SelectedDate = DateTime.Today;
    }

    private void BtnQuyNay_Click(object sender, RoutedEventArgs e)
    {
        int quy = (DateTime.Now.Month - 1) / 3 + 1;
        DpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, (quy - 1) * 3 + 1, 1);
        DpDenNgay.SelectedDate = DateTime.Today;
    }

    private void BtnNamNay_Click(object sender, RoutedEventArgs e)
    {
        DpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1);
        DpDenNgay.SelectedDate = DateTime.Today;
    }

    private async void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DpTuNgay?.SelectedDate == null || DpDenNgay?.SelectedDate == null) return;
        await LoadDashboardAsync(DpTuNgay.SelectedDate.Value, DpDenNgay.SelectedDate.Value);
    }

    // ── Load dữ liệu ────────────────────────────────────────────────────
    private async Task LoadDashboardAsync(DateTime tuNgay, DateTime denNgay)
    {
        try
        {
            using var db = new QuanLyKhachSanContext();

            // KPIs
            decimal doanhThu = await db.HoaDons
                .Where(h => h.TrangThai == "Đã thanh toán"
                         && h.NgayLap >= tuNgay && h.NgayLap <= denNgay)
                .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;

            decimal chiPhi = await db.ChiPhis
                .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
                .SumAsync(c => (decimal?)c.SoTien) ?? 0;

            int tongPhong = await db.Phongs.CountAsync();
            int phongDangO = await db.Phongs.CountAsync(p => p.MaTrangThaiPhong == "PTT02");
            double congSuat = tongPhong > 0 ? (double)phongDangO / tongPhong * 100 : 0;

            TxtDoanhThu.Text = FormatVnd(doanhThu);
            TxtChiPhi.Text = FormatVnd(chiPhi);
            TxtLoiNhuan.Text = FormatVnd(doanhThu - chiPhi);
            TxtTongPhong.Text = tongPhong.ToString();
            TxtCongSuat.Text = $"{congSuat:N0}%";

            TxtLoiNhuan.Foreground = doanhThu - chiPhi >= 0
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Color.FromRgb(255, 200, 200));

            // Biểu đồ 12 tháng
            await LoadBarChartAsync(db);

            // Trạng thái phòng
            await LoadPhongStatusAsync(db, tongPhong);

            // Top dịch vụ
            await LoadTopDichVuAsync(db, tuNgay, denNgay);

            // Chi phí theo loại
            await LoadChiPhiAsync(db, tuNgay, denNgay);

            // Đặt phòng gần đây
            await LoadRecentBookingsAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dashboard: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadBarChartAsync(QuanLyKhachSanContext db)
    {
        var now = DateTime.Now;
        var items = new List<BarChartItem>();

        // Lấy doanh thu 12 tháng gần nhất
        var data = new List<(int Year, int Month, decimal Total)>();
        for (int i = 11; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var total = await db.HoaDons
                .Where(h => h.TrangThai == "Đã thanh toán"
                         && h.NgayLap.HasValue
                         && h.NgayLap.Value.Year == d.Year
                         && h.NgayLap.Value.Month == d.Month)
                .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;
            data.Add((d.Year, d.Month, total));
        }

        decimal maxVal = data.Max(x => x.Total);
        if (maxVal == 0) maxVal = 1;

        foreach (var (year, month, total) in data)
        {
            bool isCurrentMonth = year == now.Year && month == now.Month;
            items.Add(new BarChartItem
            {
                ThangText = $"T{month}",
                BarHeight = Math.Max(4, (double)(total / maxVal) * 160),
                GiaTriGoc = total,
                Tooltip = $"Tháng {month}/{year}: {FormatVnd(total)}",
                BarColor = new SolidColorBrush(isCurrentMonth
                    ? Color.FromRgb(37, 99, 235)
                    : Color.FromRgb(147, 197, 253))
            });
        }

        BarChart.ItemsSource = items;
    }

    private async Task LoadPhongStatusAsync(QuanLyKhachSanContext db, int tongPhong)
    {
        var stats = await db.Phongs
            .Include(p => p.MaTrangThaiPhongNavigation)
            .GroupBy(p => new { p.MaTrangThaiPhong, p.MaTrangThaiPhongNavigation!.TenTrangThai })
            .Select(g => new { g.Key.MaTrangThaiPhong, g.Key.TenTrangThai, Count = g.Count() })
            .ToListAsync();

        double max = tongPhong > 0 ? tongPhong : 1;

        PhongStatusList.ItemsSource = stats.Select(s => new PhongStatusItem
        {
            TenTT = s.TenTrangThai ?? "",
            SoPhong = s.Count,
            BarWidth = (double)s.Count / max * 80,
            MauSac = new SolidColorBrush(s.MaTrangThaiPhong switch
            {
                "PTT01" => Color.FromRgb(16, 185, 129),
                "PTT02" => Color.FromRgb(225, 29, 72),
                "PTT03" => Color.FromRgb(245, 158, 11),
                "PTT04" => Color.FromRgb(100, 116, 139),
                "PTT05" => Color.FromRgb(99, 102, 241),
                _ => Color.FromRgb(100, 116, 139),
            })
        }).ToList();
    }

    private async Task LoadTopDichVuAsync(QuanLyKhachSanContext db, DateTime tu, DateTime den)
    {
        var top = await db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.NgaySuDung >= tu && d.NgaySuDung <= den)
            .GroupBy(d => d.MaDichVuNavigation.TenDichVu)
            .Select(g => new TopDichVuItem
            {
                TenDV = g.Key ?? "",
                SoLan = g.Sum(x => x.SoLuong)
            })
            .OrderByDescending(x => x.SoLan)
            .Take(5)
            .ToListAsync();

        TopDichVuList.ItemsSource = top;
    }

    private async Task LoadChiPhiAsync(QuanLyKhachSanContext db, DateTime tu, DateTime den)
    {
        var list = await db.ChiPhis
            .Include(c => c.MaLoaiCpNavigation)
            .Where(c => c.NgayChiPhi >= tu && c.NgayChiPhi <= den)
            .GroupBy(c => c.MaLoaiCpNavigation.TenLoaiCp)
            .Select(g => new { Loai = g.Key ?? "Khác", Tong = g.Sum(x => x.SoTien) })
            .OrderByDescending(x => x.Tong)
            .ToListAsync();

        decimal maxCP = list.Count > 0 ? list.Max(x => x.Tong) : 1;

        ChiPhiList.ItemsSource = list.Select(x => new ChiPhiItem
        {
            Loai = x.Loai,
            Tong = x.Tong,
            BarWidth = (double)(x.Tong / maxCP) * 200,
        }).ToList();
    }

    private async Task LoadRecentBookingsAsync(QuanLyKhachSanContext db)
    {
        var bookings = await db.DatPhongs
            .Include(d => d.MaKhachHangNavigation)
            .OrderByDescending(d => d.NgayDat)
            .Take(8)
            .Select(d => new DashboardBookingItem
            {
                MaDatPhong = d.MaDatPhong,
                TenKhachHang = d.MaKhachHangNavigation != null
                               ? d.MaKhachHangNavigation.TenKhachHang : "(Không có KH)",
                TrangThai = d.TrangThai ?? "",
            })
            .ToListAsync();

        RecentBookings.ItemsSource = bookings;
    }

    private static string FormatVnd(decimal amount)
    {
        if (amount >= 1_000_000_000)
            return (amount / 1_000_000_000m).ToString("N1", CultureInfo.InvariantCulture) + " tỷ";
        if (amount >= 1_000_000)
            return (amount / 1_000_000m).ToString("N0", CultureInfo.InvariantCulture) + " tr";
        return amount.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    }
}


