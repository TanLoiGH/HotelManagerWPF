using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class HoaDonChiTiet
{
    public string MaHoaDon { get; set; } = null!;

    public string MaDatPhong { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public int SoDem { get; set; }

    public virtual DatPhongChiTiet DatPhongChiTiet { get; set; } = null!;

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;
}


