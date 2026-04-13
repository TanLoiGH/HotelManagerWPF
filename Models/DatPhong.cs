using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class DatPhong
{
    public string MaDatPhong { get; set; } = null!;

    public string? MaKhachHang { get; set; }

    public decimal? TienCoc { get; set; }

    public DateTime? NgayDat { get; set; }

    public string? TrangThai { get; set; }
    
    public string? MaNhanVien { get; set; }

    public virtual NhanVien? MaNhanVienNavigation { get; set; }

    public virtual ICollection<DatPhongChiTiet> DatPhongChiTiets { get; set; } = [];

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhachHang? MaKhachHangNavigation { get; set; }
}


