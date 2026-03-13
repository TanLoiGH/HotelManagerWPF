using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace QuanLyKhachSan_PhamTanLoi.Views
{    
    public partial class PhongPage : Page
    {
        private QuanLyKhachSanContext db = new QuanLyKhachSanContext();
        public PhongPage()
        {
            InitializeComponent();
            LoaiPhong();
        }

        private void LoadPhong()
        {
            var ds = db.Phongs.Select(p => new PhongViewModel
            {
                MaPhong = p.MaPhong,
                TenLoaiPhong = p.MaLoaiPhong,
                TrangThai = p.MaTrangThaiPhong,

            }).ToList();

            DanhSachPhong.ItemsSource = ds;
        }

    }
}
