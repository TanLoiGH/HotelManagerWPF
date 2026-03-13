using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class TaiKhoan
{
    public string MaNhanVien { get; set; } = null!;

    public string MaQuyen { get; set; } = null!;

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual NhanVien MaNhanVienNavigation { get; set; } = null!;

    public virtual PhanQuyen MaQuyenNavigation { get; set; } = null!;
}
