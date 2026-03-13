using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class ChiPhi
{
    public string MaChiPhi { get; set; } = null!;

    public string MaLoaiCp { get; set; } = null!;

    public string? MaNhanVien { get; set; }

    public string? MaNcc { get; set; }

    public string? MaPhong { get; set; }

    public string TenChiPhi { get; set; } = null!;

    public decimal SoTien { get; set; }

    public DateTime? NgayChiPhi { get; set; }

    public string? GhiChu { get; set; }

    public virtual LoaiChiPhi MaLoaiCpNavigation { get; set; } = null!;

    public virtual NhaCungCap? MaNccNavigation { get; set; }

    public virtual NhanVien? MaNhanVienNavigation { get; set; }

    public virtual Phong? MaPhongNavigation { get; set; }
}
