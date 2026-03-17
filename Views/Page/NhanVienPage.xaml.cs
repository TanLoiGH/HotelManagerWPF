using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class NhanVienRow : NhanVienViewModel
{
    public string QuyenText => string.Join(", ", Quyen);
}

public partial class NhanVienPage : Page
{
    private List<NhanVienRow> _allNV = [];

    public NhanVienPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var db = new QuanLyKhachSanContext();
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
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
}