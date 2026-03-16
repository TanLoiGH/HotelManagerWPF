using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class NavigationService
{
    public void OpenWindow(Window window)
    {
        window.Show();
    }

    public void CloseWindow(Window window)
    {
        window.Close();
    }
}