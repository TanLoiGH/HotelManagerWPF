using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class PhongStatusVM
    {
        public string TenTT { get; set; } = "";
        public int SoPhong { get; set; }
        public Brush MauSac { get; set; } = Brushes.Gray;
        public double BarWidth { get; set; }
    }
}
