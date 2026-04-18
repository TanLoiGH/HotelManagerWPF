using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class ChiPhiRow
{
    public string MaChiPhi { get; set; } = "";
    public string TenChiPhi { get; set; } = "";
    public string TenLoaiCp { get; set; } = "";
    public decimal SoTien { get; set; }
    public DateTime? NgayChiPhi { get; set; }
    public string TenNcc { get; set; } = "";
    public string MaPhong { get; set; } = "";
    public string GhiChu { get; set; } = "";
    public string SoTienText => SoTien.ToString("N0") + " ₫";
    public string NgayText => NgayChiPhi?.ToString("dd/MM/yyyy") ?? "";
}

public partial class ChiPhiPage : Page
{
    public ChiPhiPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            DpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpDenNgay.SelectedDate = DateTime.Today;
        };
    }

    private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DpTuNgay?.SelectedDate == null || DpDenNgay?.SelectedDate == null) return;
        await LoadAsync(DpTuNgay.SelectedDate.Value, DpDenNgay.SelectedDate.Value);
    }

    private async Task LoadAsync(DateTime tu, DateTime den)
    {
        try
        {
            using var db = new QuanLyKhachSanContext();
            var chiPhiSvc = new ChiPhiService(db);

            var items = await chiPhiSvc.LayChiPhiTheoNgayAsync(tu, den);
            var rows = items.Select(c => new ChiPhiRow
            {
                MaChiPhi = c.MaChiPhi,
                TenChiPhi = c.TenChiPhi,
                TenLoaiCp = c.TenLoaiCp,
                SoTien = c.SoTien,
                NgayChiPhi = c.NgayChiPhi,
                TenNcc = c.TenNcc,
                MaPhong = c.MaPhong,
                GhiChu = c.GhiChu,
            }).ToList();

            CpGrid.ItemsSource = rows;

            decimal tong = rows.Sum(r => r.SoTien);
            TxtTongChiPhi.Text = $"Tổng: {tong:N0} ₫";

            // Summary theo loại
            SummaryItems.ItemsSource = rows
                .GroupBy(r => r.TenLoaiCp)
                .Select(g => new ChiPhiSummary
                {
                    Loai = g.Key,
                    Tong = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(x => x.Tong)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        using var db = new QuanLyKhachSanContext();
        var chiPhiSvc = new ChiPhiService(db);
        var loaiCPs = await chiPhiSvc.GetLoaiChiPhiAsync();

        var dialog = new GhiChiPhiDialog(loaiCPs) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.Result.HasValue)
        {
            var r = dialog.Result.Value;
            await chiPhiSvc.GhiChiPhiAsync(
                r.MaLoaiCP, r.TenChiPhi, r.SoTien,
                AppSession.MaNhanVien,
                r.MaNCC, r.MaPhong, r.GhiChu);

            if (DpTuNgay.SelectedDate.HasValue && DpDenNgay.SelectedDate.HasValue)
                await LoadAsync(DpTuNgay.SelectedDate.Value, DpDenNgay.SelectedDate.Value);
        }
    }
}




