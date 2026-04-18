using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.IO;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public interface IInHoaDonService
{
    bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop, Window? owner = null);
    bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null);
}

public sealed class InHoaDonServiceWpf : IInHoaDonService
{
    public bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop, Window? owner = null)
    {
        try
        {
            // Tạo đường dẫn file tạm trong thư mục Temp của Windows
            string tempFile = Path.Combine(Path.GetTempPath(), $"HD_{hoaDon.MaHoaDon}_{DateTime.Now.Ticks}.pdf");

            QuestPdfHelper.XuatVaMoHoaDonPdf(hoaDon, tenKhachHang, tenNhanVien, tempFile, kieuIn, false);
            return true;
        }
            catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ConfirmHelper.ShowError($"Lỗi khi tạo PDF: {ex.Message}");
            return false;
        }
    }

    public bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null)
    {
        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"TamTinh_{hoaDon.MaHoaDon}_{DateTime.Now.Ticks}.pdf");

            // Tham số cuối laTamTinh = true để đổi tiêu đề bill
            QuestPdfHelper.XuatVaMoHoaDonPdf(hoaDon, tenKhachHang, tenNhanVien, tempFile, HoaDonChiTietViewModel.KieuInHoaDon.TongHop, true);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ConfirmHelper.ShowError($"Lỗi khi tạo PDF tạm tính: {ex.Message}");
            return false;
        }
    }
}