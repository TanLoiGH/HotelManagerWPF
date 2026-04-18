using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IKhachHangService
    {
        Task CapNhatAsync(string maKhachHang, string tenKhachHang, string? dienThoai, string? cccd, string? email, string? diaChi, string? maLoaiKhach);
        Task<bool> CoLichSuDatPhongAsync(string maKhachHang);
        Task<List<KhachHangViewModel>> GetListAsync(string? keyword = null);
        Task<List<LoaiKhach>> LayLoaiKhachAsync();
        Task<KhachHang?> LayTheoMaAsync(string maKhachHang);
        Task<List<KhachHang>> SearchKhachHangAsync(string keyword, int limit = 8);
        Task TaoMoiAsync(string tenKhachHang, string? dienThoai, string? cccd, string? email, string? diaChi, string? maLoaiKhach);
        Task<KhachHang> TimHoacTaoAsync(string tenKhachHang, string? dienThoai, string? cccd, string? email, string? maLoaiKhachMacDinh, string? diaChi = null, string? passport = null, string? visa = null, string? quocTich = null);
        Task XoaAsync(string maKhachHang);
    }
}