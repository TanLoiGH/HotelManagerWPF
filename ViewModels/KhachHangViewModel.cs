namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class KhachHangViewModel
{
    public string MaKhachHang { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Cccd { get; set; } = "";
    public string TenLoaiKhach { get; set; } = "";
    public decimal TongTichLuy { get; set; }
    public string TichLuyText => TongTichLuy.ToString("N0") + " ₫";
}


