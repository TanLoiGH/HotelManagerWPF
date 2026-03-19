using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class DatPhongChiTiet
{
    public string MaDatPhong { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public DateTime NgayNhan { get; set; }

    public DateTime NgayTra { get; set; }

    public decimal DonGia { get; set; }

    public string? MaNhanVien { get; set; }

    public virtual ICollection<DichVuChiTiet> DichVuChiTiets { get; set; } = new List<DichVuChiTiet>();

    public virtual ICollection<HoaDonChiTiet> HoaDonChiTiets { get; set; } = new List<HoaDonChiTiet>();

    public virtual DatPhong MaDatPhongNavigation { get; set; } = null!;

    public virtual NhanVien? MaNhanVienNavigation { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;
}


