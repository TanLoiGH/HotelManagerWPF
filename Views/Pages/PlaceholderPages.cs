// ──────────────────────────────────────────────────────────────────────────
// Stub pages — mỗi module sẽ được phát triển đầy đủ ở bước sau.
// Chứa trong 1 file để tiện, sau đó tách ra từng file riêng.
// ──────────────────────────────────────────────────────────────────────────
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;


// ── Helper ───────────────────────────────────────────────────────────────
internal static class PlaceholderPageBuilder
{
    public static System.Windows.Controls.Grid Build(string icon, string title, string hexColor)
    {
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor)!);

        var grid = new System.Windows.Controls.Grid();
        var sp = new System.Windows.Controls.StackPanel
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        };

        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = icon,
            FontSize = 64,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 16),
        });
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = title,
            FontSize = 28,
            FontWeight = System.Windows.FontWeights.Bold,
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


