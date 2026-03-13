using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class TienNghiTrangThai
{
    public string MaTrangThai { get; set; } = null!;

    public string? TenTrangThai { get; set; }

    public virtual ICollection<TienNghiPhong> TienNghiPhongs { get; set; } = new List<TienNghiPhong>();
}
