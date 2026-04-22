using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class DoanhThuThangVM
    {
        public string ThangText { get; set; } = "";
        public double BarHeight { get; set; }
        public System.Drawing.Brush BarColor { get; set; } = System.Drawing.Brushes.Blue;
        public string Tooltip { get; set; } = "";
    }
}