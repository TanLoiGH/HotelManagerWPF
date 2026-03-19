using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class Phong
{
    public string MaPhong { get; set; } = null!;

    public string MaLoaiPhong { get; set; } = null!;

    public string? MaTrangThaiPhong { get; set; }

    public virtual ICollection<ChiPhi> ChiPhis { get; set; } = new List<ChiPhi>();

    public virtual ICollection<DatPhongChiTiet> DatPhongChiTiets { get; set; } = new List<DatPhongChiTiet>();

    public virtual LoaiPhong MaLoaiPhongNavigation { get; set; } = null!;

    public virtual PhongTrangThai? MaTrangThaiPhongNavigation { get; set; }

    public virtual ICollection<TienNghiPhong> TienNghiPhongs { get; set; } = new List<TienNghiPhong>();
}


