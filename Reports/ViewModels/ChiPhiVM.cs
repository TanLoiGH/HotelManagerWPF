using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class ChiPhiVM
    {
        public string Loai { get; set; } = "";
        public decimal Tong { get; set; }

        public double BarWidth { get; set; }

        public string TongText => Tong.ToString("N0") + " ₫";
    }
}
