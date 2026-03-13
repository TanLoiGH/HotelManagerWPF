using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class KhachHang
{
    public string MaKhachHang { get; set; } = null!;

    public string TenKhachHang { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public string? DiaChi { get; set; }

    public string? MaLoaiKhach { get; set; }

    public decimal? TongTichLuy { get; set; }

    public virtual ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();

    public virtual LoaiKhach? MaLoaiKhachNavigation { get; set; }
}
