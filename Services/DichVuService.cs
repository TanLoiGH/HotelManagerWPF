using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DichVuService : IDichVuService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Thay vì hardcode số 10 ở nhiều nơi, hãy đưa lên thành Hằng số cục bộ
    private const decimal DEFAULT_VAT_PERCENT = 10m;

    public DichVuService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    #region QUẢN LÝ DANH MỤC DỊCH VỤ

    public async Task<List<DichVu>> LayDanhSachAsync()
    {
        // Sử dụng AsNoTracking() giúp tăng tốc độ đọc dữ liệu lên đến 30% khi chỉ cần hiển thị
        return await _db.DichVus
            .AsNoTracking()
            .OrderBy(d => d.TenDichVu)
            .ToListAsync();
    }

    // Senior Note: Hai hàm dưới đây có vẻ bị trùng lặp do thiết kế Interface. 
    // Tôi bổ sung AsNoTracking() để tối ưu hóa performance giống LayDanhSachAsync.
    public async Task<List<DichVu>> GetDichVusAsync()
    {
        return await _db.DichVus.AsNoTracking().ToListAsync();
    }

    public async Task<List<DichVu>> GetAllDichVuAsync()
    {
        return await _db.DichVus
            .AsNoTracking()
            .Where(d => d.IsActive == true)
            .ToListAsync();
    }

    public async Task TaoMoiAsync(string tenDichVu, decimal gia, string? donViTinh, bool isActive)
    {
        var lastMa = await _db.DichVus
            .OrderByDescending(d => d.MaDichVu)
            .Select(d => d.MaDichVu)
            .FirstOrDefaultAsync();

        _db.DichVus.Add(new DichVu
        {
            MaDichVu = MaHelper.Next("DV", lastMa),
            TenDichVu = tenDichVu,
            Gia = gia,
            DonViTinh = donViTinh,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maDichVu, string tenDichVu, decimal gia, string? donViTinh, bool isActive)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu);
        if (dv == null) return;

        dv.TenDichVu = tenDichVu;
        dv.Gia = gia;
        dv.DonViTinh = donViTinh;
        dv.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacTatAsync(string maDichVu)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu);
        if (dv == null) return false;

        // Nếu dịch vụ đã từng được sử dụng -> Chỉ Soft Delete (Tắt IsActive)
        bool coGiaoDich = await _db.DichVuChiTiets.AnyAsync(d => d.MaDichVu == dv.MaDichVu);
        if (coGiaoDich)
        {
            dv.IsActive = false;
            await _db.SaveChangesAsync();
            return true; // Trả về true báo hiệu là đã Soft Delete
        }

        // Nếu chưa từng sử dụng -> Xóa cứng (Hard Delete)
        _db.DichVus.Remove(dv);
        await _db.SaveChangesAsync();
        return false; // Trả về false báo hiệu là đã xóa cứng khỏi DB
    }

    #endregion

    #region XỬ LÝ GỌI DỊCH VỤ VÀO HÓA ĐƠN

    public async Task<List<DichVuChiTiet>> GetDichVuChiTietsForHoaDonAsync(string maHoaDon)
    {
        return await _db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .AsNoTracking()
            .Where(d => d.MaHoaDon == maHoaDon)
            .ToListAsync();
    }

    public async Task UpsertDichVuAsync(string maHoaDon, string maDatPhong, string maPhong, string maDichVu,
        int soLuong)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. TÌM HÓA ĐƠN CHUẨN
            var hd = await _db.HoaDons
                .FirstOrDefaultAsync(h => (h.MaHoaDon == maHoaDon || h.MaDatPhong == maDatPhong)
                                          && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

            // 2. NẾU CHƯA CÓ HÓA ĐƠN -> TỰ TẠO VỎ HÓA ĐƠN
            if (hd == null)
            {
                var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon)
                    .FirstOrDefaultAsync();
                hd = new HoaDon
                {
                    MaHoaDon = MaHelper.Next("HD", lastMa),
                    MaDatPhong = maDatPhong,
                    NgayLap = TimeHelper.GetVietnamTime(),
                    TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan,
                    TienPhong = 0,
                    TienDichVu = 0,
                    Vat = DEFAULT_VAT_PERCENT // Dùng hằng số thay vì hardcode 10
                };
                _db.HoaDons.Add(hd);
                await _db.SaveChangesAsync();
            }

            string maHdThucTe = hd.MaHoaDon;

            // 3. THÊM HOẶC CẬP NHẬT DỊCH VỤ CHI TIẾT
            var existing = await _db.DichVuChiTiets.FirstOrDefaultAsync(d =>
                d.MaHoaDon == maHdThucTe && d.MaDichVu == maDichVu && d.MaPhong == maPhong);

            if (existing != null)
            {
                existing.SoLuong += soLuong;
            }
            else
            {
                // Thay thế Exception chung chung bằng KeyNotFoundException
                var dv = await _db.DichVus.FindAsync(maDichVu)
                         ?? throw new KeyNotFoundException($"Dịch vụ mã {maDichVu} không tồn tại trong hệ thống.");

                _db.DichVuChiTiets.Add(new DichVuChiTiet
                {
                    MaHoaDon = maHdThucTe,
                    MaDatPhong = maDatPhong,
                    MaPhong = maPhong,
                    MaDichVu = maDichVu,
                    SoLuong = soLuong,
                    DonGia = dv.Gia ?? 0,
                    NgaySuDung = TimeHelper.GetVietnamTime()
                });
            }

            await _db.SaveChangesAsync();

            // 4. CẬP NHẬT LẠI TỔNG TIỀN HÓA ĐƠN
            await CapNhatLaiTongTienHoaDonAsync(maDatPhong);

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // Sử dụng InvalidOperationException thay vì Exception thường
            throw new InvalidOperationException($"Lỗi thêm dịch vụ: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    private async Task CapNhatLaiTongTienHoaDonAsync(string maDatPhong)
    {
        var hd = await _db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

        if (hd == null) return;

        // A. Tính tiền phòng: Chỉ tính những phòng ĐÃ CHECK-IN (có trong HoaDonChiTiet)
        var roomsInBill = await _db.HoaDonChiTiets
            .Join(_db.DatPhongChiTiets,
                hct => new { hct.MaDatPhong, hct.MaPhong },
                dpct => new { dpct.MaDatPhong, dpct.MaPhong },
                (hct, dpct) => dpct)
            .Where(x => x.MaDatPhong == maDatPhong)
            .ToListAsync();

        decimal tienPhongMoi =
            roomsInBill.Sum(ct => ct.DonGia * TinhToanService.TinhSoDem(ct.NgayNhan, ct.NgayTra));

        // B. Tính tiền dịch vụ: Tất cả dịch vụ đã gọi của cả đoàn
        decimal tongDvMoi = await _db.DichVuChiTiets
            .Where(d => d.MaHoaDon == hd.MaHoaDon)
            .SumAsync(d => (decimal?)d.SoLuong * d.DonGia) ?? 0;

        // C. Tách biến rõ ràng để tính toán giống như đã làm bên DatPhongService
        decimal vat = hd.Vat ?? DEFAULT_VAT_PERCENT;
        decimal giaTriKm = hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0;
        string loaiKm = hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "";
        decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        decimal tongDaThu =
            await _db.ThanhToans.Where(t => t.MaHoaDon == hd.MaHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;

        var res = TinhToanService.TinhToanToanBo(
            tienPhong: tienPhongMoi,
            tienDichVu: tongDvMoi,
            vatPercent: vat,
            giaTriKm: giaTriKm,
            loaiKm: loaiKm,
            tienCoc: tienCoc,
            tongDaThuLichSu: tongDaThu
        );

        hd.TienPhong = res.TienPhong;
        hd.TienDichVu = tongDvMoi;
        hd.TongThanhToan = res.TongThanhToan;

        await _db.SaveChangesAsync();
    }

    #endregion
}