using QuanLyKhachSan_PhamTanLoi.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Views;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

public interface IChonDichVuService
{
    (string MaDichVu, int SoLuong)? ChonDichVu(Window? owner, List<DichVuViewModel> danhSachDichVu);
}

public sealed class ChonDichVuServiceWpf : IChonDichVuService
{
    public (string MaDichVu, int SoLuong)? ChonDichVu(Window? owner, List<DichVuViewModel> danhSachDichVu)
    {
        var dialog = new ThemDichVuDialog(danhSachDichVu) { Owner = owner };
        if (dialog.ShowDialog() == true && dialog.SelectedResult.HasValue)
            return dialog.SelectedResult.Value;
        return null;
    }
}

