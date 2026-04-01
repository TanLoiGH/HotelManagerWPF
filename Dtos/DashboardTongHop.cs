using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Dtos
{
    public class VwDoanhThuThang
    {
        public int Nam { get; set; }
        public int Thang { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class VwCoCauChiPhi
    {
        public string TenLoaiCP { get; set; } = "";
        public decimal TongChiPhi { get; set; }
    }

    public class VwTopDichVu
    {
        public string TenDichVu { get; set; } = "";
        public int TongSuDung { get; set; }
    }
}
