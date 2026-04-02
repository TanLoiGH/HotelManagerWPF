using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Reports.DTO
{
    public class ReportItem
    {
        public string FullName { get; set; }  // "dbo.SP_BAO_CAO_DoanhThu"
        public string DisplayName { get; set; } // "Báo Cáo Doanh Thu"

        public override string ToString() => DisplayName;
    }
}
