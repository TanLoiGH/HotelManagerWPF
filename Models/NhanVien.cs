using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class NhanVien
{
    public string MaNhanVien { get; set; } = null!;

    public string TenNhanVien { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public string? ChucVu { get; set; }

    public string? DiaChi { get; set; }

    public DateOnly? NgayVaoLam { get; set; }

    public string? MaTrangThai { get; set; }
    public bool? IsActive { get; set; }

    public virtual ICollection<ChiPhi> ChiPhis { get; set; } = new List<ChiPhi>();

    public virtual ICollection<DatPhongChiTiet> DatPhongChiTiets { get; set; } = new List<DatPhongChiTiet>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();



    public virtual TrangThaiNhanVien? MaTrangThaiNavigation { get; set; }

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}


