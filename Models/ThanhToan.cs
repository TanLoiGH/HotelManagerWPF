using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class ThanhToan
{
    public string MaThanhToan { get; set; } = null!;

    public string MaHoaDon { get; set; } = null!;

    public string? MaPttt { get; set; }

    public decimal SoTien { get; set; }

    public string? LoaiGiaoDich { get; set; }

    public DateTime? NgayThanhToan { get; set; }

    public string? NguoiThu { get; set; }

    public string? NoiDung { get; set; }

    public virtual HoaDon MaHoaDonNavigation { get; set; } = null!;

    public virtual PhuongThucThanhToan? MaPtttNavigation { get; set; }

    public virtual NhanVien? NguoiThuNavigation { get; set; }
}


