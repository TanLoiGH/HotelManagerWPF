using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public static class TinhToanHoaDonService
{
    public static int TinhSoDem(DateTime ngayNhan, DateTime ngayTra)
    {
        
        int soDem = (ngayTra.Date - ngayNhan.Date).Days;
        return Math.Max(1, soDem);
    }

    public static decimal TinhGiamGia(decimal tienPhong, decimal tienDichVu, string loaiKhuyenMai, decimal giaTriKm)
    {
        if (giaTriKm <= 0) return 0;

        if (loaiKhuyenMai == "Phần trăm")
            return (tienPhong + tienDichVu) * giaTriKm / 100m;

        return giaTriKm;
    }

    public static decimal TinhTongThanhToan(decimal tienPhong, decimal tienDichVu, decimal vatPercent, decimal tienCoc, decimal giamGia)
    {
        decimal tongTienGoc = tienPhong + tienDichVu;
        decimal tienVat = tongTienGoc * (vatPercent / 100m);
        decimal tongThanhToan = (tongTienGoc + tienVat) - giamGia - tienCoc;
        // Đảm bảo không âm (nếu cọc lớn hơn tổng bill thì trả về 0đ)
        return tongThanhToan < 0 ? 0 : tongThanhToan;
    }

    public static decimal TinhTienPhongTheoNgayTra(
        IEnumerable<DatPhongChiTiet> chiTiets,
        DateTime ngayTraMoi,
        Action<DatPhongChiTiet, int>? capNhatSoDem = null,
        bool capNhatNgayTra = false)
    {
        decimal tienPhong = 0;

        foreach (var ct in chiTiets)
        {
            if (capNhatNgayTra)
                ct.NgayTra = ngayTraMoi;

            int soDem = TinhSoDem(ct.NgayNhan, ngayTraMoi);
            capNhatSoDem?.Invoke(ct, soDem);

            tienPhong += ct.DonGia * soDem;
        }

        return tienPhong;
    }
}
