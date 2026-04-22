using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.Views
{
    public partial class TienNghiDanhMucPage : Page
    {
        private List<TienNghiDanhMuc> _allDanhMucs = new();
        private bool _isNew = true;
        private string? _maDanhMucDangChon = null;

        private static bool IsAdminRole()
        {
            var mq = (AppSession.MaQuyen ?? "").Trim();
            return mq == "ADMIN" || mq == "GIAM_DOC";
        }

        public TienNghiDanhMucPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            bool canCrud = IsAdminRole();
            BtnThem.IsEnabled = canCrud;
            BtnLuu.IsEnabled = canCrud;

            try
            {
                using var db = new QuanLyKhachSanContext();
                var tnSvc = new TienNghiService(db);
                _allDanhMucs = await tnSvc.LayTatCaDanhMucAsync();

                ApplyFilter(); // Áp dụng thanh search ngay khi load
                ShowEmptyState(); // Load xong thì luôn về trạng thái rỗng
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Lỗi Cơ Sở Dữ Liệu:\n{errorMsg}", "Lỗi CSDL", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Logic Tìm Kiếm
        private void ApplyFilter()
        {
            var keyword = TxtSearch?.Text.Trim().ToLower() ?? "";
            GridDanhMuc.ItemsSource = string.IsNullOrEmpty(keyword)
                ? _allDanhMucs
                : _allDanhMucs.Where(x =>
                    x.TenDanhMuc.ToLower().Contains(keyword) ||
                    x.MaDanhMuc.ToLower().Contains(keyword)).ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        // 1. Giao diện: Trạng thái trống (Không chọn gì cả)
        private void ShowEmptyState()
        {
            _isNew = true;
            _maDanhMucDangChon = null;
            GridDanhMuc.SelectedItem = null; // Bỏ chọn trên lưới

            PanelEmpty.Visibility = Visibility.Visible;
            PanelForm.Visibility = Visibility.Collapsed;
        }

        // 2. Giao diện: Trạng thái hiển thị Form (Khi Thêm hoặc Sửa)
        private void ShowForm(TienNghiDanhMuc? row = null)
        {
            PanelEmpty.Visibility = Visibility.Collapsed;
            PanelForm.Visibility = Visibility.Visible;

            if (row == null) // Chế độ THÊM MỚI
            {
                _isNew = true;
                _maDanhMucDangChon = null;
                TxtFormTitle.Text = "Thêm danh mục mới";
                TxtTenDanhMuc.Text = "";
                ChkActive.IsChecked = true;

                BtnXoa.Visibility = Visibility.Collapsed; // Đang thêm thì ẩn luôn nút Xóa cho logic
                GridDanhMuc.SelectedItem = null;
            }
            else // Chế độ SỬA
            {
                _isNew = false;
                _maDanhMucDangChon = row.MaDanhMuc;
                TxtFormTitle.Text = "Sửa danh mục";
                TxtTenDanhMuc.Text = row.TenDanhMuc;
                ChkActive.IsChecked = row.IsActive ?? false;

                BtnXoa.Visibility = Visibility.Visible; // Hiện lại nút xóa
                BtnXoa.IsEnabled = IsAdminRole();
            }
        }

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            ShowForm(null); // Gọi Form rỗng để Thêm mới
        }

        private void GridDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridDanhMuc.SelectedItem is TienNghiDanhMuc row)
            {
                ShowForm(row); // Đổ dữ liệu dòng đang chọn lên Form
            }
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdminRole())
            {
                MessageBox.Show("Bạn không có quyền quản trị danh mục.", "Từ chối", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string tenDM = TxtTenDanhMuc.Text.Trim();
            if (string.IsNullOrWhiteSpace(tenDM))
            {
                MessageBox.Show("Vui lòng nhập tên danh mục.", "Thiếu thông tin", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isActive = ChkActive.IsChecked == true;
            try
            {
                using var db = new QuanLyKhachSanContext();
                var tnSvc = new TienNghiService(db);

                if (_isNew)
                {
                    await tnSvc.TaoMoiDanhMucAsync(tenDM, isActive);
                    MessageBox.Show("Thêm danh mục thành công!", "Thông báo", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (_maDanhMucDangChon != null)
                {
                    await tnSvc.CapNhatDanhMucAsync(_maDanhMucDangChon, tenDM, isActive);
                    MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                await LoadDataAsync(); // Load xong nó sẽ tự gọi ShowEmptyState()
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Chi tiết lỗi CSDL:\n{errorMsg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdminRole() || _maDanhMucDangChon == null) return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa danh mục \"{TxtTenDanhMuc.Text}\" không?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new QuanLyKhachSanContext();
                var tnSvc = new TienNghiService(db);

                await tnSvc.XoaDanhMucAsync(_maDanhMucDangChon);

                MessageBox.Show("Xóa danh mục thành công!", "Thông báo", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Chi tiết lỗi CSDL:\n{errorMsg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}