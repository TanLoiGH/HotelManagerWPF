using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class PhongStatusVM
    {
        public string TenTT { get; set; } = "";
        public int SoPhong { get; set; }
        public System.Drawing.Brush MauSac { get; set; } = System.Drawing.Brushes.Gray;
        public double BarWidth { get; set; }
    }
}