using System.Collections.Generic;
using System.Windows;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class ThemDichVuDialog : Window
{
    // Trả thêm MaPhong trong Tuple
    public (string MaDichVu, int SoLuong, string MaPhong)? SelectedResult { get; private set; }

    public ThemDichVuDialog(List<DichVuViewModel> danhSachDichVu, List<string> danhSachMaPhong)
    {
        InitializeComponent();

        DichVuGrid.ItemsSource = danhSachDichVu;

        // Đổ danh sách mã phòng vào ComboBox
        CboPhong.ItemsSource = danhSachMaPhong;
        if (danhSachMaPhong.Count > 0) CboPhong.SelectedIndex = 0;
    }

    private void BtnChon_Click(object sender, RoutedEventArgs e)
    {
        if (DichVuGrid.SelectedItem is not DichVuViewModel dv)
        {
            MessageBox.Show("Vui lòng chọn dịch vụ.", "Thông báo");
            return;
        }

        if (CboPhong.SelectedItem is not string maPhong)
        {
            MessageBox.Show("Vui lòng chọn phòng sử dụng dịch vụ.", "Thông báo");
            return;
        }

        if (!int.TryParse(TxtSoLuong.Text, out int soLuong) || soLuong <= 0)
        {
            MessageBox.Show("Số lượng không hợp lệ.", "Lỗi");
            return;
        }

        // Trả về đầy đủ thông tin: Dịch vụ nào, bao nhiêu cái, cho phòng nào
        SelectedResult = (dv.MaDichVu, soLuong, maPhong);
        DialogResult = true;
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}