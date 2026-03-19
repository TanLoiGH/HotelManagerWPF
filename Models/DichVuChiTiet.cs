using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class DichVuChiTiet
{
    public string MaHoaDon { get; set; } = null!;

    public string MaDatPhong { get; set; } = null!;

    public string MaPhong { get; set; } = null!;

    public string MaDichVu { get; set; } = null!;

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public DateTime? NgaySuDung { get; set; }

    public virtual DatPhongChiTiet DatPhongChiTiet { get; set; } = null!;

    public virtual DichVu MaDichVuNavigation { get; set; } = null!;

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;
}


