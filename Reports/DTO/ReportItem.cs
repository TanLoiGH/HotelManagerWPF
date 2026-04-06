namespace QuanLyKhachSan_PhamTanLoi.Reports.DTO
{
    public class ReportItem
    {
        public string FullName { get; set; } = string.Empty;  // "dbo.SP_BAO_CAO_DoanhThu"
        public string DisplayName { get; set; } = string.Empty; // "Báo Cáo Doanh Thu"

        public override string ToString() => DisplayName;
    }
}
