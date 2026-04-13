using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels; // THÊM DÒNG NÀY ĐỂ NHẬN DIỆN KieuInHoaDon
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public interface IInHoaDonService
{
    // Bổ sung KieuInHoaDon vào tham số thứ 4
    bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop, Window? owner = null);
    bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null);
}

public sealed class InHoaDonServiceWpf : IInHoaDonService
{
    // Bổ sung tương tự vào phần implement
    public bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop, Window? owner = null)
    {
        // Tạm thời vẫn truyền vào hàm Build cũ, ở bước tiếp theo khi bạn làm giao diện in chúng ta sẽ truyền kieuIn này vào PrintHelper
        var doc = PrintHelper.BuildPOSInvoiceDocument(hoaDon, tenKhachHang, tenNhanVien);
        return PrintHelper.PreviewAndPrint(doc, $"HD_{hoaDon.MaHoaDon}", owner);
    }

    public bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null)
    {
        var doc = PrintHelper.BuildTamTinhDocument(hoaDon, tenKhachHang, tenNhanVien);
        return PrintHelper.PreviewAndPrint(doc, $"TAM_TINH_{hoaDon.MaHoaDon}", owner);
    }
}