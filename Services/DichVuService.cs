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

    public DichVuService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<List<DichVu>> LayDanhSachAsync()
    {
        return await _db.DichVus
            .AsNoTracking()
            .OrderBy(d => d.TenDichVu)
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

    private async Task CapNhatLaiTongTienHoaDonAsync(string maDatPhong)
    {
        var hd = await _db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

        if (hd == null) return;

        // A. Tính tiền phòng: Chỉ tính những phòng ĐÃ CHECK-IN (có trong HoaDonChiTiet)
        var roomsInBill = await _db.HoaDonChiTiets
            .Join(_db.DatPhongChiTiets, hct => new { hct.MaDatPhong, hct.MaPhong }, dpct => new { dpct.MaDatPhong, dpct.MaPhong }, (hct, dpct) => dpct)
            .Where(x => x.MaDatPhong == maDatPhong)
            .ToListAsync();

        decimal tienPhong = roomsInBill.Sum(ct => ct.DonGia * TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra));

        // B. Tính tiền dịch vụ: Tất cả dịch vụ đã gọi của cả đoàn
        decimal tongDv = await _db.DichVuChiTiets
            .Where(d => d.MaHoaDon == hd.MaHoaDon)
            .SumAsync(d => (decimal?)d.SoLuong * d.DonGia) ?? 0;

        // C. Gọi Helper tính toán toàn bộ (VAT, Khuyến mãi, Cọc)
        var res = TinhToanHoaDonService.TinhToanToanBo(
            tienPhong,
            tongDv,
            hd.Vat ?? 10,
            hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0,
            hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "",
            hd.MaDatPhongNavigation?.TienCoc ?? 0,
            await _db.ThanhToans.Where(t => t.MaHoaDon == hd.MaHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0
        );

        hd.TienPhong = res.TienPhong;
        hd.TienDichVu = tongDv;
        hd.TongThanhToan = res.TongThanhToan;

        await _db.SaveChangesAsync();
    }


    public async Task<bool> XoaHoacTatAsync(string maDichVu)
    {
        var dv = await _db.DichVus.FindAsync(maDichVu);
        if (dv == null) return false;

        bool coGiaoDich = await _db.DichVuChiTiets.AnyAsync(d => d.MaDichVu == dv.MaDichVu);
        if (coGiaoDich)
        {
            dv.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.DichVus.Remove(dv);
        await _db.SaveChangesAsync();
        return false;
    }

    public async Task<List<DichVu>> GetDichVusAsync()
    {
        return await _db.DichVus.ToListAsync();
    }

    public async Task<List<DichVuChiTiet>> GetDichVuChiTietsForHoaDonAsync(string maHoaDon)
    {
        return await _db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.MaHoaDon == maHoaDon)
            .ToListAsync();
    }

    public async Task<List<DichVu>> GetAllDichVuAsync()
    {
        return await _db.DichVus.Where(d => d.IsActive == true).ToListAsync();
    }

    public async Task UpsertDichVuAsync(string maHoaDon, string maDatPhong, string maPhong, string maDichVu, int soLuong)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. TÌM HÓA ĐƠN CHUẨN (Ưu tiên tìm theo maDatPhong vì maHoaDon từ UI có thể rỗng)
            var hd = await _db.HoaDons
                .FirstOrDefaultAsync(h => (h.MaHoaDon == maHoaDon || h.MaDatPhong == maDatPhong)
                                     && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

            // 2. NẾU CHƯA CÓ HÓA ĐƠN (Trường hợp khách chưa check-in phòng nào) -> TỰ TẠO VỎ HÓA ĐƠN
            if (hd == null)
            {
                var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync();
                hd = new HoaDon
                {
                    MaHoaDon = MaHelper.Next("HD", lastMa),
                    MaDatPhong = maDatPhong,
                    NgayLap = TimeHelper.GetVietnamTime(),
                    TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan,
                    TienPhong = 0,
                    TienDichVu = 0,
                    Vat = 10 // Mặc định 10%
                };
                _db.HoaDons.Add(hd);
                await _db.SaveChangesAsync();
            }

            string maHdThucTe = hd.MaHoaDon;

            // 3. THÊM HOẶC CẬP NHẬT DỊCH VỤ CHI TIẾT
            var existing = await _db.DichVuChiTiets.FirstOrDefaultAsync(d => d.MaHoaDon == maHdThucTe && d.MaDichVu == maDichVu && d.MaPhong == maPhong);

            if (existing != null)
            {
                existing.SoLuong += soLuong;
            }
            else
            {
                var dv = await _db.DichVus.FindAsync(maDichVu) ?? throw new Exception("Dịch vụ không tồn tại trong hệ thống.");
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

            // 4. CẬP NHẬT LẠI TỔNG TIỀN HÓA ĐƠN (Đồng bộ logic với DatPhongService)
            await CapNhatLaiTongTienHoaDonAsync(maDatPhong);

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            // DEBUG VỚI INNER EXCEPTION: Giúp bạn thấy lỗi thực sự từ SQL (như lỗi Khóa ngoại, Check constraint)
            var mess = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new Exception($"Lỗi thêm dịch vụ: {mess}", ex);
        }
    }
}
