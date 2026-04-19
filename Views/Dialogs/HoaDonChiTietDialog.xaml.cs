using Microsoft.Extensions.DependencyInjection;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

public partial class HoaDonChiTietDialog : Window
{
    private readonly HoaDonChiTietViewModel _vm;
    private readonly QuanLyKhachSanContext _db;

    public HoaDonChiTietDialog(string maHoaDon, Func<Task>? taiLaiTrangHoaDonAsync = null)
    {
        InitializeComponent();

        // ✅ Lấy toàn bộ Service từ DI thay vì 'new' thủ công
        var hdSvc = App.ServiceProvider.GetRequiredService<IHoaDonService>();
        var dvSvc = App.ServiceProvider.GetRequiredService<IDichVuService>();
        var hopThoai = App.ServiceProvider.GetRequiredService<IHopThoaiService>();
        var inHoaDon = App.ServiceProvider.GetRequiredService<IInHoaDonService>();
        var chonDichVu = App.ServiceProvider.GetRequiredService<IChonDichVuService>();

        _vm = new HoaDonChiTietViewModel(
            maHoaDon,
            hoaDonSvc: hdSvc,
            dichVuSvc: dvSvc,
            layChuSoHuu: () => this,
            dong: dialogResult =>
            {
                DialogResult = dialogResult;
                Close();
            },
            hopThoai: hopThoai,
            inHoaDon: inHoaDon,
            chonDichVu: chonDichVu,
            taiLaiTrangHoaDonAsync: taiLaiTrangHoaDonAsync);

        DataContext = _vm;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _vm.TaiLaiAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _db.Dispose();
    }

    private void txtSoTien_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        // 1. Tạm tắt sự kiện để tránh lặp vô hạn (Infinite Loop)
        textBox.TextChanged -= txtSoTien_TextChanged;

        try
        {
            // 2. Lưu lại vị trí con trỏ chuột
            int selectionStart = textBox.SelectionStart;
            int lengthBefore = textBox.Text.Length;

            // 3. Xóa các ký tự không phải số để lấy số nguyên thủy
            string rawText = textBox.Text.Replace(",", "").Replace(".", "").Replace(" ", "");

            // 4. Định dạng lại thành tiền tệ (N0)
            if (decimal.TryParse(rawText, out decimal soTien))
            {
                textBox.Text = soTien.ToString("N0");

                int lengthAfter = textBox.Text.Length;
                int viTriMoi = selectionStart + (lengthAfter - lengthBefore);

                if (viTriMoi < 0) viTriMoi = 0;
                if (viTriMoi > lengthAfter) viTriMoi = lengthAfter;

                textBox.SelectionStart = viTriMoi;
            }
            else if (string.IsNullOrEmpty(rawText))
            {
                textBox.Text = "";
            }

            // 6. Cập nhật thẳng giá trị đã format xuống ViewModel
            _vm.SoTienNhap = textBox.Text;
        }
        finally
        {
            // Bật lại sự kiện
            textBox.TextChanged += txtSoTien_TextChanged;
        }

    }
}
