using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class NhanVienViewModel
{
    public string MaNhanVien { get; set; } = "";
    public string TenNhanVien { get; set; } = "";
    public string ChucVu { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public List<string> Quyen { get; set; } = [];
}