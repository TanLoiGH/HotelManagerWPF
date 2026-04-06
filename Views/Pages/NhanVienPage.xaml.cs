using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhanVienRow : NhanVienViewModel
{
    public string QuyenText => string.Join(", ", Quyen);
}

public record BoolOption(bool Value, string Text);

public partial class NhanVienPage : Page
{
    private List<NhanVienRow> _allNV = [];
    private NhanVienRow? _selected;
    private bool _isNew;

    public NhanVienPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var employeeSvc = new EmployeeService(db);

        // Load combos
        var trangThais = await employeeSvc.GetEmployeeStatusesAsync();
        CboTrangThai.ItemsSource = trangThais;
        CboTrangThai.DisplayMemberPath = "TenTrangThai";
        CboTrangThai.SelectedValuePath = "MaTrangThai";

        var quyens = await employeeSvc.GetRolesAsync();
        CboQuyen.ItemsSource = quyens;
        CboQuyen.DisplayMemberPath = "TenQuyen";
        CboQuyen.SelectedValuePath = "MaQuyen";

        CboTaiKhoanActive.ItemsSource = new List<BoolOption>
        {
            new(true, "Hoạt động"),
            new(false, "Khóa"),
        };
        CboTaiKhoanActive.DisplayMemberPath = "Text";
        CboTaiKhoanActive.SelectedValuePath = "Value";

        var nhanViens = await employeeSvc.GetEmployeesAsync();
        _allNV = nhanViens
            .Select(nv => new NhanVienRow
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "",
                DienThoai = nv.DienThoai ?? "",
                TenTrangThai = nv.MaTrangThaiNavigation != null
                               ? nv.MaTrangThaiNavigation.TenTrangThai ?? "" : "",
                Quyen = nv.TaiKhoans
                    .OrderByDescending(t => t.IsActive == true)
                    .Select(t => t.MaQuyenNavigation.TenQuyen ?? "")
                    .Take(1)
                    .ToList()
            })
            .ToList();

        NVGrid.ItemsSource = _allNV;
        TxtTongNV.Text = $"{_allNV.Count} nhân viên";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var kw = TxtSearch.Text.Trim().ToLower();
        NVGrid.ItemsSource = string.IsNullOrEmpty(kw)
            ? _allNV
            : _allNV.Where(n =>
                n.TenNhanVien.ToLower().Contains(kw) ||
                n.ChucVu.ToLower().Contains(kw) ||
                n.DienThoai.Contains(kw)).ToList();
    }

    private async void NVGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NVGrid.SelectedItem is not NhanVienRow row) return;
        _selected = row;
        _isNew = false;
        await ShowFormAsync(row);
    }

    private void BtnThem_Click(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _isNew = true;
        NVGrid.SelectedItem = null;
        ShowFormNew();
    }

    private void ShowFormNew()
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = "Thêm nhân viên mới";
        TxtTen.Text = TxtChucVu.Text = TxtDienThoai.Text
            = TxtEmail.Text = TxtCCCD.Text
            = TxtTenDangNhap.Text = "";
        PbMatKhau.Password = "";
        CboTrangThai.SelectedIndex = 0;
        CboQuyen.SelectedIndex = 0;
        CboTaiKhoanActive.SelectedValue = true;
        BtnXoa.IsEnabled = false;
    }

    private async Task ShowFormAsync(NhanVienRow row)
    {
        PanelEmpty.Visibility = Visibility.Collapsed;
        PanelForm.Visibility = Visibility.Visible;
        TxtFormTitle.Text = "Sửa nhân viên";

        TxtTen.Text = row.TenNhanVien;
        TxtChucVu.Text = row.ChucVu;
        TxtDienThoai.Text = row.DienThoai;
        BtnXoa.IsEnabled = true;

        // Load thêm email, cccd, tài khoản từ DB
        using var db = new QuanLyKhachSanContext();
        var employeeSvc = new EmployeeService(db);
        var nv = await employeeSvc.LayNhanVienVaTaiKhoanAsync(row.MaNhanVien);

        if (nv == null) return;

        TxtEmail.Text = nv.Email ?? "";
        TxtCCCD.Text = nv.Cccd ?? "";
        CboTrangThai.SelectedValue = nv.MaTrangThai;

        var tk = nv.TaiKhoans
            .OrderByDescending(t => t.IsActive == true)
            .FirstOrDefault();
        TxtTenDangNhap.Text = tk?.TenDangNhap ?? "";
        PbMatKhau.Password = "";  // không hiện mật khẩu cũ
        CboQuyen.SelectedValue = tk?.MaQuyen;
        CboTaiKhoanActive.SelectedValue = tk?.IsActive ?? false;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            ConfirmHelper.ShowWarning("Vui lòng nhập họ tên.");
            return;
        }

        if (!ConfirmHelper.ConfirmSave(ten)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var employeeSvc = new EmployeeService(db);

            string tenDangNhap = TxtTenDangNhap.Text.Trim();
            string matKhau = PbMatKhau.Password;
            string? maQuyen = CboQuyen.SelectedValue as string;
            bool isTkActive = CboTaiKhoanActive.SelectedValue as bool? ?? true;
            await employeeSvc.LuuNhanVienVaTaiKhoanAsync(
                _isNew,
                _selected?.MaNhanVien,
                ten,
                TxtChucVu.Text.Trim(),
                TxtDienThoai.Text.Trim(),
                TxtEmail.Text.Trim(),
                TxtCCCD.Text.Trim(),
                CboTrangThai.SelectedValue as string ?? "TT01",
                tenDangNhap,
                matKhau,
                maQuyen,
                isTkActive);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
            MessageBox.Show("Lưu thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnXoa_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        if (!ConfirmHelper.ConfirmDeactivate(_selected.TenNhanVien)) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var employeeSvc = new EmployeeService(db);
            await employeeSvc.VoHieuHoaNhanVienAsync(_selected.MaNhanVien);

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ConfirmHelper.ShowError($"Lỗi: {ex.Message}");
        }
    }
}

