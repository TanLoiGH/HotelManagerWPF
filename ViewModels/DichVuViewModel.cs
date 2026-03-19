namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class DichVuViewModel
{
    public string MaDichVu { get; set; } = "";
    public string TenDichVu { get; set; } = "";
    public decimal Gia { get; set; }
    public string DonViTinh { get; set; } = "";
    public string GiaText => Gia.ToString("N0") + " ₫";
}


