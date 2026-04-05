using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Models
{
    public class TienNghiDanhMuc
    {
        public string MaDanhMuc { get; set; } = "";
        public string TenDanhMuc { get; set; } = "";
        public bool? IsActive { get; set; }

        public virtual ICollection<TienNghi> TienNghis { get; set; } = new List<TienNghi>();

    }
}
