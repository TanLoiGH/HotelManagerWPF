using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels
{
    public class HoaDonDetailViewModel
    {
        public string MaHoaDon { get; set; } = "";
        public string KhachHang { get; set; } = "";
        public string NhanVien { get; set; } = "";
        public DateTime? NgayLap { get; set; }
        public string TrangThai { get; set; } = "";

        public List<PhongItem> Phongs { get; set; } = [];
        public List<DichVuItem> DichVus { get; set; } = [];

        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal VAT { get; set; }
        public decimal TongThanhToan { get; set; }

        public string TienPhongText => TienPhong.ToString("N0") + " ₫";
        public string TienDichVuText => TienDichVu.ToString("N0") + " ₫";
        public string VATText => VAT.ToString("N0") + " ₫";
        public string TongThanhToanText => TongThanhToan.ToString("N0") + " ₫";
    }

    public class PhongItem
    {
        public string MaPhong { get; set; } = "";
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public int SoDem { get; set; }
        public decimal DonGia { get; set; }

        public string DonGiaText => DonGia.ToString("N0") + " ₫";
    }

    public class DichVuItem
    {
        public string TenDichVu { get; set; } = "";
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public decimal ThanhTien => DonGia * SoLuong;

        public string DonGiaText => DonGia.ToString("N0") + " ₫";
        public string ThanhTienText => ThanhTien.ToString("N0") + " ₫";
    }
}
