using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

/// <summary>
/// Trả về Visible nếu string không rỗng, ngược lại Collapsed.
/// Dùng để hiển thị thông báo lỗi trong LoginWindow.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
