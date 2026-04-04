using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
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
    /// Interaction logic for HoaDonChiTietDialog.xaml
    /// </summary>
    public partial class HoaDonChiTietDialog : Window
    {
        private readonly string _maHoaDon;

        public HoaDonChiTietDialog(string maHoaDon)
        {
            InitializeComponent();
            _maHoaDon = maHoaDon;
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            await LoadHoaDon(_maHoaDon);
        }

        private async Task LoadHoaDon(string maHoaDon)
        {
            using var db = new QuanLyKhachSanContext();
            var hdSvc = new HoaDonService(db, new KhachHangService(db));
            await hdSvc.EnsureHoaDonChiTietAsync(maHoaDon);

            var hd = await db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.MaKhachHangNavigation)
                .Include(h => h.MaNhanVienNavigation)
                .Include(h => h.HoaDonChiTiets)
                    .ThenInclude(ct => ct.DatPhongChiTiet)
                .Include(h => h.DichVuChiTiets)
                    .ThenInclude(dv => dv.MaDichVuNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null) return;

            var vm = new HoaDonDetailViewModel
            {
                MaHoaDon = hd.MaHoaDon,
                KhachHang = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "",
                NhanVien = hd.MaNhanVienNavigation?.TenNhanVien ?? "",
                NgayLap = hd.NgayLap,
                TrangThai = hd.TrangThai ?? "",

                TienPhong = hd.TienPhong ?? 0,
                TienDichVu = hd.TienDichVu ?? 0,
                VAT = hd.Vat ?? 0,
                TongThanhToan = hd.TongThanhToan ?? 0
            };

            vm.Phongs = hd.HoaDonChiTiets.Select(p => new PhongItem
            {
                MaPhong = p.MaPhong,
                NgayNhan = p.DatPhongChiTiet.NgayNhan,
                NgayTra = p.DatPhongChiTiet.NgayTra,
                SoDem = p.SoDem,
                DonGia = p.DatPhongChiTiet.DonGia
            }).ToList();

            vm.DichVus = hd.DichVuChiTiets.Select(d => new DichVuItem
            {
                TenDichVu = d.MaDichVuNavigation.TenDichVu,
                SoLuong = d.SoLuong,
                DonGia = d.DonGia
            }).ToList();

            DataContext = vm;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            // gọi PrintHelper.PrintPOSInvoice(...)
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            // mở dialog thanh toán
        }
    }
}
