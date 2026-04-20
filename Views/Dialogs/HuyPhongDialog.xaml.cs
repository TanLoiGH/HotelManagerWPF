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
using System.Windows.Shapes;

namespace QuanLyKhachSan_PhamTanLoi.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for HuyPhongDialog.xaml
    /// </summary>
    public partial class HuyPhongDialog : Window
    {
        public string LyDoHuy { get; private set; } = "";
        public decimal TienHoanTra { get; private set; } = 0;
        private decimal _tienCocCuaKhach;

        public HuyPhongDialog(decimal tienCoc = 0, bool isHuyRiengLe = false)
        {
            InitializeComponent();
            _tienCocCuaKhach = tienCoc;

            txtGoiYCoc.Text = $"(Khách đã cọc: {tienCoc:N0} VNĐ)";

            // Tự động nhảy lý do mặc định cho thông minh
            if (isHuyRiengLe)
            {
                cboLyDo.SelectedIndex = 0; // Nhảy vào dòng: "Huỷ riêng phòng này"
            }
            else
            {
                cboLyDo.SelectedIndex = 1; // Nhảy vào dòng: "Khách đổi ý / Hủy trước hạn"
            }
        }

        // Xử lý 2 nút Quick-action
        private void BtnHoanDu_Click(object sender, RoutedEventArgs e) => txtTienHoan.Text = _tienCocCuaKhach.ToString("N0");
        private void BtnKhongHoan_Click(object sender, RoutedEventArgs e) => txtTienHoan.Text = "0";
        private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cboLyDo.Text))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập lý do hủy phòng!", "Cảnh báo");
                return;
            }

            if (!decimal.TryParse(txtTienHoan.Text.Replace(",", "").Replace(".", ""), out decimal tien) || tien < 0)
            {
                MessageBox.Show("Số tiền hoàn trả không hợp lệ!", "Cảnh báo");
                return;
            }

            LyDoHuy = cboLyDo.Text.Trim();
            TienHoanTra = tien;
            DialogResult = true;
            Close();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Báo hiệu là bấm Hủy bỏ
            Close();
        }
    }
}
