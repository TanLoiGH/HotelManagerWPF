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
        public Brush BarColor { get; set; } = Brushes.Blue;
        public string Tooltip { get; set; } = "";
    }
}
