using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for DoiPhongDialog.xaml
    /// </summary>
    public partial class DoiPhongDialog : Window
    {
        private readonly string _maDatPhong;
        private readonly string _maPhongCu;
        private readonly DatPhongService _datPhongSvc;
        private readonly QuanLyKhachSanContext _db = new();

        public DoiPhongDialog(string maDatPhong, string maPhongCu)
        {
            InitializeComponent();
            _maDatPhong = maDatPhong;
            _maPhongCu = maPhongCu;
            TxtPhongCu.Text = maPhongCu;

            _datPhongSvc = new DatPhongService(_db);

            LoadDanhSachPhongTrong();
        }

        private async void LoadDanhSachPhongTrong()
        {
            // Chỉ lấy các phòng đang trống (PTT01) để cho phép chuyển sang
            var phongTrongs = await _db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .Where(p => p.MaTrangThaiPhong == "PTT01")
                .Select(p => new { p.MaPhong, TenPhong = $"{p.MaPhong} - {p.MaLoaiPhongNavigation.TenLoaiPhong} ({p.MaLoaiPhongNavigation.GiaPhong:N0}đ)" })
                .ToListAsync();

            CboPhongMoi.ItemsSource = phongTrongs;
            if (phongTrongs.Any())
                CboPhongMoi.SelectedIndex = 0;
        }

        private async void BtnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            if (CboPhongMoi.SelectedValue is not string maPhongMoi)
            {
                MessageBox.Show("Vui lòng chọn phòng mới!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BtnXacNhan.IsEnabled = false;
                // Gọi hàm Backend đã sửa ban nãy
                await _datPhongSvc.DoiPhongAsync(_maDatPhong, _maPhongCu, maPhongMoi);

                MessageBox.Show($"Đã chuyển từ phòng {_maPhongCu} sang phòng {maPhongMoi} thành công!",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi", ex); 
                MessageBox.Show($"Lỗi đổi phòng:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnXacNhan.IsEnabled = true;
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _db.Dispose();
        }
    }
}
