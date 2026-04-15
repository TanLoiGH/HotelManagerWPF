using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly DashboardService _dashboardService;
    private DateTime _tuNgay = new(TimeHelper.GetVietnamTime().Year, TimeHelper.GetVietnamTime().Month, 1);
    private DateTime _denNgay = TimeHelper.GetVietnamTime().Date;

    private string _doanhThuText = "0 ₫";
    private string _chiPhiText = "0 ₫";
    private string _loiNhuanText = "0 ₫";
    private string _tongPhongText = "0";
    private string _congSuatText = "0%";
    private SolidColorBrush _loiNhuanForeground = new(Colors.White);

    public DashboardViewModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;

        LoadCommand = new RelayCommand(async _ => await LoadDataAsync());
        FilterThangNayCommand = new RelayCommand(_ => { TuNgay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); DenNgay = DateTime.Today; });
        FilterQuyNayCommand = new RelayCommand(_ => { int quy = (DateTime.Now.Month - 1) / 3 + 1; TuNgay = new DateTime(DateTime.Now.Year, (quy - 1) * 3 + 1, 1); DenNgay = DateTime.Today; });
        FilterNamNayCommand = new RelayCommand(_ => { TuNgay = new DateTime(DateTime.Now.Year, 1, 1); DenNgay = DateTime.Today; });
    }

    // Properties
    public DateTime TuNgay { get => _tuNgay; set { if (SetProperty(ref _tuNgay, value)) _ = LoadDataAsync(); } }
    public DateTime DenNgay { get => _denNgay; set { if (SetProperty(ref _denNgay, value)) _ = LoadDataAsync(); } }

    public string DoanhThuText { get => _doanhThuText; set => SetProperty(ref _doanhThuText, value); }
    public string ChiPhiText { get => _chiPhiText; set => SetProperty(ref _chiPhiText, value); }
    public string LoiNhuanText { get => _loiNhuanText; set => SetProperty(ref _loiNhuanText, value); }
    public string TongPhongText { get => _tongPhongText; set => SetProperty(ref _tongPhongText, value); }
    public string CongSuatText { get => _congSuatText; set => SetProperty(ref _congSuatText, value); }
    public SolidColorBrush LoiNhuanForeground { get => _loiNhuanForeground; set => SetProperty(ref _loiNhuanForeground, value); }

    public ObservableCollection<BarChartItem> MonthlyRevenue { get; } = new();
    public ObservableCollection<PhongStatusItem> RoomStatusStats { get; } = new();
    public ObservableCollection<TopDichVuItem> TopServices { get; } = new();
    public ObservableCollection<ChiPhiItem> ExpenseStats { get; } = new();
    public ObservableCollection<DashboardBookingItem> RecentBookings { get; } = new();

    // Commands
    public ICommand LoadCommand { get; }
    public ICommand FilterThangNayCommand { get; }
    public ICommand FilterQuyNayCommand { get; }
    public ICommand FilterNamNayCommand { get; }

    // Methods
    public async Task LoadDataAsync()
    {
        try
        {
            var doanhThu = await _dashboardService.GetDoanhThuAsync(TuNgay, DenNgay);
            var chiPhi = await _dashboardService.GetChiPhiAsync(TuNgay, DenNgay);
            var (tongPhong, phongDangO) = await _dashboardService.GetRoomStatsAsync();
            var congSuat = tongPhong > 0 ? (double)phongDangO / tongPhong * 100 : 0;

            DoanhThuText = FormatVnd(doanhThu);
            ChiPhiText = FormatVnd(chiPhi);
            LoiNhuanText = FormatVnd(doanhThu - chiPhi);
            TongPhongText = tongPhong.ToString();
            CongSuatText = $"{congSuat:N0}%";
            LoiNhuanForeground = (doanhThu - chiPhi >= 0) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Color.FromRgb(255, 200, 200));

            // Chart
            var monthlyData = await _dashboardService.GetMonthlyRevenueAsync();
            decimal maxRev = monthlyData.Any() ? monthlyData.Max(x => x.Total) : 1;
            if (maxRev == 0) maxRev = 1;

            MonthlyRevenue.Clear();
            foreach (var (year, month, total) in monthlyData)
            {
                bool isCurrent = year == DateTime.Now.Year && month == DateTime.Now.Month;
                MonthlyRevenue.Add(new BarChartItem
                {
                    ThangText = $"T{month}",
                    BarHeight = Math.Max(4, (double)(total / maxRev) * 160),
                    GiaTriGoc = total,
                    Tooltip = $"Tháng {month}/{year}: {FormatVnd(total)}",
                    BarColor = new SolidColorBrush(isCurrent ? Color.FromRgb(37, 99, 235) : Color.FromRgb(147, 197, 253))
                });
            }

            // Room Distribution
            var roomStats = await _dashboardService.GetRoomStatusDistributionAsync();
            RoomStatusStats.Clear();
            foreach (var s in roomStats)
            {
                RoomStatusStats.Add(new PhongStatusItem
                {
                    TenTT = s.TenTrangThai,
                    SoPhong = s.Count,
                    BarWidth = (double)s.Count / (tongPhong > 0 ? tongPhong : 1) * 80,
                    MauSac = new SolidColorBrush(s.MaTrangThai switch
                    {
                        "PTT01" => Color.FromRgb(16, 185, 129),
                        "PTT02" => Color.FromRgb(225, 29, 72),
                        "PTT03" => Color.FromRgb(245, 158, 11),
                        "PTT04" => Color.FromRgb(100, 116, 139),
                        "PTT05" => Color.FromRgb(99, 102, 241),
                        _ => Color.FromRgb(100, 116, 139),
                    })
                });
            }

            // Top Services
            var topSvcs = await _dashboardService.GetTopServicesAsync(TuNgay, DenNgay);
            TopServices.Clear();
            foreach (var s in topSvcs) TopServices.Add(new TopDichVuItem { TenDV = s.TenDichVu, SoLan = s.Count });

            // Expenses
            var expenses = await _dashboardService.GetExpenseDistributionAsync(TuNgay, DenNgay);
            decimal maxExp = expenses.Any() ? expenses.Max(x => x.Total) : 1;
            ExpenseStats.Clear();
            foreach (var e in expenses) ExpenseStats.Add(new ChiPhiItem { Loai = e.Loai, Tong = e.Total, BarWidth = (double)(e.Total / (maxExp > 0 ? maxExp : 1)) * 200 });

            // Recent Bookings
            var bookings = await _dashboardService.GetRecentBookingsAsync();
            RecentBookings.Clear();
            foreach (var b in bookings)
            {
                RecentBookings.Add(new DashboardBookingItem
                {
                    MaDatPhong = b.MaDatPhong,
                    TenKhachHang = b.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
                    TrangThai = b.TrangThai ?? ""
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dashboard: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string FormatVnd(decimal amount)
    {
        if (amount >= 1_000_000_000) return (amount / 1_000_000_000m).ToString("N1", CultureInfo.InvariantCulture) + " tỷ";
        if (amount >= 1_000_000) return (amount / 1_000_000m).ToString("N0", CultureInfo.InvariantCulture) + " tr";
        return amount.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    }
}

public class BarChartItem { public string ThangText { get; set; } = ""; public double BarHeight { get; set; } public decimal GiaTriGoc { get; set; } public string Tooltip { get; set; } = ""; public SolidColorBrush BarColor { get; set; } = new(Color.FromRgb(0, 120, 212)); }
public class PhongStatusItem { public string TenTT { get; set; } = ""; public int SoPhong { get; set; } public double BarWidth { get; set; } public SolidColorBrush MauSac { get; set; } = new(Colors.Gray); }
public class TopDichVuItem { public string TenDV { get; set; } = ""; public int SoLan { get; set; } }
public class ChiPhiItem { public string Loai { get; set; } = ""; public decimal Tong { get; set; } public double BarWidth { get; set; } public string TongText => Tong.ToString("N0") + " ₫"; }
public class DashboardBookingItem { public string MaDatPhong { get; set; } = ""; public string TenKhachHang { get; set; } = ""; public string TrangThai { get; set; } = ""; public SolidColorBrush StatusColor => TrangThai switch { "Chờ nhận phòng" => new SolidColorBrush(Color.FromRgb(99, 102, 241)), "Đang ở" => new SolidColorBrush(Color.FromRgb(16, 185, 129)), "Đã trả phòng" => new SolidColorBrush(Color.FromRgb(100, 116, 139)), _ => new SolidColorBrush(Color.FromRgb(37, 99, 235)), }; }
