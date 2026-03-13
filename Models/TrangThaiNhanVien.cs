using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.Models;

public partial class TrangThaiNhanVien
{
    public string MaTrangThai { get; set; } = null!;

    public string TenTrangThai { get; set; } = null!;

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
