using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhanVienRow : NhanVienViewModel
{
    public string QuyenText => string.Join(", ", Quyen);
}

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

        // Load combos
        var trangThais = await db.TrangThaiNhanViens.ToListAsync();
        CboTrangThai.ItemsSource = trangThais;
        CboTrangThai.DisplayMemberPath = "TenTrangThai";
        CboTrangThai.SelectedValuePath = "MaTrangThai";

        var quyens = await db.PhanQuyens.ToListAsync();
        CboQuyen.ItemsSource = quyens;
        CboQuyen.DisplayMemberPath = "TenQuyen";
        CboQuyen.SelectedValuePath = "MaQuyen";

        _allNV = await db.NhanViens
            .Include(nv => nv.MaTrangThaiNavigation)
            .Include(nv => nv.TaiKhoans)
                .ThenInclude(tk => tk.MaQuyenNavigation)
            .Select(nv => new NhanVienRow
            {
                MaNhanVien = nv.MaNhanVien,
                TenNhanVien = nv.TenNhanVien,
                ChucVu = nv.ChucVu ?? "",
                DienThoai = nv.DienThoai ?? "",
                TenTrangThai = nv.MaTrangThaiNavigation != null
                               ? nv.MaTrangThaiNavigation.TenTrangThai ?? "" : "",
                Quyen = nv.TaiKhoans
                               .Where(t => t.IsActive == true)
                               .Select(t => t.MaQuyenNavigation.TenQuyen ?? "")
                               .ToList()
            })
            .ToListAsync();

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
        var nv = await db.NhanViens
            .Include(n => n.TaiKhoans)
            .FirstOrDefaultAsync(n => n.MaNhanVien == row.MaNhanVien);

        if (nv == null) return;

        TxtEmail.Text = nv.Email ?? "";
        TxtCCCD.Text = nv.Cccd ?? "";
        CboTrangThai.SelectedValue = nv.MaTrangThai;

        var tk = nv.TaiKhoans.FirstOrDefault(t => t.IsActive == true);
        TxtTenDangNhap.Text = tk?.TenDangNhap ?? "";
        PbMatKhau.Password = "";  // không hiện mật khẩu cũ
        CboQuyen.SelectedValue = tk?.MaQuyen;
    }

    private async void BtnLuu_Click(object sender, RoutedEventArgs e)
    {
        string ten = TxtTen.Text.Trim();
        if (string.IsNullOrWhiteSpace(ten))
        {
            MessageBox.Show("Vui lòng nhập họ tên.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();

            if (_isNew)
            {
                // Kiểm tra tên đăng nhập trùng
                string tdnhap = TxtTenDangNhap.Text.Trim();
                if (!string.IsNullOrWhiteSpace(tdnhap))
                {
                    bool trung = await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tdnhap);
                    if (trung)
                    {
                        MessageBox.Show("Tên đăng nhập đã tồn tại.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var lastMaNV = await db.NhanViens
                    .OrderByDescending(n => n.MaNhanVien)
                    .Select(n => n.MaNhanVien)
                    .FirstOrDefaultAsync();

                var nv = new NhanVien
                {
                    MaNhanVien = MaHelper.Next("NV", lastMaNV),
                    TenNhanVien = ten,
                    ChucVu = TxtChucVu.Text.Trim(),
                    DienThoai = TxtDienThoai.Text.Trim(),
                    Email = TxtEmail.Text.Trim(),
                    Cccd = TxtCCCD.Text.Trim(),
                    MaTrangThai = CboTrangThai.SelectedValue as string ?? "TT01",
                    NgayVaoLam = DateOnly.FromDateTime(DateTime.Today),
                };
                db.NhanViens.Add(nv);

                // Tạo tài khoản nếu có
                if (!string.IsNullOrWhiteSpace(TxtTenDangNhap.Text)
                    && !string.IsNullOrWhiteSpace(PbMatKhau.Password)
                    && CboQuyen.SelectedValue is string maQuyen)
                {
                    db.TaiKhoans.Add(new TaiKhoan
                    {
                        MaNhanVien = nv.MaNhanVien,
                        MaQuyen = maQuyen,
                        TenDangNhap = TxtTenDangNhap.Text.Trim(),
                        MatKhau = PbMatKhau.Password,
                        IsActive = true,
                    });
                }
            }
            else if (_selected != null)
            {
                var nv = await db.NhanViens
                    .Include(n => n.TaiKhoans)
                    .FirstOrDefaultAsync(n => n.MaNhanVien == _selected.MaNhanVien);

                if (nv != null)
                {
                    nv.TenNhanVien = ten;
                    nv.ChucVu = TxtChucVu.Text.Trim();
                    nv.DienThoai = TxtDienThoai.Text.Trim();
                    nv.Email = TxtEmail.Text.Trim();
                    nv.Cccd = TxtCCCD.Text.Trim();
                    nv.MaTrangThai = CboTrangThai.SelectedValue as string ?? "TT01";

                    // Cập nhật tài khoản
                    var tk = nv.TaiKhoans.FirstOrDefault(t => t.IsActive == true);
                    if (tk != null && !string.IsNullOrWhiteSpace(PbMatKhau.Password))
                        tk.MatKhau = PbMatKhau.Password;

                    if (tk != null && CboQuyen.SelectedValue is string mq)
                        tk.MaQuyen = mq;
                }
            }

            await db.SaveChangesAsync();
            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
            MessageBox.Show("Lưu thành công!", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (MessageBox.Show($"Vô hiệu hóa nhân viên \"{_selected.TenNhanVien}\"?\n(Không xóa dữ liệu, chỉ đổi trạng thái)",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes) return;

        try
        {
            using var db = new QuanLyKhachSanContext();
            var nv = await db.NhanViens
                .Include(n => n.TaiKhoans)
                .FirstOrDefaultAsync(n => n.MaNhanVien == _selected.MaNhanVien);

            if (nv != null)
            {
                nv.MaTrangThai = "TT02"; // Nghỉ việc
                foreach (var tk in nv.TaiKhoans)
                    tk.IsActive = false;
                await db.SaveChangesAsync();
            }

            await LoadAsync();
            PanelForm.Visibility = Visibility.Collapsed;
            PanelEmpty.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}