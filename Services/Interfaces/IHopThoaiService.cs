using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

public interface IHopThoaiService
{
    bool XacNhan(string noiDung, string tieuDe = "Xác nhận");
    void ThongBao(string noiDung, string tieuDe = "Thông báo");
    void CanhBao(string noiDung, string tieuDe = "Cảnh báo");
    void BaoLoi(string noiDung, string tieuDe = "Lỗi");
}

public sealed class HopThoaiServiceWpf : IHopThoaiService
{
    public bool XacNhan(string noiDung, string tieuDe = "Xác nhận")
        => MessageBox.Show(noiDung, tieuDe, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ThongBao(string noiDung, string tieuDe = "Thông báo")
        => MessageBox.Show(noiDung, tieuDe, MessageBoxButton.OK, MessageBoxImage.Information);

    public void CanhBao(string noiDung, string tieuDe = "Cảnh báo")
        => MessageBox.Show(noiDung, tieuDe, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void BaoLoi(string noiDung, string tieuDe = "Lỗi")
        => MessageBox.Show(noiDung, tieuDe, MessageBoxButton.OK, MessageBoxImage.Error);
}

