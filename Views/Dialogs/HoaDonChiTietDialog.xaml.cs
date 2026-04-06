using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs;

public partial class HoaDonChiTietDialog : Window
{
    private readonly HoaDonChiTietViewModel _vm;
    private readonly QuanLyKhachSanContext _db;

    public HoaDonChiTietDialog(string maHoaDon, Func<Task>? taiLaiTrangHoaDonAsync = null)
    {
        InitializeComponent();

        _db = new QuanLyKhachSanContext();
        var khSvc = new KhachHangService(_db);
        var hdSvc = new HoaDonService(_db, khSvc);
        var dvSvc = new DichVuService(_db);
        var hopThoai = new HopThoaiServiceWpf();
        var inHoaDon = new InHoaDonServiceWpf();
        var chonDichVu = new ChonDichVuServiceWpf();

        _vm = new HoaDonChiTietViewModel(
            maHoaDon,
            hoaDonSvc: hdSvc,
            dichVuSvc: dvSvc,
            getOwner: () => this,
            close: dialogResult =>
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
        await _vm.ReloadAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _db.Dispose();
    }
}
