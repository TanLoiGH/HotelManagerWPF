using System;
using System.Collections.Generic;
using System.Text;
using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.Models
{
    public class HeThongNhatKy
    {
        public int Id { get; set; }
        public string? MaNhanVien { get; set; }
        public string? ThaoTac { get; set; }
        public string? ChiTiet { get; set; }
        public DateTime ThoiGian { get; set; } = TimeHelper.GetVietnamTime();
        public string? IpAddress { get; set; }

        public virtual NhanVien? MaNhanVienNavigation { get; set; }
    }
}
