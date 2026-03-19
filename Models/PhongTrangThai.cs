using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class PhongTrangThai
{
    public string MaTrangThaiPhong { get; set; } = null!;

    public string? TenTrangThai { get; set; }

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}


