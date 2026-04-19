using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IHoaDonService
    {
        Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong);
        Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent);
        Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon);
        Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon);
        Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync();
        Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon);
        Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon);
        Task<List<HoaDon>> LayHoaDonsAsync();
        Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon);
        Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon);
        Task<bool> ThanhToanAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null);
        Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null);
        Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null);
        Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null);
        Task HuyHoaDonAsync(string maHoaDon);
    }
}