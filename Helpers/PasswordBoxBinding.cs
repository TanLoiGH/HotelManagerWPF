using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

/// <summary>
/// Cho phép bind PasswordBox.Password theo MVVM (WPF không hỗ trợ binding trực tiếp).
/// Usage:
/// PasswordBox helpers:PasswordBoxBinding.BoundPassword="{Binding MatKhauCu, Mode=TwoWay}"
/// </summary>
public static class PasswordBoxBinding
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxBinding),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxBinding),
            new PropertyMetadata(false, OnBindPasswordChanged));

    // Tránh vòng lặp set Password -> PasswordChanged -> set BoundPassword
    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBinding),
            new PropertyMetadata(false));

    public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);
    public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);

    public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);
    public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);

    private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetValue(IsUpdatingProperty, value);
    private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);

    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox pb) return;

        if ((bool)e.OldValue)
            pb.PasswordChanged -= HandlePasswordChanged;
        if ((bool)e.NewValue)
            pb.PasswordChanged += HandlePasswordChanged;
    }

    private static void OnBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox pb) return;
        if (!GetBindPassword(pb)) return;
        if (GetIsUpdating(pb)) return;

        pb.Password = e.NewValue as string ?? string.Empty;
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb) return;

        SetIsUpdating(pb, true);
        try
        {
            SetBoundPassword(pb, pb.Password);
        }
        finally
        {
            SetIsUpdating(pb, false);
        }
    }
}

