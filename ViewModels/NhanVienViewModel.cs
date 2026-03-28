using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class NhanVienViewModel
{
    public string MaNhanVien { get; set; } = "";
    public string TenNhanVien { get; set; } = "";
    public string ChucVu { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cccd { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string NgayVaoLamText { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public List<string> Quyen { get; set; } = [];
}


