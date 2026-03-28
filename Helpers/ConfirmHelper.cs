using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Helpers;

/// <summary>
/// Lớp trợ giúp quản lý các hộp thoại xác nhận tập trung (OOP Helper)
/// </summary>
public static class ConfirmHelper
{
    private const string TitleConfirm = "Xác nhận";
    private const string TitleSave = "Xác nhận lưu";
    private const string TitleDelete = "Xác nhận xóa";
    private const string TitleError = "Lỗi";
    private const string TitleInfo = "Thông báo";

    public static bool Confirm(string message, string title = TitleConfirm)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public static bool ConfirmSave(string itemName = "")
    {
        string message = string.IsNullOrEmpty(itemName)
            ? "Bạn có muốn lưu các thay đổi này không?"
            : $"Bạn có muốn lưu thông tin cho \"{itemName}\" không?";
        return Confirm(message, TitleSave);
    }

    public static bool ConfirmDelete(string itemName)
    {
        return MessageBox.Show($"Bạn có chắc chắn muốn xóa \"{itemName}\" không?\nThao tác này không thể hoàn tác.",
            TitleDelete, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public static bool ConfirmDeactivate(string itemName)
    {
        return MessageBox.Show($"Bạn có muốn vô hiệu hóa \"{itemName}\" không?\nDữ liệu vẫn được giữ lại nhưng sẽ không hiển thị ở các chức năng khác.",
            "Xác nhận vô hiệu hóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public static void ShowError(string message)
    {
        MessageBox.Show(message, TitleError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowWarning(string message)
    {
        MessageBox.Show(message, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static void ShowInfo(string message)
    {
        MessageBox.Show(message, TitleInfo, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
