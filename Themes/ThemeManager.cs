using System;
using System.Linq;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Themes
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            var dict = new ResourceDictionary();
            string source = themeName == "Dark" ? "Themes/Dark.xaml" : "Themes/Light.xaml";
            dict.Source = new Uri(source, UriKind.Relative);

            var app = Application.Current;

            // Tìm dictionary theme hiện tại (dựa vào tên file Light.xaml hoặc Dark.xaml)
            var oldDict = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                                     (d.Source.OriginalString.Contains("Light.xaml") ||
                                      d.Source.OriginalString.Contains("Dark.xaml")));

            if (oldDict != null)
                app.Resources.MergedDictionaries.Remove(oldDict);

            app.Resources.MergedDictionaries.Add(dict);
        }
    }
}