// ViewModels/PhongViewModel.cs
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class PhongViewModel
{
    public string MaPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string MaTrangThai { get; set; } = "";
    public int Tang { get; set; }

    // Tên trạng thái hiển thị
    public string TenTrangThai => MaTrangThai switch
    {
        "PTT01" => "Phòng trống",
        "PTT02" => "Đang có khách",
        "PTT03" => "Đang dọn dẹp",
        "PTT04" => "Bảo trì",
        "PTT05" => "Đã đặt trước",
        _ => "Không rõ"
    };

    // Màu badge trạng thái
    public SolidColorBrush MauTrangThai => MaTrangThai switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(212, 237, 218)), // xanh nhạt
        "PTT02" => new SolidColorBrush(Color.FromRgb(255, 200, 180)), // cam nhạt
        "PTT03" => new SolidColorBrush(Color.FromRgb(255, 243, 205)), // vàng nhạt
        "PTT04" => new SolidColorBrush(Color.FromRgb(220, 220, 220)), // xám
        "PTT05" => new SolidColorBrush(Color.FromRgb(220, 210, 255)), // tím nhạt
        _ => new SolidColorBrush(Color.FromRgb(200, 200, 200))
    };

    public SolidColorBrush MauChữTrangThai => MaTrangThai switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(25, 135, 84)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(200, 80, 50)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(160, 120, 0)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(100, 100, 100)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(90, 50, 180)),
        _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))
    };
}