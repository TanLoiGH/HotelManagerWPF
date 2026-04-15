using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.Diagnostics;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class QuestPdfHelper
{
    public static void XuatVaMoHoaDonPdf(HoaDon hd, string tenKhachHang, string tenNhanVien, string duongDanLuu,
        HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop, bool laTamTinh = false)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(x => ComposeHeader(x, laTamTinh, kieuIn));
                page.Content().Element(x => ComposeContent(x, hd, tenKhachHang, tenNhanVien, kieuIn));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        document.GeneratePdf(duongDanLuu);
        MởFilePdf(duongDanLuu);
    }

    private static void ComposeHeader(IContainer container, bool laTamTinh, HoaDonChiTietViewModel.KieuInHoaDon kieuIn)
    {
        string title = laTamTinh ? "HÓA ĐƠN TẠM TÍNH" : "HÓA ĐƠN THANH TOÁN";
        string subTitle = laTamTinh ? "PROFORMA INVOICE" : "HOTEL INVOICE";

        if (!laTamTinh)
        {
            title = kieuIn switch
            {
                HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong => "HÓA ĐƠN TIỀN PHÒNG",
                HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu => "HÓA ĐƠN DỊCH VỤ DÙNG KÈM",
                _ => "HÓA ĐƠN THANH TOÁN"
            };
        }

        container.Column(col =>
        {
            col.Item().AlignCenter().Text("HOTEL NAME").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
            col.Item().AlignCenter().Text("123 Đường ABC, Quận XXZ, TP. Hà Nội");
            col.Item().AlignCenter().Text("Tel: (024) 1234 5678  |  Email: info@hotelname.com");
            col.Item().PaddingTop(10).AlignCenter().Text(title).FontSize(20).Bold();
            col.Item().AlignCenter().Text(subTitle).FontSize(12).FontColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeContent(IContainer container, HoaDon hd, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn)
    {
        var showRentalDate = kieuIn != HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu;
       
        var ngayDen = hd.MaDatPhongNavigation?.NgayNhanDuKien?.ToString("dd/MM/yyyy") ?? "...";
        var ngayDi = hd.MaDatPhongNavigation?.NgayTraDuKien?.ToString("dd/MM/yyyy") ?? "...";

        container.PaddingVertical(1, Unit.Centimetre).Column(col =>
        {
            // --- THÔNG TIN CHUNG ---
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Số HD/ Invoke NO: {hd.MaHoaDon}");
                    c.Item().Text($"Ngày lập: {TimeHelper.GetVietnamTime():dd/MM/yyyy}");
                    c.Item().Text($"Thu ngân: {tenNhanVien}").Bold();
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Khách hàng: {tenKhachHang}").Bold();
                    if (showRentalDate) // Chỉ hiện ngày thuê khi in tiền phòng
                    {
                        c.Item().Text($"Phòng: {hd.MaDatPhong}");
                        c.Item().Text($"Thời gian: {ngayDen} - {ngayDi}");
                    }
                });
            });

            col.Item().PaddingVertical(15);

            // --- BẢNG CHI TIẾT ---
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(120);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("STT");
                    header.Cell().Element(CellStyle).Text("Diễn Giải");
                    header.Cell().Element(CellStyle).AlignCenter().Text("SL");
                    header.Cell().Element(CellStyle).AlignRight().Text("Đơn Giá");
                    header.Cell().Element(CellStyle).AlignRight().Text("Thành Tiền");

                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                int stt = 1;

                // Tiền Phòng
                if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong)
                {
                    foreach (var p in hd.HoaDonChiTiets)
                    {
                        var donGia = p.DatPhongChiTiet?.DonGia ?? 0;
                        var soDem = p.SoDem;
                        table.Cell().Element(BlockStyle).Text(stt++.ToString());
                        table.Cell().Element(BlockStyle).Text($"Phòng {p.MaPhong}");
                        table.Cell().Element(BlockStyle).AlignCenter().Text(soDem.ToString());
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{donGia:N0}");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{donGia * soDem:N0}");
                    }
                }

                // Dịch vụ
                if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu)
                {
                    foreach (var dv in hd.DichVuChiTiets)
                    {
                        var donGia = dv.DonGia;
                        var soLuong = dv.SoLuong;
                        var tenDv = dv.MaDichVuNavigation?.TenDichVu ?? "Dịch vụ";

                        table.Cell().Element(BlockStyle).Text(stt++.ToString());
                        table.Cell().Element(BlockStyle).Text(tenDv);
                        table.Cell().Element(BlockStyle).AlignCenter().Text(soLuong.ToString());
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{donGia:N0}");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{donGia * soLuong:N0}");
                    }
                }

                static IContainer BlockStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
            });

            // --- TỔNG TIỀN & FOOTER ---
            decimal tienPhong = (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong) ? (hd.TienPhong ?? 0) : 0;
            decimal tienDichVu = (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu) ? (hd.TienDichVu ?? 0) : 0;
            decimal phanTramVat = hd.Vat ?? 0;
            decimal subTotal = tienPhong + tienDichVu;
            decimal vatAmount = subTotal * (phanTramVat / 100m);

            decimal tienCoc = (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong) ? (hd.MaDatPhongNavigation?.TienCoc ?? 0) : 0;
            decimal tongThanhToan = hd.TongThanhToan ?? (subTotal + vatAmount - tienCoc);

            col.Item().PaddingTop(15).Column(c =>
            {
                c.Item().AlignRight().Text($"Tổng tiền phòng & dịch vụ: {subTotal:N0} VNĐ");

                if (vatAmount > 0)
                    c.Item().AlignRight().Text($"VAT {phanTramVat}%: {vatAmount:N0} VNĐ");

                if (tienCoc > 0)
                    c.Item().AlignRight().Text($"Đã thanh toán cọc: -{tienCoc:N0} VNĐ").FontColor(Colors.Red.Medium);

                c.Item().PaddingTop(5).AlignRight().Text(text =>
                {
                    text.Span("TỔNG THANH TOÁN: ").SemiBold();
                    text.Span($"{tongThanhToan:N0} VNĐ").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                });
            });

            // Chữ ký & Đọc số
            col.Item().PaddingTop(30).Column(c =>
            {
                c.Item().Text($"Số tiền bằng chữ: {NumberToText((long)tongThanhToan)} đồng chẵn.").Italic();

                c.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Khách hàng (Guest)").SemiBold();
                    row.RelativeItem().AlignCenter().Column(sig =>
                    {
                        sig.Item().AlignCenter().Text("Thu ngân (Cashier)").SemiBold();
                        sig.Item().PaddingTop(60).AlignCenter().Text(tenNhanVien).Italic().FontColor(Colors.Blue.Darken2);
                    });
                });
            });
        });
    }

    private static void MởFilePdf(string filePath)
    {
        try
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true };
            p.Start();
        }
        catch { }
    }

    #region HÀM ĐỌC SỐ THÀNH CHỮ TIẾNG VIỆT
    private static string NumberToText(long inputNumber)
    {
        string[] unitNumbers = new string[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        string[] placeValues = new string[] { "", "nghìn", "triệu", "tỷ" };
        bool isNegative = false;
        if (inputNumber < 0) { isNegative = true; inputNumber = -inputNumber; }

        string sNumber = inputNumber.ToString("#");
        long number = Convert.ToInt64(sNumber);
        if (number == 0) return "Không";

        string result = "", tmp = "";
        int[] position = new int[6];
        int digits = sNumber.Length;
        int groupCount = (int)Math.Ceiling((double)digits / 3);

        for (int i = 0; i < groupCount; i++)
        {
            int p = (digits - (i * 3)) - 3;
            if (p < 0) { position[i] = Convert.ToInt32(sNumber.Substring(0, 3 + p)); }
            else { position[i] = Convert.ToInt32(sNumber.Substring(p, 3)); }
        }

        for (int i = groupCount - 1; i >= 0; i--)
        {
            tmp = ReadGroup(position[i]);
            if (tmp != "") { result += tmp + " " + placeValues[i] + " "; }
        }

        result = result.Trim();
        if (isNegative) result = "Âm " + result;
        return result.Substring(0, 1).ToUpper() + result.Substring(1);

        string ReadGroup(int groupNumber)
        {
            string res = "";
            int hundred = groupNumber / 100;
            int ten = groupNumber % 100 / 10;
            int unit = groupNumber % 10;

            if (hundred == 0 && ten == 0 && unit == 0) return "";
            if (hundred != 0)
            {
                res += unitNumbers[hundred] + " trăm ";
                if (ten == 0 && unit != 0) res += "lẻ ";
            }
            if (ten != 0 && ten != 1) { res += unitNumbers[ten] + " mươi "; if (ten == 0 && unit != 0) res += "lẻ "; }
            if (ten == 1) res += "mười ";

            switch (unit)
            {
                case 1: if (ten != 0 && ten != 1) { res += "mốt "; } else { res += unitNumbers[unit] + " "; } break;
                case 5: if (ten == 0) { res += unitNumbers[unit] + " "; } else { res += "lăm "; } break;
                default: if (unit != 0) { res += unitNumbers[unit] + " "; } break;
            }
            return res.Trim();
        }
    }
    #endregion
}