using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Commands;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public sealed class AboutViewModel : BaseViewModel
{
    public ICommand MoHuongDanCommand { get; }

    public AboutViewModel()
    {
        MoHuongDanCommand = new RelayCommand(_ => MoHuongDanSuDung());
    }

    private void MoHuongDanSuDung()
    {
        try
        {
            // Tìm file Hướng dẫn sử dụng (Tên file có thể sửa lại theo thực tế: .pdf, .docx)
            // Khuyến nghị: Copy file HDSD vào thư mục "Docs" bên trong thư mục Build (Debug/Release)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDir, "Docs", "QLKS_PhamTanLoi_BaoCao.docx");

            if (File.Exists(filePath))
            {
                // Dùng Process.Start để nhờ Windows mở file bằng ứng dụng mặc định (PDF Reader/Word)
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show(
                    $"Không tìm thấy file Hướng dẫn tại:\n{filePath}\n\nVui lòng kiểm tra lại thư mục Docs.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi mở Hướng dẫn sử dụng:\n{ex.Message}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}