using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IDatPhongService
    {
        Task CheckInAsync(string maDatPhong, string maNhanVienLeTan);
        Task DoiPhongAsync(string maDatPhong, string maPhongCu, string maPhongMoi);
        Task GiaHanAsync(string maDatPhong, string maPhong, DateTime ngayTraMoi);
        Task HoanThanhDonDepAsync(string maPhong);
        Task HuyDatPhongAsync(string maDatPhong, string maNhanVien);
        Task HuyDatPhongAsync(string maDatPhong, string lyDo, decimal tienHoanTra = 0);
        Task<DatPhong> TaoDatPhongAsync(string maKhachHang, List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms, string maNhanVien = null, decimal tienCoc = 0, int soNguoi = 1);
    }
}