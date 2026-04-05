using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class PhuongThucThanhToan
{
    public string MaPttt { get; set; } = null!;

    public string TenPhuongThuc { get; set; } = null!;
    public bool? IsActive { get; set; }

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}


