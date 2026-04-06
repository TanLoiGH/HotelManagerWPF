// ===========================================================================
// GhiChiPhiDialog.xaml.cs
// CHI_PHI + LOAI_CHI_PHI + NHA_CUNG_CAP (optional) + PHONG (optional)
// ===========================================================================
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using System.Windows;


namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class GhiChiPhiDialog : Window
{
    public (string MaLoaiCP, string TenChiPhi, decimal SoTien,
            string? MaNCC, string? MaPhong, string? GhiChu)? Result
    { get; private set; }

    public GhiChiPhiDialog(List<LoaiChiPhiDto> loaiCPs)
    {
        InitializeComponent();
        CboLoaiCP.ItemsSource = loaiCPs;
        CboLoaiCP.DisplayMemberPath = "TenLoaiCP";
        CboLoaiCP.SelectedValuePath = "MaLoaiCP";
        CboLoaiCP.SelectedIndex = 0;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await LoadNhaCungCapAsync();
        await LoadPhongAsync();
    }

    // NHA_CUNG_CAP
    private async Task LoadNhaCungCapAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var nccSvc = new NhaCungCapService(db);
        var nccs = await nccSvc.LayDanhSachAsync();
        CboNCC.ItemsSource = nccs.Where(n => n.IsActive ?? false).Select(n => new { n.MaNcc, n.TenNcc }).ToList();
        CboNCC.DisplayMemberPath = "TenNcc";
        CboNCC.SelectedValuePath = "MaNcc";
    }

    // PHONG (chọn phòng nếu chi phí liên quan đến phòng)
    private async Task LoadPhongAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var roomSvc = new RoomService(db);
        var phongs = await roomSvc.LayDanhSachMaPhongAsync();
        CboPhong.ItemsSource = phongs;
    }

    private void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        if (CboLoaiCP.SelectedValue is not string maLoaiCP)
        {
            MessageBox.Show("Chọn loại chi phí.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtSoTien.Text.Replace(",", "").Replace(".", ""),
            out decimal soTien) || soTien <= 0)
        {
            ConfirmHelper.ShowWarning("Số tiền không hợp lệ.");
            return;
        }

        if (!ConfirmHelper.Confirm("Bạn có chắc chắn muốn ghi nhận chi phí này không?", "Xác nhận lưu"))
            return;

        Result = (
            maLoaiCP,
            TxtTenChiPhi.Text.Trim(),
            soTien,
            CboNCC.SelectedValue as string,
            CboPhong.SelectedValue as string,
            TxtGhiChu.Text.Trim()
        );
        DialogResult = true;
        Close();
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}



