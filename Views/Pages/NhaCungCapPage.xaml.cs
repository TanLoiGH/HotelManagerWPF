using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhaCungCapRow
{
    public string MaNcc { get; set; } = "";
    public string TenNcc { get; set; } = "";
    public string DienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string NguoiLienHe { get; set; } = "";
    public bool? IsActive { get; set; }
    public string IsActiveText => IsActive == true ? "✓ Có" : "✗ Không";
    public SolidColorBrush IsActiveColor => IsActive == true
        ? new SolidColorBrush(Color.FromRgb(0, 184, 148))
        : new SolidColorBrush(Color.FromRgb(200, 200, 200));
}

public partial class NhaCungCapPage : Page
{
    private List<NhaCungCapRow> _all = [];
    private NhaCungCapRow? _selected;
    private bool _isNew;

    public NhaCungCapPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var nccSvc = new NhaCungCapService(db);
        var items = await nccSvc.LayDanhSachAsync();

        _all = items.Select(n => new NhaCungCapRow
        {
            MaNcc = n.MaNcc,
            TenNcc = n.TenNcc,
            DienThoai = n.DienThoai ?? "",
            Email = n.Email ?? "",
            DiaChi = n.DiaChi ?? "",
            NguoiLienHe = n.NguoiLienHe ?? "",
            IsActive = n.IsActive,
        }).ToList();

        NCCGrid.ItemsSource = _all;
        TxtTongNCC.Text = $"{_all.Count} nhà cung cấp";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        NCCGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(n =>
                n.TenNcc.ToLower().Contains(kw) ||
                n.DienThoai.Contains(kw) ||
                n.Email.ToLower().Contains(kw)).ToList();
    }

    private void NCCGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NCCGrid.SelectedItem is not NhaCungCapRow row) return;
        _selected = row;
        _isNew = false;
        ShowForm(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        NCCGrid.SelectedItem = null;
        ShowForm(null);
    }

    private void ShowForm(NhaCungCapRow? row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = row == null ? "Thêm nhà cung cấp mới" : "Sửa nhà cung cấp";

        TxtTen.Text = row?.TenNcc ?? "";
        TxtDienThoai.Text = row?.DienThoai ?? "";
        TxtEmail.Text = row?.Email ?? "";
        TxtNguoiLienHe.Text = row?.NguoiLienHe ?? "";
        TxtDiaChi.Text = row?.DiaChi ?? "";
        ChkActive.IsChecked = row?.IsActive ?? true;
        BtnXoa.IsEnabled = row != null;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            ConfirmHelper.ShowWarning("Vui lòng nhập tên nhà cung cấp.");
            return;
        }

        if (!ConfirmHelper.ConfirmSave()) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var nccSvc = new NhaCungCapService(db);

            if (_isNew)
            {
                await nccSvc.TaoMoiAsync(
                    ten,
                    TxtDienThoai.Text.Trim(),
                    TxtEmail.Text.Trim(),
                    TxtDiaChi.Text.Trim(),
                    TxtNguoiLienHe.Text.Trim(),
                    ChkActive.IsChecked ?? true);
            }
            else if (_selected != null)
            {
                await nccSvc.CapNhatAsync(
                    _selected.MaNcc,
                    ten,
                    TxtDienThoai.Text.Trim(),
                    TxtEmail.Text.Trim(),
                    TxtDiaChi.Text.Trim(),
                    TxtNguoiLienHe.Text.Trim(),
                    ChkActive.IsChecked ?? true);
            }
            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnXoa_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        if (!ConfirmHelper.ConfirmDeactivate(_selected.TenNcc)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var nccSvc = new NhaCungCapService(db);
            bool daVoHieuHoa = await nccSvc.XoaHoacVoHieuHoaAsync(_selected.MaNcc);
            if (daVoHieuHoa)
                ConfirmHelper.ShowInfo("NCC đã có dữ liệu liên quan — đã vô hiệu hóa thay vì xóa.");

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);

            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}


