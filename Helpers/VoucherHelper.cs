using System.Text;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

/// <summary>
/// Trợ giúp tạo các chứng từ liên quan đến tiền cọc (Vouchers)
/// </summary>
public static class VoucherHelper
{
    public static string GenerateDepositReceipt(DatPhong dp, string khName, string staffName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; padding: 20px; line-height: 1.6; color: #333; }");
        sb.AppendLine(".header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 10px; }");
        sb.AppendLine(".title { font-size: 24px; font-weight: bold; text-transform: uppercase; margin-bottom: 5px; }");
        sb.AppendLine(".info-grid { display: grid; grid-template-columns: 150px 1fr; gap: 10px; margin-bottom: 20px; }");
        sb.AppendLine(".label { font-weight: bold; }");
        sb.AppendLine(".amount { font-size: 20px; font-weight: bold; color: #d32f2f; margin: 20px 0; border: 1px dashed #333; padding: 10px; text-align: center; }");
        sb.AppendLine(".footer { display: flex; justify-content: space-around; margin-top: 50px; text-align: center; }");
        sb.AppendLine(".signature { height: 100px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<div class='title'>PHIẾU THU TIỀN ĐẶT CỌC</div>");
        sb.AppendLine($"<div>Số: {dp.MaDatPhong} | Ngày: {TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='info-grid'>");
        sb.AppendLine("<div class='label'>Khách hàng:</div>");
        sb.AppendLine($"<div>{khName}</div>");
        sb.AppendLine("<div class='label'>Lý do thu:</div>");
        sb.AppendLine($"<div>Đặt cọc giữ phòng cho lịch đặt {dp.MaDatPhong}</div>");
        sb.AppendLine("<div class='label'>Ngày dự kiến:</div>");
        sb.AppendLine($"<div>{dp.NgayDat:dd/MM/yyyy}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<div class='amount'>SỐ TIỀN: {dp.TienCoc:N0} VNĐ</div>");

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<div><div class='label'>NGƯỜI NỘP TIỀN</div><div class='signature'>(Ký, họ tên)</div></div>");
        sb.AppendLine($"<div><div class='label'>NHÂN VIÊN THU</div><div class='signature'>(Ký, họ tên)</div><br/>{staffName}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static string GenerateRefundReceipt(DatPhong dp, string khName, decimal amount, string reason, string staffName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; padding: 20px; line-height: 1.6; color: #333; }");
        sb.AppendLine(".header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 10px; }");
        sb.AppendLine(".title { font-size: 24px; font-weight: bold; text-transform: uppercase; margin-bottom: 5px; }");
        sb.AppendLine(".info-grid { display: grid; grid-template-columns: 150px 1fr; gap: 10px; margin-bottom: 20px; }");
        sb.AppendLine(".label { font-weight: bold; }");
        sb.AppendLine(".amount { font-size: 20px; font-weight: bold; color: #2e7d32; margin: 20px 0; border: 1px dashed #333; padding: 10px; text-align: center; }");
        sb.AppendLine(".footer { display: flex; justify-content: space-around; margin-top: 50px; text-align: center; }");
        sb.AppendLine(".signature { height: 100px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<div class='title'>PHIẾU CHI HOÀN TRẢ TIỀN CỌC</div>");
        sb.AppendLine($"<div>Số: {dp.MaDatPhong}-REF | Ngày: {TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='info-grid'>");
        sb.AppendLine("<div class='label'>Khách hàng:</div>");
        sb.AppendLine($"<div>{khName}</div>");
        sb.AppendLine("<div class='label'>Lý do hoàn:</div>");
        sb.AppendLine($"<div>{reason} (Từ đặt phòng {dp.MaDatPhong})</div>");
        sb.AppendLine("</div>");

        sb.AppendLine($"<div class='amount'>SỐ TIỀN HOÀN TRẢ: {amount:N0} VNĐ</div>");

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine("<div><div class='label'>NGƯỜI NHẬN TIỀN</div><div class='signature'>(Ký, họ tên)</div></div>");
        sb.AppendLine($"<div><div class='label'>NHÂN VIÊN CHI</div><div class='signature'>(Ký, họ tên)</div><br/>{staffName}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static string GeneratePOSInvoice(HoaDon hd, string khName, string staffName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: 'Courier New', Courier, monospace; width: 300px; padding: 10px; font-size: 12px; }");
        sb.AppendLine(".center { text-align: center; }");
        sb.AppendLine(".bold { font-weight: bold; }");
        sb.AppendLine(".line { border-bottom: 1px dashed #000; margin: 5px 0; }");
        sb.AppendLine(".flex { display: flex; justify-content: space-between; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='center bold' style='font-size: 16px;'>HOTELIFY</div>");
        sb.AppendLine("<div class='center'>Đ/C: 123 Đường ABC, Quận XYZ, TP.HCM</div>");
        sb.AppendLine("<div class='center'>SĐT: 0123.456.789</div>");
        sb.AppendLine("<div class='line'></div>");

        sb.AppendLine("<div class='center bold'>HÓA ĐƠN THANH TOÁN</div>");
        sb.AppendLine($"<div class='center'>Số: {hd.MaHoaDon}</div>");
        sb.AppendLine($"<div class='center'>Ngày: {TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</div>");
        sb.AppendLine("<div class='line'></div>");

        sb.AppendLine($"<div>Khách hàng: {khName}</div>");
        sb.AppendLine($"<div>Nhân viên: {staffName}</div>");
        sb.AppendLine("<div class='line'></div>");

        // Chi tiết phòng
        sb.AppendLine("<div class='bold'>Chi tiết:</div>");
        sb.AppendLine("<div class='flex'><span style='width: 150px;'>Nội dung</span><span>T.Tiền</span></div>");
        sb.AppendLine($"<div class='flex'><span>Tiền phòng</span><span>{hd.TienPhong:N0}</span></div>");

        if (hd.TienDichVu > 0)
            sb.AppendLine($"<div class='flex'><span>Tiền dịch vụ</span><span>{hd.TienDichVu:N0}</span></div>");

        sb.AppendLine("<div class='line'></div>");

        // Tổng cộng
        decimal tienGoc = (hd.TienPhong ?? 0) + (hd.TienDichVu ?? 0);
        sb.AppendLine($"<div class='flex'><span>Tổng cộng:</span><span>{tienGoc:N0}</span></div>");

        if (hd.MaKhuyenMaiNavigation != null)
            sb.AppendLine($"<div class='flex'><span>Khuyến mãi:</span><span>-{hd.MaKhuyenMaiNavigation.GiaTriKm:N0}</span></div>");

        sb.AppendLine($"<div class='flex'><span>Thuế VAT ({hd.Vat}%):</span><span>+{(hd.TongThanhToan * (hd.Vat / 100)):N0}</span></div>");

        decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        if (tienCoc > 0)
            sb.AppendLine($"<div class='flex'><span>Đã cọc:</span><span>-{tienCoc:N0}</span></div>");

        sb.AppendLine("<div class='line'></div>");
        sb.AppendLine($"<div class='flex bold' style='font-size: 14px;'><span>THÀNH TIỀN:</span><span>{hd.TongThanhToan:N0} VNĐ</span></div>");
        sb.AppendLine("<div class='line'></div>");

        sb.AppendLine("<div class='center'>Cảm ơn quý khách!</div>");
        sb.AppendLine("<div class='center'>Hẹn gặp lại!</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
