using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class DatPhong
{
    public string MaDatPhong { get; set; } = null!;

    public string? MaKhachHang { get; set; }

    public DateTime? NgayDat { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DatPhongChiTiet> DatPhongChiTiets { get; set; } = new List<DatPhongChiTiet>();

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang? MaKhachHangNavigation { get; set; }
}


