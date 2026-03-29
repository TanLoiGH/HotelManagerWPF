using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using System.IO;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

public partial class PrintPreviewDialog : Window
{
    private readonly FlowDocument _doc;
    private readonly string _jobName;

    public PrintPreviewDialog(FlowDocument doc, string jobName = "InHoaDon")
    {
        _doc = doc;
        _jobName = jobName;
        InitializeComponent();

        // Hiển thị tài liệu bằng FlowDocumentScrollViewer
        DocViewer.Document = _doc;

        CheckPrinterStatus();
    }

    private void CheckPrinterStatus()
    {
        try
        {
            // Giả lập kiểm tra driver máy in
            var printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters;
            if (printers.Count == 0)
            {
                TxtPrinterStatus.Text = "⚠ Không tìm thấy driver máy in nào trong hệ thống.";
                TxtPrinterStatus.Foreground = System.Windows.Media.Brushes.Red;
                BtnPrint.IsEnabled = false;
            }
            else
            {
                TxtPrinterStatus.Text = $"✅ Sẵn sàng in ({printers.Count} thiết bị tìm thấy)";
                TxtPrinterStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
        }
        catch
        {
            TxtPrinterStatus.Text = "⚠ Lỗi kết nối Driver máy in.";
        }
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        var pd = new PrintDialog();
        // pd.PrintQueue = new PrintServer().GetPrintQueue //Kết nối với máy in thật thì uncomemt dòng này("Tên_Máy_In_Của_Bạn");
        if (pd.ShowDialog() == true)
        {
            try
            {
                IDocumentPaginatorSource idp = _doc;
                pd.PrintDocument(idp.DocumentPaginator, _jobName);
                ConfirmHelper.ShowInfo("Đã gửi lệnh in thành công!");
                Close();
            }
            catch (Exception ex)
            {
                ConfirmHelper.ShowError($"Lỗi khi in: {ex.Message}");
            }
        }
    }

    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog sfd = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = _jobName + ".pdf"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                // Trong thực tế, có thể dùng các thư viện như iTextSharp hoặc PdfSharp
                // Ở đây ta dùng Microsoft Print to PDF làm giả lập xuất file
                PrintDialog pd = new PrintDialog();
                // Tự động tìm máy in PDF nếu có

                ConfirmHelper.ShowInfo("Vui lòng chọn 'Microsoft Print to PDF' trong hộp thoại tiếp theo để xuất file.");
                if (pd.ShowDialog() == true)
                {
                    pd.PrintDocument(((_doc as IDocumentPaginatorSource).DocumentPaginator), _jobName);
                }
            }
            catch (Exception ex)
            {
                ConfirmHelper.ShowError($"Lỗi xuất PDF: {ex.Message}");
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
