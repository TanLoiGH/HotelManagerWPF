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

        DocViewer.Document = _doc;

        CheckPrinterStatus();
    }

    private void CheckPrinterStatus()
    {
        try
        {
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
        if (pd.ShowDialog() == true)
        {
            try
            {
                IDocumentPaginatorSource idp = _doc;
                pd.PrintDocument(idp.DocumentPaginator, _jobName);
                ConfirmHelper.ShowInfo("Đã gửi lệnh in thành công!");
                DialogResult = true;
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
                PrintDialog pd = new PrintDialog();

                ConfirmHelper.ShowInfo("Vui lòng chọn 'Microsoft Print to PDF' trong hộp thoại tiếp theo để xuất file.");
                if (pd.ShowDialog() == true)
                {
                    if (_doc is IDocumentPaginatorSource source)
                    {
                        pd.PrintDocument(source.DocumentPaginator, _jobName);
                    }
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
