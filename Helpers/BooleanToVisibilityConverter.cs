using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;
// Senior Fix: Dùng File-scoped namespace cho gọn

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Senior Refactor: Sử dụng Switch Expression (Pattern Matching) để code ngắn gọn, rành mạch
        bool isVisible = value switch
        {
            null => false,
            bool b => b,
            _ => true
        };

        // Senior Fix: Ép kiểu an toàn bằng 'as' và so sánh không phân biệt hoa/thường. 
        // Tránh dùng .ToString() để không cấp phát string rác (Garbage Collection) liên tục trên UI Thread.
        if (string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Senior Fix: Cài đặt ConvertBack chuẩn mực đề phòng trường hợp TwoWay Binding
        if (value is Visibility visibility)
        {
            bool isVisible = visibility == Visibility.Visible;
            bool isInverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);

            return isInverse ? !isVisible : isVisible;
        }

        // Nếu không convert được, báo cho WPF biết để bỏ qua, không ném Exception làm sập App
        return DependencyProperty.UnsetValue;
    }
}