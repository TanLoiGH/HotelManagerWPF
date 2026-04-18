using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

public interface IChonDichVuService
{
    // Thêm tham số danhSachMaPhong
    (string MaDichVu, int SoLuong, string MaPhong)? ChonDichVu(Window? owner, List<DichVuViewModel> danhSachDichVu, List<string> danhSachMaPhong);
}

public sealed class ChonDichVuServiceWpf : IChonDichVuService
{
    public (string MaDichVu, int SoLuong, string MaPhong)? ChonDichVu(Window? owner, List<DichVuViewModel> danhSachDichVu, List<string> danhSachMaPhong)
    {
        // Truyền danh sách phòng vào Constructor của Dialog
        var dialog = new ThemDichVuDialog(danhSachDichVu, danhSachMaPhong) { Owner = owner };
        if (dialog.ShowDialog() == true && dialog.SelectedResult.HasValue)
            return dialog.SelectedResult.Value;
        return null;
    }
}

