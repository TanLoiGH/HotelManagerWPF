using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public static class PrintHelper
{
    /// <summary>
    /// Thực hiện in hóa đơn POS (mặc định mở Preview trước)
    /// </summary>
    public static void PrintPOSInvoice(HoaDon hd, string khName, string staffName, bool showPreview = true)
    {
        FlowDocument doc = CreatePOSDocument(hd, khName, staffName);

        if (showPreview)
        {
            var preview = new PrintPreviewDialog(doc, $"HD_{hd.MaHoaDon}");
            preview.Owner = Application.Current.MainWindow;
            preview.ShowDialog();
        }
        else
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"InHoaDon_{hd.MaHoaDon}");
            }
        }
    }

    /// <summary>
    /// Hàm in thử nghiệm (Test Print) để kiểm tra kết nối và layout máy in
    /// </summary>
    public static void TestPrint()
    {
        FlowDocument testDoc = new FlowDocument
        {
            PagePadding = new Thickness(10),
            PageWidth = 300,
            ColumnWidth = 300,
            FontFamily = new FontFamily("Segoe UI")
        };

        testDoc.Blocks.Add(new Paragraph(new Run("TEST PRINT - HOTELIFY")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        testDoc.Blocks.Add(new Paragraph(new Run("Checking Printer Connection...")) { TextAlignment = TextAlignment.Center });
        testDoc.Blocks.Add(new Paragraph(new Run("ESC/POS Emulation: 80mm")) { TextAlignment = TextAlignment.Center });
        testDoc.Blocks.Add(new Paragraph(new Run(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))) { TextAlignment = TextAlignment.Center });
        testDoc.Blocks.Add(new Paragraph(new Run("------------------------------------------")) { TextAlignment = TextAlignment.Center });
        testDoc.Blocks.Add(new Paragraph(new Run("SUCCESSFUL!")) { FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });

        var preview = new PrintPreviewDialog(testDoc, "TestPrint");
        preview.Owner = Application.Current.MainWindow;
        preview.ShowDialog();
    }

    private static FlowDocument CreatePOSDocument(HoaDon hd, string khName, string staffName)
    {
        // Tạo tài liệu in khổ 80mm (xấp xỉ 300 units)
        FlowDocument doc = new FlowDocument
        {
            PagePadding = new Thickness(10),
            PageWidth = 300, // Khổ giấy máy in nhiệt 80mm
            ColumnWidth = 300,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        // Header
        Paragraph header = new Paragraph(new Run("HOTELIFY"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        };
        doc.Blocks.Add(header);
        doc.Blocks.Add(new Paragraph(new Run("123 Đường ABC, Quận XYZ, TP.HCM\nSĐT: 0123.456.789")) { TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")) { TextAlignment = TextAlignment.Center });

        // Title
        doc.Blocks.Add(new Paragraph(new Run("HÓA ĐƠN THANH TOÁN")) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run($"Số: {hd.MaHoaDon}\nNgày: {DateTime.Now:dd/MM/yyyy HH:mm}")) { TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")) { TextAlignment = TextAlignment.Center });

        // Info Section
        Paragraph info = new Paragraph();
        info.Inlines.Add(new Bold(new Run("Khách hàng: ")));
        info.Inlines.Add(new Run(khName + "\n"));
        info.Inlines.Add(new Bold(new Run("Nhân viên: ")));
        info.Inlines.Add(new Run(staffName));
        doc.Blocks.Add(info);

        // Table Detail Section
        Table table = new Table { CellSpacing = 0, Margin = new Thickness(0, 10, 0, 10) };
        table.Columns.Add(new TableColumn { Width = new GridLength(180) });
        table.Columns.Add(new TableColumn { Width = new GridLength(100) });

        TableRowGroup group = new TableRowGroup();
        TableRow headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Nội dung")))));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("T.Tiền"))) { TextAlignment = TextAlignment.Right }));
        group.Rows.Add(headerRow);

        group.Rows.Add(CreateRow("Tiền phòng", hd.TienPhong?.ToString("N0") ?? "0"));
        if (hd.TienDichVu > 0)
            group.Rows.Add(CreateRow("Tiền dịch vụ", hd.TienDichVu?.ToString("N0") ?? "0"));

        table.RowGroups.Add(group);
        doc.Blocks.Add(table);
        doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")) { TextAlignment = TextAlignment.Center });

        // Totals Section
        Table totalTable = new Table { CellSpacing = 0 };
        totalTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
        totalTable.Columns.Add(new TableColumn { Width = new GridLength(130) });
        TableRowGroup totalGroup = new TableRowGroup();

        if (hd.MaKhuyenMaiNavigation != null)
            AddTotalRow(totalGroup, "Khuyến mãi:", $"-{hd.MaKhuyenMaiNavigation.GiaTriKm:N0}");

        AddTotalRow(totalGroup, $"Thuế VAT ({hd.Vat}%):", $"+{(hd.TongThanhToan * (hd.Vat / 100)):N0}");

        decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        if (tienCoc > 0)
            AddTotalRow(totalGroup, "Đã đặt cọc:", $"-{tienCoc:N0}");

        TableRow finalRow = new TableRow();
        finalRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("THÀNH TIỀN:"))) { FontSize = 14 }));
        finalRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run($"{hd.TongThanhToan:N0} VNĐ"))) { FontSize = 14, TextAlignment = TextAlignment.Right }));
        totalGroup.Rows.Add(finalRow);

        totalTable.RowGroups.Add(totalGroup);
        doc.Blocks.Add(totalTable);

        doc.Blocks.Add(new Paragraph(new Run("------------------------------------------")) { TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run("Cảm ơn quý khách! Hẹn gặp lại!")) { TextAlignment = TextAlignment.Center, FontStyle = FontStyles.Italic, Margin = new Thickness(0, 10, 0, 0) });

        return doc;
    }

    private static TableRow CreateRow(string label, string value)
    {
        TableRow row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))));
        row.Cells.Add(new TableCell(new Paragraph(new Run(value)) { TextAlignment = TextAlignment.Right }));
        return row;
    }

    private static void AddTotalRow(TableRowGroup group, string label, string value)
    {
        group.Rows.Add(CreateRow(label, value));
    }
}
