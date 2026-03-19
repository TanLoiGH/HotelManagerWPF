using QuanLyKhachSan_PhamTanLoi.Models;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class BookingItem
{
    public string MaDatPhong { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayDatText { get; set; } = "";
    public string TrangThai { get; set; } = "";
}

public partial class ChonDatPhongDialog : Window
{
    public string? SelectedMaDatPhong { get; private set; }
    public string? SelectedMaKhuyenMai { get; private set; }

    public ChonDatPhongDialog(List<DatPhong> datPhongs, List<KhuyenMai> khuyenMais)
    {
        InitializeComponent();
        DpGrid.ItemsSource = datPhongs.Select(d => new BookingItem
        {
            MaDatPhong = d.MaDatPhong,
            TenKhachHang = d.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
            NgayDatText = d.NgayDat?.ToString("dd/MM/yyyy") ?? "",
            TrangThai = d.TrangThai ?? "",
        }).ToList();

        var khuyenMaiOptions = new List<KhuyenMai>
        {
            new()
            {
                MaKhuyenMai = "",
                TenKhuyenMai = "— Không áp dụng —"
            }
        };

        khuyenMaiOptions.AddRange(khuyenMais);
        CboKhuyenMai.ItemsSource = khuyenMaiOptions;
        CboKhuyenMai.DisplayMemberPath = "TenKhuyenMai";
        CboKhuyenMai.SelectedValuePath = "MaKhuyenMai";
        CboKhuyenMai.SelectedIndex = 0;
    }

    private void BtnChon_Click(object sender, RoutedEventArgs e)
    {
        if (DpGrid.SelectedItem is not BookingItem item)
        {
            MessageBox.Show("Vui lòng chọn một đặt phòng.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        SelectedMaDatPhong = item.MaDatPhong;
        SelectedMaKhuyenMai = CboKhuyenMai.SelectedValue as string;
        DialogResult = true;
        Close();
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}


