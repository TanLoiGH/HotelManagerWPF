using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class NhaCungCap
{
    public string MaNcc { get; set; } = null!;

    public string TenNcc { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? NguoiLienHe { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ChiPhi> ChiPhis { get; set; } = new List<ChiPhi>();

    public virtual ICollection<TienNghi> TienNghis { get; set; } = new List<TienNghi>();
}
