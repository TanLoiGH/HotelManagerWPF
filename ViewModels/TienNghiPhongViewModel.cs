using System;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class TienNghiPhongViewModel
{
    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";
    public DateOnly? HanBaoHanh { get; set; }
    public string TenNCC { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public string MaTrangThai { get; set; } = "TNTT01";
    public bool CanBaoTri { get; set; }
    public string HanBaoHanhText => HanBaoHanh.HasValue
        ? HanBaoHanh.Value.ToString("dd/MM/yyyy") : "—";
}