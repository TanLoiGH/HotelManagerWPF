using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class LoaiKhach
{
    public string MaLoaiKhach { get; set; } = null!;

    public string? TenLoaiKhach { get; set; }

    public decimal? NguongTichLuy { get; set; }

    public virtual ICollection<KhachHang> KhachHangs { get; set; } = new List<KhachHang>();

    public virtual ICollection<KhuyenMai> KhuyenMais { get; set; } = new List<KhuyenMai>();
}


