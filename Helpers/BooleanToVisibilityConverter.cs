using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = false;

        // Xử lý value null
        if (value == null)
        {
            boolValue = false;
        }
        // Xử lý kiểu bool
        else if (value is bool b)
        {
            boolValue = b;
        }
        // Xử lý kiểu object khác (mặc định: không null => true)
        else
        {
            boolValue = true;
        }

        bool inverse = parameter?.ToString() == "Inverse";

        if (inverse)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
