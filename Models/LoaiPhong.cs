using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class LoaiPhong
{
    public string MaLoaiPhong { get; set; } = null!;

    public string? TenLoaiPhong { get; set; }

    public int? SoNguoiToiDa { get; set; }

    public decimal GiaPhong { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
