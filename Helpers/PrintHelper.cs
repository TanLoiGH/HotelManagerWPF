using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Printing; 

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class PrintHelper
{
    private static readonly SolidColorBrush NavyBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#002D62"));
    private static readonly SolidColorBrush LightGrayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));

    public static bool PreviewAndPrint(FlowDocument document, string documentName, Window? owner)
    {
        PrintDialog printDlg = new PrintDialog();
        // Hiển thị FlowDocument lên DocumentViewer trên cửa sổ PrintPreviewDialog của bạn
        // Hàm này sẽ do PrintPreviewDialog gọi. Ở đây chúng ta trả về document để cửa sổ Preview xử lý.
        // Tuy nhiên, theo thiết kế interface hiện tại của bạn, tôi sẽ giả lập lệnh in trực tiếp ở đây 
        // hoặc bạn có thể truyền document này ra ngoài.

        // Cách tốt nhất: Mở cửa sổ Preview của bạn.
        // Truyền thẳng document và tên vào trong ngoặc tròn
        var previewWindow = new Views.Dialogs.PrintPreviewDialog(document, documentName);
        previewWindow.Owner = owner;
        previewWindow.ShowDialog();

        return true; // Trả về true nếu người dùng đã in/xem xong
    }

    public static FlowDocument BuildPOSInvoiceDocument(HoaDon hd, string tenKhachHang, string tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon kieuIn = HoaDonChiTietViewModel.KieuInHoaDon.TongHop)
    {
        FlowDocument doc = new FlowDocument
        {
            PagePadding = new Thickness(50),
            FontFamily = new FontFamily("Arial"),
            ColumnWidth = 800, // Khổ A4
            Background = Brushes.White
        };

        // 1. HEADER KHÁCH SẠN
        doc.Blocks.Add(CreateHeader());

        // 2. THÔNG TIN HÓA ĐƠN VÀ KHÁCH HÀNG
        doc.Blocks.Add(CreateMetaInfo(hd, tenKhachHang, tenNhanVien));

        // 3. XỬ LÝ SỐ LIỆU THEO KIỂU IN
        decimal tienPhong = 0;
        decimal tienDichVu = 0;
        decimal phanTramVat = hd.Vat ?? 0;
        decimal tienCoc = 0;

        if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong)
        {
            tienPhong = hd.TienPhong ?? 0;
            tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        }

        if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu)
        {
            tienDichVu = hd.TienDichVu ?? 0;
        }

        decimal tienVat = tienPhong * (phanTramVat / 100m);
        decimal tongThanhToan = (tienPhong + tienDichVu + tienVat) - tienCoc;

        // 4. BẢNG CHI TIẾT
        doc.Blocks.Add(CreateDataTable(hd, kieuIn));

        // 5. TỔNG KẾT
        doc.Blocks.Add(CreateTotals(tienPhong + tienDichVu, phanTramVat, tienVat, tienCoc, tongThanhToan, kieuIn));

        // 6. FOOTER (Chữ ký & Đọc số)
        doc.Blocks.Add(CreateFooter(tongThanhToan, hd, tenNhanVien));

        return doc;
    }

    public static FlowDocument BuildTamTinhDocument(HoaDon hd, string tenKhachHang, string tenNhanVien)
    {
        // Tạm tính có thể tái sử dụng hàm trên, chỉ cần thay đổi Tiêu đề nếu muốn.
        return BuildPOSInvoiceDocument(hd, tenKhachHang, tenNhanVien, HoaDonChiTietViewModel.KieuInHoaDon.TongHop);
    }

    #region CÁC HÀM VẼ THÀNH PHẦN (VẼ UI BẰNG CODE)
    private static Block CreateHeader()
    {
        Paragraph p = new Paragraph() { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) };
        p.Inlines.Add(new Bold(new Run("HOTEL NAME\n")) { FontSize = 26, Foreground = NavyBrush });
        p.Inlines.Add(new Run("123 Đường ABC, Quận XXZ, TP. Hà Nội\n") { FontSize = 13 });
        p.Inlines.Add(new Run("Tel: (024) 1234 5678  |  Email: info@hotelname.com\n") { FontSize = 13 });
        p.Inlines.Add(new Run("Mã số thuế (Tax Code): 0101234567\n") { FontSize = 13 });

        p.Inlines.Add(new Bold(new Run("\nHÓA ĐƠN THANH TOÁN\n")) { FontSize = 22, Foreground = NavyBrush });
        p.Inlines.Add(new Run("HOTEL INVOICE") { FontSize = 14, Foreground = Brushes.Gray });

        return p;
    }

    private static Block CreateMetaInfo(HoaDon hd, string tenKhachHang, string tenNhanVien)
    {
        Table table = new Table() { Margin = new Thickness(0, 0, 0, 15) };
        table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });

        TableRowGroup group = new TableRowGroup();
        string ngayVao = hd.MaDatPhongNavigation?.NgayNhanDuKien?.ToString("dd/MM/yyyy") ?? "";
        string ngayRa = hd.MaDatPhongNavigation?.NgayTraDuKien?.ToString("dd/MM/yyyy") ?? "";

        group.Rows.Add(CreateTwoColRow($"Số HD (Invoice No.): {hd.MaHoaDon}", $"Khách hàng (Guest): {tenKhachHang}"));
        group.Rows.Add(CreateTwoColRow($"Ngày lập (Date): {DateTime.Now:dd/MM/yyyy}", $"Phòng (Room): {hd.MaDatPhong}"));
        group.Rows.Add(CreateTwoColRow($"Thu ngân (Cashier): {tenNhanVien}", $"Lưu trú (Stay): {ngayVao} - {ngayRa}"));

        table.RowGroups.Add(group);
        return table;
    }

    private static TableRow CreateTwoColRow(string col1, string col2)
    {
        TableRow row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(col1)) { Margin = new Thickness(2) }));
        row.Cells.Add(new TableCell(new Paragraph(new Run(col2)) { Margin = new Thickness(2) }));
        return row;
    }

    private static Block CreateDataTable(HoaDon hd, HoaDonChiTietViewModel.KieuInHoaDon kieuIn)
    {
        Table table = new Table() { CellSpacing = 0, BorderBrush = NavyBrush, BorderThickness = new Thickness(1) };
        table.Columns.Add(new TableColumn() { Width = new GridLength(40) }); // STT
        table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); // Tên
        table.Columns.Add(new TableColumn() { Width = new GridLength(80) }); // SL
        table.Columns.Add(new TableColumn() { Width = new GridLength(100) }); // Đơn giá
        table.Columns.Add(new TableColumn() { Width = new GridLength(120) }); // Thành tiền

        TableRowGroup group = new TableRowGroup();

        // Header
        TableRow headerRow = new TableRow() { Background = NavyBrush, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("STT")) { TextAlignment = TextAlignment.Center, Padding = new Thickness(5) }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Diễn Giải")) { Padding = new Thickness(5) }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("SL")) { TextAlignment = TextAlignment.Center, Padding = new Thickness(5) }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Đơn Giá")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(5) }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Thành Tiền")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(5) }));
        group.Rows.Add(headerRow);

        int stt = 1;

        // Thêm dòng Tiền phòng
        if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiTienPhong)
        {
            foreach (var p in hd.HoaDonChiTiets)
            {
                decimal donGia = p.DatPhongChiTiet?.DonGia ?? 0;
                int soDem = p.SoDem;
                decimal thanhTien = donGia * soDem;
                group.Rows.Add(CreateDataRow(stt++, $"Phòng {p.MaPhong}", soDem.ToString(), donGia, thanhTien));
            }
        }

        // Thêm dòng Dịch vụ
        if (kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.TongHop || kieuIn == HoaDonChiTietViewModel.KieuInHoaDon.ChiDichVu)
        {
            foreach (var d in hd.DichVuChiTiets)
            {
                decimal donGia = d.DonGia;
                int soLuong = d.SoLuong ;
                decimal thanhTien = donGia * soLuong;
                string tenDv = d.MaDichVuNavigation?.TenDichVu ?? "Dịch vụ";
                group.Rows.Add(CreateDataRow(stt++, tenDv, soLuong.ToString(), donGia, thanhTien));
            }
        }

        table.RowGroups.Add(group);
        return table;
    }

    private static TableRow CreateDataRow(int stt, string name, string qty, decimal price, decimal total)
    {
        TableRow row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(stt.ToString())) { TextAlignment = TextAlignment.Center, Padding = new Thickness(5) }) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
        row.Cells.Add(new TableCell(new Paragraph(new Run(name)) { Padding = new Thickness(5) }) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
        row.Cells.Add(new TableCell(new Paragraph(new Run(qty)) { TextAlignment = TextAlignment.Center, Padding = new Thickness(5) }) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
        row.Cells.Add(new TableCell(new Paragraph(new Run($"{price:N0}")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(5) }) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
        row.Cells.Add(new TableCell(new Paragraph(new Run($"{total:N0}")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(5) }) { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) });
        return row;
    }

    private static Block CreateTotals(decimal subTotal, decimal vatPercent, decimal vatAmount, decimal deposit, decimal grandTotal, HoaDonChiTietViewModel.KieuInHoaDon kieuIn)
    {
        Table table = new Table() { Margin = new Thickness(0, 10, 0, 20) };
        table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn() { Width = new GridLength(150) });

        TableRowGroup group = new TableRowGroup();

        group.Rows.Add(CreateTotalRow("Cộng tiền phòng & dịch vụ / Subtotal:", subTotal));

        if (vatAmount > 0)
            group.Rows.Add(CreateTotalRow($"Thuế GTGT (VAT) {vatPercent}%:", vatAmount));

        if (deposit > 0)
            group.Rows.Add(CreateTotalRow("Đã thanh toán cọc / Deposit:", -deposit, Brushes.Red));

        // Grand Total Row
        TableRow finalRow = new TableRow() { Background = NavyBrush, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
        finalRow.Cells.Add(new TableCell(new Paragraph(new Run("Tổng Cộng Thanh Toán / Grand Total:")) { Padding = new Thickness(10) }));
        finalRow.Cells.Add(new TableCell(new Paragraph(new Run($"{grandTotal:N0} VNĐ")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(10) }));
        group.Rows.Add(finalRow);

        table.RowGroups.Add(group);
        return table;
    }

    private static TableRow CreateTotalRow(string label, decimal amount, Brush? color = null)
    {
        TableRow row = new TableRow();
        color ??= Brushes.Black;
        row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { Padding = new Thickness(5, 2, 5, 2), Foreground = color }));
        row.Cells.Add(new TableCell(new Paragraph(new Run($"{amount:N0} VNĐ")) { TextAlignment = TextAlignment.Right, Padding = new Thickness(5, 2, 5, 2), Foreground = color }));
        return row;
    }

    private static Block CreateFooter(decimal tongTien, HoaDon hd, string tenNhanVien)
    {
        Paragraph p = new Paragraph();

        string bangChu = NumberToText((long)tongTien);
        p.Inlines.Add(new Bold(new Run($"Số tiền bằng chữ: ")) { FontStyle = FontStyles.Italic });
        p.Inlines.Add(new Run($"{bangChu} đồng chẵn.\n\n") { FontStyle = FontStyles.Italic });

        // Chữ ký
        Table sigTable = new Table();
        sigTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
        sigTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
        TableRowGroup grp = new TableRowGroup();
        TableRow row = new TableRow();

        // Khách hàng ký
        row.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Khách hàng (Guest)")) { FontSize = 14 }) { TextAlignment = TextAlignment.Center }));

        // Thu ngân ký
        Paragraph pThuNgan = new Paragraph() { TextAlignment = TextAlignment.Center };
        pThuNgan.Inlines.Add(new Bold(new Run("Thu ngân (Cashier)\n")) { FontSize = 14 });
        pThuNgan.Inlines.Add(new Run("\n\n\n\n")); // Khoảng trống để ký
        pThuNgan.Inlines.Add(new Run(tenNhanVien) { FontStyle = FontStyles.Italic, Foreground = NavyBrush, FontSize = 14 });
        row.Cells.Add(new TableCell(pThuNgan));

        grp.Rows.Add(row);
        sigTable.RowGroups.Add(grp);

        return sigTable;
    }
    #endregion

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

    #region TEST MÁY IN
    public static void TestPrint()
    {
        try
        {
            // 1. Kết nối với trình quản lý in ấn của Windows
            var printServer = new LocalPrintServer();
            var printQueues = printServer.GetPrintQueues();

            // 2. Kiểm tra nếu không có máy in nào
            if (printQueues == null || !printQueues.Any())
            {
                MessageBox.Show("CẢNH BÁO: Không tìm thấy máy in nào được cài đặt trên máy tính này!",
                                "Kiểm tra máy in", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Lấy thông tin máy in mặc định
            string defaultPrinter = printServer.DefaultPrintQueue.FullName;
            int totalPrinters = printQueues.Count();

            var message = $"Hệ thống tìm thấy {totalPrinters} máy in.\n" +
                          $"Máy in mặc định: {defaultPrinter}\n\n" +
                          "Bạn có muốn in thử một trang kiểm tra ngay bây giờ không?";

            var result = MessageBox.Show(message, "Kết nối thành công",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);

            // 4. Nếu nhấn Yes, thực hiện lệnh in thử một đoạn văn bản ngắn
            if (result == MessageBoxResult.Yes)
            {
                FlowDocument testDoc = new FlowDocument();
                testDoc.PagePadding = new Thickness(50);

                var p = new Paragraph();
                p.Inlines.Add(new Bold(new Run("TRANG IN THỬ HỆ THỐNG QUẢN LÝ KHÁCH SẠN\n")));
                p.Inlines.Add(new Run($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n"));
                p.Inlines.Add(new Run("Trạng thái thiết bị: Đang hoạt động tốt.\n"));
                p.Inlines.Add(new Run("------------------------------------------"));

                testDoc.Blocks.Add(p);

                PrintDialog pd = new PrintDialog();
                // Lệnh in này sẽ gửi trực tiếp đến máy in mặc định mà không hiện hộp thoại chọn máy in nữa
                pd.PrintDocument(((IDocumentPaginatorSource)testDoc).DocumentPaginator, "TestPrintJob");

                MessageBox.Show("Đã gửi lệnh in thử. Vui lòng kiểm tra máy in của bạn.", "Thông báo");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"LỖI MÁY IN: {ex.Message}", "Lỗi hệ thống",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

}