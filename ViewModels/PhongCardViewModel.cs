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

    public SolidColorBrush CardBackground => new SolidColorBrush(Color.FromRgb(255, 255, 255));

    public SolidColorBrush BadgeBackground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(209, 250, 229)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(255, 228, 230)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(241, 245, 249)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(224, 231, 255)),
        _ => new SolidColorBrush(Color.FromRgb(241, 245, 249)),
    };

    public SolidColorBrush BadgeForeground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(225, 29, 72)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(100, 116, 139)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(99, 102, 241)),
        _ => new SolidColorBrush(Color.FromRgb(100, 116, 139)),
    };

    public Color ShadowColor => MaTrangThaiPhong switch
    {
        "PTT01" => Color.FromRgb(16, 185, 129),
        "PTT02" => Color.FromRgb(225, 29, 72),
        "PTT03" => Color.FromRgb(245, 158, 11),
        "PTT05" => Color.FromRgb(99, 102, 241),
        _ => Color.FromRgb(37, 99, 235),
    };

    public string StatusIcon => MaTrangThaiPhong switch
    {
        "PTT01" => "✨",
        "PTT02" => "👤",
        "PTT03" => "🧹",
        "PTT04" => "🛠️",
        "PTT05" => "📅",
        _ => "ℹ️"
    };

    public string? GuestName { get; set; }
    public string InfoText => !string.IsNullOrEmpty(GuestName) ? GuestName : "Chưa có thông tin";
    public string CapacityText1 => $"Phòng {SoNguoiToiDa} người";
    public string CapacityText2 => $"{TenLoaiPhong}";


}
