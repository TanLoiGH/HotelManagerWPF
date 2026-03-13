using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class TienNghi
{
    public string MaTienNghi { get; set; } = null!;

    public string? MaNcc { get; set; }

    public string TenTienNghi { get; set; } = null!;

    public DateOnly? HanBaoHanh { get; set; }

    public int? TongSoLuong { get; set; }

    public string? DonViTinh { get; set; }

    public bool? IsActive { get; set; }

    public virtual NhaCungCap? MaNccNavigation { get; set; }

    public virtual ICollection<TienNghiPhong> TienNghiPhongs { get; set; } = new List<TienNghiPhong>();
}
