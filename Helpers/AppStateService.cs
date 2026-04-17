namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public class AppStateService
    {
        // --- STATE: SƠ ĐỒ PHÒNG ---
        public string? SelectedMaPhong { get; set; }
        public string? SoDoSearchText { get; set; }
        public string? SoDoFilterTag { get; set; } = "all";
        public double? SoDoScrollOffset { get; set; }
        public bool IsMultiSelectMode { get; set; }

        // --- STATE: QUẢN LÝ HÓA ĐƠN ---
        public string? SelectedMaHoaDon { get; set; }
        public string? HoaDonSearchText { get; set; }
        public DateTime? HoaDonTuNgay { get; set; } = DateTime.Today.AddDays(-7);
        public DateTime? HoaDonDenNgay { get; set; } = DateTime.Today;
        public string? HoaDonFilterTrangThai { get; set; } = "Tất cả";

        // --- STATE: QUẢN LÝ DỊCH VỤ / KHÁCH HÀNG ---
        public string? SelectedMaDichVu { get; set; }
        public string? SelectedMaKhachHang { get; set; }
        public string? SelectedMaNhanVien { get; set; }

        // --- STATE: HỆ THỐNG ---
        public string? LastActiveTab { get; set; }
        public void Reset()
        {
            // Reset Sơ đồ
            SelectedMaPhong = null;
            SoDoSearchText = null;
            SoDoFilterTag = "all";
            SoDoScrollOffset = null;
            IsMultiSelectMode = false;

            // Reset Hóa đơn
            SelectedMaHoaDon = null;
            HoaDonSearchText = null;
            HoaDonTuNgay = DateTime.Today.AddDays(-7);
            HoaDonDenNgay = DateTime.Today;
            HoaDonFilterTrangThai = "Tất cả";

            // Reset các lựa chọn khác
            SelectedMaDichVu = null;
            SelectedMaKhachHang = null;
            SelectedMaNhanVien = null;
            LastActiveTab = null;
        }
    }
}