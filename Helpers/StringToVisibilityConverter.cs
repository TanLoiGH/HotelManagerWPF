using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nếu có ConverterParameter: chỉ Visible khi value == parameter (hỗ trợ string & number/Count).
            if (parameter != null)
            {
                if (value == null) return Visibility.Collapsed;

                var paramText = parameter.ToString() ?? "";

                if (TryCompareAsNumber(value, paramText, culture, out bool numberEquals))
                    return numberEquals ? Visibility.Visible : Visibility.Collapsed;

                return string.Equals(value.ToString(), paramText, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (value == null) return Visibility.Collapsed;

            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;

            if (value is string s)
                return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;

            if (value is ICollection collection)
                return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (TryGetNumber(value, culture, out var number))
                return number > 0 ? Visibility.Visible : Visibility.Collapsed;

            // object khác: không null => Visible
            return Visibility.Visible;
        }

        private static bool TryCompareAsNumber(object value, string paramText, CultureInfo culture, out bool equals)
        {
            equals = false;
            if (!TryGetNumber(value, culture, out var v)) return false;
            if (!decimal.TryParse(paramText, NumberStyles.Any, culture, out var p)) return false;
            equals = v == p;
            return true;
        }

        private static bool TryGetNumber(object value, CultureInfo culture, out decimal number)
        {
            number = 0;
            try
            {
                if (value is IConvertible)
                {
                    number = System.Convert.ToDecimal(value, culture);
                    return true;
                }
            }
            catch
            {
                // ignore
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
