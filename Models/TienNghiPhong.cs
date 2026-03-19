using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class TienNghiPhong
{
    public string MaPhong { get; set; } = null!;

    public string MaTienNghi { get; set; } = null!;

    public string? MaTrangThai { get; set; }

    public virtual Phong MaPhongNavigation { get; set; } = null!;

    public virtual TienNghi MaTienNghiNavigation { get; set; } = null!;

    public virtual TienNghiTrangThai? MaTrangThaiNavigation { get; set; }
}


