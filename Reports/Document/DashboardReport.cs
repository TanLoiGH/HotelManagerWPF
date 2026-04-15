using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuanLyKhachSan_PhamTanLoi.Reports.ViewModels;
using QuanLyKhachSan_PhamTanLoi.Helpers;

public class DashboardReport : IDocument
{
    private readonly DashboardViewModel _viewModel;

    public DashboardReport(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Trang ");
                x.CurrentPageNumber();
            });
        });
    }

    // --- Component Header ---
    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("BÁO CÁO QUẢN TRỊ KHÁCH SẠN").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                col.Item().Text($"Ngày xuất: {TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}");
            });
        });
    }

    // --- Component Content chính ---
    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(col =>
        {
            // 1. Thẻ tóm tắt (Kinh doanh)
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => SummaryCard(c, "Doanh Thu", _viewModel.DoanhThuText, Colors.Green.Medium));
                row.ConstantItem(10);
                row.RelativeItem().Element(c => SummaryCard(c, "Chi Phí", _viewModel.TongChiPhiText, Colors.Red.Medium));
                row.ConstantItem(10);
                row.RelativeItem().Element(c => SummaryCard(c, "Lợi Nhuận", _viewModel.LoiNhuanText, Colors.Blue.Medium));
            });

            col.Item().PaddingTop(20).Text("THỐNG KÊ DỊCH VỤ SỬ DỤNG NHIỀU").SemiBold().FontSize(14);

            // 2. Bảng Top Dịch Vụ
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("STT");
                    header.Cell().Element(CellStyle).Text("Tên Dịch Vụ");
                    header.Cell().Element(CellStyle).AlignRight().Text("Số Lần Sử Dụng");
                    static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1);
                });

                int i = 1;
                foreach (var dv in _viewModel.TopDichVu)
                {
                    table.Cell().Element(ItemStyle).Text(i++.ToString());
                    table.Cell().Element(ItemStyle).Text(dv.TenDV);
                    table.Cell().Element(ItemStyle).AlignRight().Text(dv.SoLan.ToString());
                    static IContainer ItemStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                }
            });

            // 3. Trạng thái phòng (Tổng hợp)
            col.Item().PaddingTop(20).Text($"TỔNG QUAN PHÒNG (Tổng: {_viewModel.TongPhong})").SemiBold().FontSize(14);
            col.Item().PaddingTop(10).Row(row =>
            {
                foreach (var status in _viewModel.PhongStatus)
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignCenter().Text(status.TenTT).FontSize(9);
                        c.Item().AlignCenter().Text(status.SoPhong.ToString()).FontSize(12).SemiBold();
                    });
                }
            });
        });
    }

    // Helper: Tạo thẻ tóm tắt
    void SummaryCard(IContainer container, string title, string value, string color)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Item().Text(title).FontSize(10).FontColor(Colors.Grey.Medium);
            col.Item().Text(value).FontSize(14).SemiBold().FontColor(color);
        });
    }
}