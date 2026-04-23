using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public enum KetQuaThanhToan
{
    GhiNhanChuaDu,
    HoanTat,
    DaHoanTat,
    TuChoi
}

public record ThongTinThanhToan(KetQuaThanhToan KetQua, decimal TongDaThu, decimal ConLai, string ThongDiep);

public record KetQuaTinhToan(
    decimal TienPhong,
    decimal TienDichVu,
    decimal GiamGia,
    decimal TienVat,
    decimal TongThanhToan,
    decimal ConLai);

public static class TinhToanService
{
    public static KetQuaTinhToan TinhToanToanBo(
        decimal tienPhong, decimal tienDichVu, decimal vatPercent,
        decimal giaTriKm, string loaiKm, decimal tienCoc, decimal tongDaThuLichSu)
    {
        decimal giamGia = TinhGiamGia(tienPhong, loaiKm, giaTriKm);
        decimal tienPhongSauGiam = Math.Max(0, tienPhong - giamGia);
        decimal tienVat = tienPhongSauGiam * (vatPercent / 100m);
        decimal tongThanhToan = tienPhongSauGiam + tienVat + tienDichVu;
        decimal conLai = tongThanhToan - (tienCoc + tongDaThuLichSu);

        return new KetQuaTinhToan(tienPhong, tienDichVu, giamGia, tienVat, tongThanhToan, conLai);
    }

    // --- CÁC HÀM MỚI ĐƯỢC BỔ SUNG ĐỂ "GÁNH" TÍNH TOÁN CHO VIEWMODEL ---

    public static decimal TinhTienPhongThucTe(decimal donGia, DateTime ngayNhan, DateTime ngayTra)
    {
        return donGia * ThoiGianLuuTru(ngayNhan, ngayTra);
    }

    public static decimal TinhTongTienDichVu(IEnumerable<DichVuChiTiet> dichVus)
    {
        if (dichVus == null || !dichVus.Any()) return 0;
        return dichVus.Sum(d => (decimal)d.SoLuong * d.DonGia);
    }

    // -----------------------------------------------------------------

    //public static decimal TienPhongTamTinh(IEnumerable<decimal> giaPhongs, DateTime ngayNhan, DateTime ngayTra)
    //{
    //    if (giaPhongs == null || !giaPhongs.Any()) return 0;
    //    return giaPhongs.Sum() * ThoiGianLuuTru(ngayNhan, ngayTra);
    //}

    public static decimal TinhGiamGia(decimal tienPhong, string loaiKhuyenMai, decimal giaTriKm)
    {
        if (giaTriKm <= 0) return 0;
        return (loaiKhuyenMai == "Phần trăm") ? (tienPhong * giaTriKm / 100m) : giaTriKm;
    }

    public static int ThoiGianLuuTru(DateTime ngayNhan, DateTime ngayTra)
    {
        int soDem = (ngayTra.Date - ngayNhan.Date).Days;
        return Math.Max(1, soDem);
    }
}