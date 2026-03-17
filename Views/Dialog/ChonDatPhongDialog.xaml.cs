using QuanLyKhachSan_PhamTanLoi.Models;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class DatPhongItem
{
    public string MaDatPhong { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayDatText { get; set; } = "";
    public string TrangThai { get; set; } = "";
}

public partial class ChonDatPhongDialog : Window
{
    public string? SelectedMaDatPhong { get; private set; }

    public ChonDatPhongDialog(List<DatPhong> datPhongs)
    {
        InitializeComponent();
        DpGrid.ItemsSource = datPhongs.Select(d => new DatPhongItem
        {
            MaDatPhong = d.MaDatPhong,
            TenKhachHang = d.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
            NgayDatText = d.NgayDat?.ToString("dd/MM/yyyy") ?? "",
            TrangThai = d.TrangThai ?? "",
        }).ToList();
    }

    private void BtnChon_Click(object sender, RoutedEventArgs e)
    {
        if (DpGrid.SelectedItem is not DatPhongItem item)
        {
            MessageBox.Show("Vui lòng chọn một đặt phòng.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        SelectedMaDatPhong = item.MaDatPhong;
        DialogResult = true;
        Close();
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}