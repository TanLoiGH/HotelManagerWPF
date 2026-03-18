using System.Windows;
using System.Windows.Media;

namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public static class ThemeProperties
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(default(CornerRadius)));

        public static CornerRadius GetCornerRadius(DependencyObject obj) =>
            (CornerRadius)obj.GetValue(CornerRadiusProperty);

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value) =>
            obj.SetValue(CornerRadiusProperty, value);

        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "HoverBackground",
                typeof(Brush),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null));

        public static Brush GetHoverBackground(DependencyObject obj) =>
            (Brush)obj.GetValue(HoverBackgroundProperty);

        public static void SetHoverBackground(DependencyObject obj, Brush value) =>
            obj.SetValue(HoverBackgroundProperty, value);

        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "PressedBackground",
                typeof(Brush),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null));

        public static Brush GetPressedBackground(DependencyObject obj) =>
            (Brush)obj.GetValue(PressedBackgroundProperty);

        public static void SetPressedBackground(DependencyObject obj, Brush value) =>
            obj.SetValue(PressedBackgroundProperty, value);

        public static readonly DependencyProperty HoverBorderBrushProperty =
            DependencyProperty.RegisterAttached(
                "HoverBorderBrush",
                typeof(Brush),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null));

        public static Brush GetHoverBorderBrush(DependencyObject obj) =>
            (Brush)obj.GetValue(HoverBorderBrushProperty);

        public static void SetHoverBorderBrush(DependencyObject obj, Brush value) =>
            obj.SetValue(HoverBorderBrushProperty, value);

        public static readonly DependencyProperty PressedBorderBrushProperty =
            DependencyProperty.RegisterAttached(
                "PressedBorderBrush",
                typeof(Brush),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(null));

        public static Brush GetPressedBorderBrush(DependencyObject obj) =>
            (Brush)obj.GetValue(PressedBorderBrushProperty);

        public static void SetPressedBorderBrush(DependencyObject obj, Brush value) =>
            obj.SetValue(PressedBorderBrushProperty, value);

        public static readonly DependencyProperty DisabledOpacityProperty =
            DependencyProperty.RegisterAttached(
                "DisabledOpacity",
                typeof(double),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(0.55d));

        public static double GetDisabledOpacity(DependencyObject obj) =>
            (double)obj.GetValue(DisabledOpacityProperty);

        public static void SetDisabledOpacity(DependencyObject obj, double value) =>
            obj.SetValue(DisabledOpacityProperty, value);

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.RegisterAttached(
                "ShadowColor",
                typeof(Color),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(Colors.Black));

        public static Color GetShadowColor(DependencyObject obj) =>
            (Color)obj.GetValue(ShadowColorProperty);

        public static void SetShadowColor(DependencyObject obj, Color value) =>
            obj.SetValue(ShadowColorProperty, value);

        public static readonly DependencyProperty ShadowBlurRadiusProperty =
            DependencyProperty.RegisterAttached(
                "ShadowBlurRadius",
                typeof(double),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(12d));

        public static double GetShadowBlurRadius(DependencyObject obj) =>
            (double)obj.GetValue(ShadowBlurRadiusProperty);

        public static void SetShadowBlurRadius(DependencyObject obj, double value) =>
            obj.SetValue(ShadowBlurRadiusProperty, value);

        public static readonly DependencyProperty ShadowDepthProperty =
            DependencyProperty.RegisterAttached(
                "ShadowDepth",
                typeof(double),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(0d));

        public static double GetShadowDepth(DependencyObject obj) =>
            (double)obj.GetValue(ShadowDepthProperty);

        public static void SetShadowDepth(DependencyObject obj, double value) =>
            obj.SetValue(ShadowDepthProperty, value);

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.RegisterAttached(
                "ShadowOpacity",
                typeof(double),
                typeof(ThemeProperties),
                new FrameworkPropertyMetadata(0.07d));

        public static double GetShadowOpacity(DependencyObject obj) =>
            (double)obj.GetValue(ShadowOpacityProperty);

        public static void SetShadowOpacity(DependencyObject obj, double value) =>
            obj.SetValue(ShadowOpacityProperty, value);
    }
}
