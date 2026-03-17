// ===========================================================================
// ThemDichVuDialog.xaml.cs
// Chọn DICH_VU + số lượng → trả về (MaDichVu, SoLuong)
// ===========================================================================
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;

public partial class ThemDichVuDialog : Window
{
    public (string MaDichVu, int SoLuong)? SelectedResult { get; private set; }

    public ThemDichVuDialog(List<DichVuViewModel> dichVus)
    {
        InitializeComponent();
        DichVuGrid.ItemsSource = dichVus;
    }

    private void BtnChon_Click(object sender, RoutedEventArgs e)
    {
        if (DichVuGrid.SelectedItem is not DichVuViewModel dv) return;
        if (!int.TryParse(TxtSoLuong.Text, out int sl) || sl <= 0)
        {
            MessageBox.Show("Số lượng không hợp lệ.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SelectedResult = (dv.MaDichVu, sl);
        DialogResult = true;
        Close();
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}