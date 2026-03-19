using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class DichVu
{
    public string MaDichVu { get; set; } = null!;

    public string TenDichVu { get; set; } = null!;

    public decimal? Gia { get; set; }

    public string? DonViTinh { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<DichVuChiTiet> DichVuChiTiets { get; set; } = new List<DichVuChiTiet>();
}


