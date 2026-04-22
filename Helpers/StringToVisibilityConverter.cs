using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;
// Senior Fix: File-scoped namespace

/// <summary>
/// Senior Note: Class này thực chất là một UniversalVisibilityConverter.
/// Nó hỗ trợ kiểm tra Null, String, Boolean, ICollection Count, và Numbers.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 1. TRƯỜNG HỢP CÓ PARAMETER: Chỉ hiển thị khi Value bằng với Parameter
        if (parameter != null)
        {
            if (value == null) return Visibility.Collapsed;

            string paramText = parameter.ToString() ?? string.Empty;

            // Kiểm tra so sánh dạng số (Tối ưu hóa: không dùng try-catch)
            if (TryCompareAsNumber(value, paramText, out bool numberEquals))
                return numberEquals ? Visibility.Visible : Visibility.Collapsed;

            // So sánh chuỗi (không phân biệt hoa thường)
            return string.Equals(value.ToString(), paramText, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // 2. TRƯỜNG HỢP KHÔNG CÓ PARAMETER: Dùng Pattern Matching (Switch Expression)
        return value switch
        {
            null => Visibility.Collapsed,
            bool b => b ? Visibility.Visible : Visibility.Collapsed,
            string s => string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible,
            ICollection collection => collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
            _ when IsNumber(value, out decimal number) => number > 0 ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Visible // Mặc định object tồn tại thì hiển thị
        };
    }

    /// <summary>
    /// Senior Fix: Kiểm tra kiểu số bằng Type Pattern Matching cực nhanh,
    /// tuyệt đối KHÔNG sử dụng Try-Catch để ép kiểu trong WPF Converter.
    /// </summary>
    private static bool IsNumber(object value, out decimal number)
    {
        number = 0;
        switch (value)
        {
            case sbyte sb:
                number = sb;
                return true;
            case byte b:
                number = b;
                return true;
            case short s:
                number = s;
                return true;
            case ushort us:
                number = us;
                return true;
            case int i:
                number = i;
                return true;
            case uint ui:
                number = ui;
                return true;
            case long l:
                number = l;
                return true;
            case ulong ul:
                number = ul;
                return true;
            case float f:
                number = (decimal)f;
                return true;
            case double d:
                number = (decimal)d;
                return true;
            case decimal dec:
                number = dec;
                return true;
            default: return false;
        }
    }

    private static bool TryCompareAsNumber(object value, string paramText, out bool equals)
    {
        equals = false;
        if (!IsNumber(value, out decimal v)) return false;

        // Dùng InvariantCulture để parse tham số XAML vì tham số ở UI thường viết theo chuẩn quốc tế (dấu chấm thập phân)
        if (!decimal.TryParse(paramText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p)) return false;

        equals = v == p;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Trả về UnsetValue để thông báo cho WPF bỏ qua nếu vô tình gán TwoWay Binding
        return DependencyProperty.UnsetValue;
    }
}