// ===========================================================================
// ThanhToanDialog.xaml.cs
// THANH_TOAN + PHUONG_THUC_THANH_TOAN
// ===========================================================================
using QuanLyKhachSan_PhamTanLoi;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;
using Microsoft.EntityFrameworkCore;


namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class ThanhToanDialog : Window
{
    private readonly string _maHoaDon;
    private readonly HoaDonService _hdSvc;
    private readonly QuanLyKhachSanContext _db;

    public ThanhToanDialog(string maHoaDon, List<PhuongThucThanhToanDto> ptttList)
    {
        InitializeComponent();
        _maHoaDon = maHoaDon;
        _db = new QuanLyKhachSanContext();
        _hdSvc = new HoaDonService(_db, new KhachHangService(_db));

        CboPTTT.ItemsSource = ptttList;
        CboPTTT.DisplayMemberPath = "TenPhuongThuc";
        CboPTTT.SelectedValuePath = "MaPTTT";
        CboPTTT.SelectedIndex = 0;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await LoadHoaDonInfoAsync();
    }

    private async Task LoadHoaDonInfoAsync()
    {
        using var db = new QuanLyKhachSanContext();

        // HOA_DON + HOA_DON_CHI_TIET + DICH_VU_CHI_TIET
        var hd = await db.HoaDons
            .Include(h => h.HoaDonChiTiets)       // HOA_DON_CHI_TIET
            .Include(h => h.DichVuChiTiets)        // DICH_VU_CHI_TIET
                .ThenInclude(d => d.MaDichVuNavigation)
            .Include(h => h.MaKhuyenMaiNavigation) // KHUYEN_MAI
            .FirstOrDefaultAsync(h => h.MaHoaDon == _maHoaDon);

        if (hd == null) return;

        TxtMaHD.Text = hd.MaHoaDon;
        TxtTienPhong.Text = (hd.TienPhong ?? 0).ToString("N0") + " ₫";
        TxtTienDV.Text = (hd.TienDichVu ?? 0).ToString("N0") + " ₫";
        TxtVAT.Text = $"{hd.Vat ?? 10}%";
        TxtKhuyenMai.Text = hd.MaKhuyenMaiNavigation?.TenKhuyenMai ?? "Không";
        TxtTong.Text = (hd.TongThanhToan ?? 0).ToString("N0") + " ₫";
        TxtSoTien.Text = (hd.TongThanhToan ?? 0).ToString("N0");

        // Hiển thị lịch sử thanh toán (THANH_TOAN)
        var tts = await db.ThanhToans
            .Include(t => t.MaPtttNavigation)  // PHUONG_THUC_THANH_TOAN
            .Where(t => t.MaHoaDon == _maHoaDon)
            .OrderBy(t => t.NgayThanhToan)
            .ToListAsync();

        ListThanhToan.ItemsSource = tts;
        PanelLichSu.Visibility = tts.Any() ? Visibility.Visible : Visibility.Collapsed;

        decimal tongDaThu = tts.Sum(t => t.SoTien);
        decimal conLai = (hd.TongThanhToan ?? 0) - tongDaThu;
        TxtConLai.Text = $"Còn lại: {conLai:N0} ₫";
        if (conLai > 0) TxtSoTien.Text = conLai.ToString("N0");
    }

    private async void BtnThanhToan_Click(object sender, RoutedEventArgs e)
    {
        if (CboPTTT.SelectedValue is not string maPTTT) return;
        if (!decimal.TryParse(TxtSoTien.Text.Replace(",", "").Replace(".", ""),
            out decimal soTien) || soTien <= 0)
        {
            MessageBox.Show("Số tiền không hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string maNhanVien = App.CurrentUser?.MaNhanVien ?? "NV001";
        string loai = CboLoaiGD.SelectedIndex == 0
            ? "Thanh toán cuối" : CboLoaiGD.SelectedItem?.ToString() ?? "Thanh toán cuối";

        try
        {
            BtnThanhToan.IsEnabled = false;
            bool daThuDu = await _hdSvc.ThanhToanAsync(
                _maHoaDon, soTien, maPTTT, maNhanVien, loai, TxtNoiDung.Text);

            if (daThuDu)
            {
                MessageBox.Show("Thanh toán hoàn tất! Phòng sẽ được chuyển sang trạng thái cần dọn dẹp.",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show($"Đã ghi nhận thanh toán {soTien:N0} ₫. Khách chưa thanh toán đủ.",
                    "Ghi nhận", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadHoaDonInfoAsync();
                BtnThanhToan.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi thanh toán:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            BtnThanhToan.IsEnabled = true;
        }
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    // Thêm vào cuối class:
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _db.Dispose();
    }
}