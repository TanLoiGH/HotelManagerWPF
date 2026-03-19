using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class KhuyenMai
{
    public string MaKhuyenMai { get; set; } = null!;

    public string? TenKhuyenMai { get; set; }

    public string? LoaiKhuyenMai { get; set; }

    public decimal? GiaTriKm { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public decimal? GiaTriToiThieu { get; set; }

    public string? MaLoaiKhach { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual LoaiKhach? MaLoaiKhachNavigation { get; set; }
}


