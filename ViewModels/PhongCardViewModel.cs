using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class PhongCardViewModel
{
    public string MaPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public string MaTrangThaiPhong { get; set; } = "";
    public int SoNguoiToiDa { get; set; }
    public decimal GiaPhong { get; set; }
    public string GiaPhongText => GiaPhong.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    public int Tang { get; set; }

    public int SoPhongSort
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MaPhong)) return 0;
            var digits = new string(MaPhong.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : 0;
        }
    }

    public SolidColorBrush CardBackground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(0, 184, 148)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(225, 112, 85)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(243, 156, 18)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(127, 140, 141)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(108, 92, 231)),
        _ => new SolidColorBrush(Color.FromRgb(99, 110, 114)),
    };

    public Color ShadowColor => MaTrangThaiPhong switch
    {
        "PTT01" => Color.FromRgb(0, 184, 148),
        "PTT02" => Color.FromRgb(225, 112, 85),
        "PTT03" => Color.FromRgb(243, 156, 18),
        "PTT05" => Color.FromRgb(108, 92, 231),
        _ => Color.FromRgb(0, 120, 212),
    };
}
