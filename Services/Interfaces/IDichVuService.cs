using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IDichVuService
    {
        Task CapNhatAsync(string maDichVu, string tenDichVu, decimal gia, string? donViTinh, bool isActive);
        Task<List<DichVu>> GetAllDichVuAsync();
        Task<List<DichVuChiTiet>> GetDichVuChiTietsForHoaDonAsync(string maHoaDon);
        Task<List<DichVu>> GetDichVusAsync();
        Task<List<DichVu>> LayDanhSachAsync();
        Task TaoMoiAsync(string tenDichVu, decimal gia, string? donViTinh, bool isActive);
        Task UpsertDichVuAsync(string maHoaDon, string maDatPhong, string maPhong, string maDichVu, int soLuong);
        Task<bool> XoaHoacTatAsync(string maDichVu);
    }
}