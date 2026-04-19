using System;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

public partial class GiaHanDialog : Window
{
    // Biến này sẽ lưu trữ ngày khách chọn để ViewModel bên ngoài lấy ra dùng
    public DateTime NgayTraMoi { get; private set; }

    public GiaHanDialog(DateTime ngayTraHienTai)
    {
        InitializeComponent();

        // Cài đặt mặc định: Chọn ngày tiếp theo của ngày trả hiện tại
        DpNgayTraMoi.SelectedDate = ngayTraHienTai.AddDays(1);

        // Khóa không cho Lễ tân chọn lùi ngày (chỉ được chọn tương lai)
        DpNgayTraMoi.DisplayDateStart = ngayTraHienTai.AddDays(1);
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        if (DpNgayTraMoi.SelectedDate == null)
        {
            MessageBox.Show("Vui lòng chọn ngày trả phòng mới!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NgayTraMoi = DpNgayTraMoi.SelectedDate.Value;
        this.DialogResult = true; // Báo hiệu là đã chọn thành công và đóng Dialog
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false; // Đóng Dialog và không làm gì cả
    }
}