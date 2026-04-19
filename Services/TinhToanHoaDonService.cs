using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

// Đưa các định nghĩa này lên đầu hoặc trong namespace chung để các file khác đều thấy
public enum KetQuaThanhToan { GhiNhanChuaDu, HoanTat, DaHoanTat, TuChoi }
public record ThongTinThanhToan(KetQuaThanhToan KetQua, decimal TongDaThu, decimal ConLai, string ThongDiep);

public record KetQuaTinhToanHd(
    decimal TienPhong,
    decimal TienDichVu,
    decimal GiamGia,
    decimal TienVat,
    decimal TongThanhToan,
    decimal ConLai);

public static class TinhToanHoaDonService
{
    // HÀM MỚI: Dùng cho logic đồng bộ
    public static KetQuaTinhToanHd TinhToanToanBo(
        decimal tienPhong,
        decimal tienDichVu,
        decimal vatPercent,
        decimal giaTriKm,
        string loaiKm,
        decimal tienCoc,
        decimal tongDaThuLichSu)
    {
        decimal giamGia = 0;
        if (giaTriKm > 0)
        {
            giamGia = (loaiKm == "Phần trăm") ? (tienPhong * giaTriKm / 100m) : giaTriKm;
        }

        decimal tienPhongSauGiam = Math.Max(0, tienPhong - giamGia);
        decimal tienVat = tienPhongSauGiam * (vatPercent / 100m);
        decimal tongThanhToan = tienPhongSauGiam + tienVat + tienDichVu;
        decimal conLai = tongThanhToan - (tienCoc + tongDaThuLichSu);

        return new KetQuaTinhToanHd(tienPhong, tienDichVu, giamGia, tienVat, tongThanhToan, conLai);
    }

    // HÀM CŨ: Khôi phục lại để DatPhongService.cs không báo lỗi CS0117
    public static decimal TinhTongThanhToan(decimal tienPhong, decimal tienDichVu, decimal vatPercent, decimal tienCoc, decimal giamGia)
    {
        decimal tienPhongSauGiam = Math.Max(0, tienPhong - giamGia);
        decimal tongThanhToan = (tienPhongSauGiam * (1 + vatPercent / 100m)) + tienDichVu - tienCoc;
        return Math.Max(0, tongThanhToan);
    }

    public static decimal TinhGiamGia(decimal tienPhong, decimal tienDichVu, string loaiKhuyenMai, decimal giaTriKm)
    {
        if (giaTriKm <= 0) return 0;
        return (loaiKhuyenMai == "Phần trăm") ? (tienPhong * giaTriKm / 100m) : giaTriKm;
    }

    public static int TinhSoDem(DateTime ngayNhan, DateTime ngayTra)
    {
        int soDem = (ngayTra.Date - ngayNhan.Date).Days;
        return Math.Max(1, soDem);
    }
}