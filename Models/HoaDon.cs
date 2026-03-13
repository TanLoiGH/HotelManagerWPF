using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class HoaDon
{
    public string MaHoaDon { get; set; } = null!;

    public string? MaDatPhong { get; set; }

    public string? MaNhanVien { get; set; }

    public DateTime? NgayLap { get; set; }

    public decimal? TienPhong { get; set; }

    public decimal? TienDichVu { get; set; }

    public decimal? Vat { get; set; }

    public string? MaKhuyenMai { get; set; }

    public decimal? TongThanhToan { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DichVuChiTiet> DichVuChiTiets { get; set; } = new List<DichVuChiTiet>();

    public virtual ICollection<HoaDonChiTiet> HoaDonChiTiets { get; set; } = new List<HoaDonChiTiet>();

    public virtual DatPhong? MaDatPhongNavigation { get; set; }

    public virtual KhuyenMai? MaKhuyenMaiNavigation { get; set; }

    public virtual NhanVien? MaNhanVienNavigation { get; set; }

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
