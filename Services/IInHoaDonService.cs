using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public interface IInHoaDonService
{
    bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null);
    bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null);
}

public sealed class InHoaDonServiceWpf : IInHoaDonService
{
    public bool XemTruocVaInHoaDon(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null)
    {
        var doc = PrintHelper.BuildPOSInvoiceDocument(hoaDon, tenKhachHang, tenNhanVien);
        return PrintHelper.PreviewAndPrint(doc, $"HD_{hoaDon.MaHoaDon}", owner);
    }

    public bool XemTruocVaInTamTinh(HoaDon hoaDon, string tenKhachHang, string tenNhanVien, Window? owner = null)
    {
        var doc = PrintHelper.BuildTamTinhDocument(hoaDon, tenKhachHang, tenNhanVien);
        return PrintHelper.PreviewAndPrint(doc, $"TAM_TINH_{hoaDon.MaHoaDon}", owner);
    }
}

