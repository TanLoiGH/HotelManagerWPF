using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class LoaiChiPhi
{
    public string MaLoaiCp { get; set; } = null!;

    public string TenLoaiCp { get; set; } = null!;
    public bool? IsActive { get; set; }
    public virtual ICollection<ChiPhi> ChiPhis { get; set; } = new List<ChiPhi>();
}


