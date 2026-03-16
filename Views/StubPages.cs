// ──────────────────────────────────────────────────────────────────────────
// Stub pages — mỗi module sẽ được phát triển đầy đủ ở bước sau.
// Chứa trong 1 file để tiện, sau đó tách ra từng file riêng.
// ──────────────────────────────────────────────────────────────────────────
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;

// ── Đặt Phòng ────────────────────────────────────────────────────────────
public partial class DatPhongPage : Page
{
    public DatPhongPage() { Content = MakeStub("📅", "Đặt Phòng", "#6C5CE7"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Khách Hàng ───────────────────────────────────────────────────────────
public partial class KhachHangPage : Page
{
    public KhachHangPage() { Content = MakeStub("👥", "Khách Hàng", "#0078D4"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Hoá Đơn ─────────────────────────────────────────────────────────────
public partial class HoaDonPage : Page
{
    public HoaDonPage() { Content = MakeStub("🧾", "Hoá Đơn", "#00B894"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Thanh Toán ───────────────────────────────────────────────────────────
public partial class ThanhToanPage : Page
{
    public ThanhToanPage() { Content = MakeStub("💳", "Thanh Toán", "#E17055"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Dịch Vụ ─────────────────────────────────────────────────────────────
public partial class DichVuPage : Page
{
    public DichVuPage() { Content = MakeStub("🍽️", "Dịch Vụ", "#FDCB6E"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Nhân Viên ────────────────────────────────────────────────────────────
public partial class NhanVienPage : Page
{
    public NhanVienPage() { Content = MakeStub("🪪", "Nhân Viên", "#0F3460"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Nhà Cung Cấp ─────────────────────────────────────────────────────────
public partial class NhaCungCapPage : Page
{
    public NhaCungCapPage() { Content = MakeStub("🏭", "Nhà Cung Cấp", "#636E72"); }
    private static System.Windows.FrameworkElement MakeStub(string icon, string title, string color)
        => StubPageHelper.Build(icon, title, color);
}

// ── Helper ───────────────────────────────────────────────────────────────
internal static class StubPageHelper
{
    public static System.Windows.Controls.Grid Build(string icon, string title, string hexColor)
    {
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor)!);

        var grid = new System.Windows.Controls.Grid();
        var sp   = new System.Windows.Controls.StackPanel
        {
            VerticalAlignment   = System.Windows.VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        };

        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = icon, FontSize = 64,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 16),
        });
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = title, FontSize = 28, FontWeight = System.Windows.FontWeights.Bold,
            Foreground = brush,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 8),
        });
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Module đang được phát triển...",
            FontSize = 14,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(150, 150, 150)),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        });

        grid.Children.Add(sp);
        return grid;
    }
}
