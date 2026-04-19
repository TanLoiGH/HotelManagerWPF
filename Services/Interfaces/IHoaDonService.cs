using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services; // Để nhận diện ThongTinThanhToan

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

public interface IHoaDonService
{
    Task<List<HoaDon>> LayHoaDonsAsync();
    Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon);
    Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon);
    Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync();
    Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon);
    Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon);
    Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null);
    Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null);
    Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null);
    Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong);
    Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon);
    Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent);
    Task HuyHoaDonAsync(string maHoaDon);
    Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon);
    Task<bool> ThanhToanAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null);
}