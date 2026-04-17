using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IPhongService
    {
        Task CapNhatDanhSachTienNghiPhongAsync(string maPhong, List<string> selectedMaTienNghi);
        Task CapNhatPhongAsync(string maPhong, string maLoaiPhong, string maTrangThai);
        Task GoTienNghiKhoiPhongAsync(string maPhong, string maTienNghi);
        Task<List<DatPhongChiTiet>> LayChiTietDatPhongDangHoatDongAsync();
        Task<List<string>> LayDanhSachMaPhongAsync();
        Task<List<Phong>> LayDanhSachPhongChiTietAsync();
        Task<List<PhongQuanTriItem>> LayDanhSachPhongQuanTriAsync();
        Task<DatPhongChiTiet?> LayDatPhongChoNhanTheoPhongAsync(string maPhong);
        Task<List<LoaiPhong>> LayLoaiPhongAsync();
        Task<List<TienNghiPhong>> LayTienNghiPhongAsync(string maPhong);
        Task<List<TienNghiPhongQuanTriItem>> LayTienNghiPhongQuanTriAsync(string maPhong);
        Task<List<PhongTrangThai>> LayTrangThaiPhongAsync();
        Task TaoPhongAsync(string maPhong, string maLoaiPhong, string maTrangThai);
        Task XoaPhongAsync(string maPhong);
    }
}