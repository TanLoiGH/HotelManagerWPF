using System.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DatPhongService : IDatPhongService
{
    private readonly QuanLyKhachSanContext _db;
    public DatPhongService(QuanLyKhachSanContext db) => _db = db;

    #region HÓA ĐƠN NỘI BỘ
    private async Task TaoHoaDonNeuChuaCoAsync(string maDatPhong, string maNhanVien)
    {
        bool hdExist = await _db.HoaDons.AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);
        if (hdExist) return;

        var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
        if (!chiTiets.Any()) throw new InvalidOperationException("Không có phòng trong đoàn.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;
        decimal tienPhong = chiTiets.Sum(ct => ct.DonGia * TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra));
        decimal vatPercent = SystemSettingsService.Load().VatPercent;

        var res = TinhToanHoaDonService.TinhToanToanBo(tienPhong, 0, vatPercent, 0, "", tienCoc, 0);

        var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync();
        var hd = new HoaDon
        {
            MaHoaDon = MaHelper.Next("HD", lastMa),
            MaDatPhong = maDatPhong,
            MaNhanVien = maNhanVien,
            NgayLap = TimeHelper.GetVietnamTime(),
            TienPhong = res.TienPhong,
            TienDichVu = 0,
            Vat = vatPercent,
            TongThanhToan = res.TongThanhToan,
            TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan
        };

        _db.HoaDons.Add(hd);
        foreach (var ct in chiTiets)
        {
            _db.HoaDonChiTiets.Add(new HoaDonChiTiet
            {
                MaHoaDon = hd.MaHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
            });
        }
        await _db.SaveChangesAsync();
    }
    #endregion

    #region ĐẶT PHÒNG & CHECK-IN
    public async Task<DatPhong> TaoDatPhongAsync(string maKhachHang, List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms, string maNhanVien = null, decimal tienCoc = 0, int soNguoi = 1)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var lastMa = await _db.DatPhongs.OrderByDescending(d => d.MaDatPhong).Select(d => d.MaDatPhong).FirstOrDefaultAsync();
            var dp = new DatPhong
            {
                MaDatPhong = MaHelper.Next("DP", lastMa),
                MaKhachHang = maKhachHang,
                MaNhanVien = maNhanVien,
                NgayDat = TimeHelper.GetVietnamTime(),
                TienCoc = tienCoc,
                TrangThai = DatPhongTrangThaiTexts.ChoNhanPhong,
                NgayNhanDuKien = rooms.Min(r => r.NgayNhan),
                NgayTraDuKien = rooms.Max(r => r.NgayTra)
            };
            _db.DatPhongs.Add(dp);

            foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
            {
                await EnsurePhongAvailableAsync(maPhong, ngayNhan, ngayTra);
                var phong = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation).FirstAsync(p => p.MaPhong == maPhong);
                _db.DatPhongChiTiets.Add(new DatPhongChiTiet
                {
                    MaDatPhong = dp.MaDatPhong,
                    MaPhong = maPhong,
                    NgayNhan = ngayNhan,
                    NgayTra = ngayTra,
                    DonGia = phong.MaLoaiPhongNavigation.GiaPhong,
                    MaNhanVien = maNhanVien
                });
                phong.MaTrangThaiPhong = PhongTrangThaiCodes.DaDat;
            }
            await _db.SaveChangesAsync(); await tx.CommitAsync(); return dp;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task CheckInAsync(string maDatPhong, string maNhanVienLeTan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var dp = await _db.DatPhongs.Include(d => d.DatPhongChiTiets).FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong) ?? throw new Exception("Error");
            dp.TrangThai = DatPhongTrangThaiTexts.DangO;
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                if (phong != null) phong.MaTrangThaiPhong = PhongTrangThaiCodes.DangO;
                ct.NgayNhan = TimeHelper.GetVietnamTime();
            }
            await _db.SaveChangesAsync();
            await TaoHoaDonNeuChuaCoAsync(maDatPhong, maNhanVienLeTan);
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    #endregion

    #region ĐỔI PHÒNG & GIA HẠN
    public async Task DoiPhongAsync(string maDatPhong, string maPhongCu, string maPhongMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var ctCu = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhongCu) ?? throw new Exception("Null");
            DateTime now = TimeHelper.GetVietnamTime();
            var phongCu = await _db.Phongs.FindAsync(maPhongCu);
            var phongMoi = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation).FirstAsync(p => p.MaPhong == maPhongMoi);
            var hdctCu = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhongCu);

            if (now.Date <= ctCu.NgayNhan.Date)
            {
                await EnsurePhongAvailableAsync(maPhongMoi, ctCu.NgayNhan, ctCu.NgayTra, maDatPhong);
                phongMoi.MaTrangThaiPhong = phongCu?.MaTrangThaiPhong ?? PhongTrangThaiCodes.DangO;
                if (phongCu != null) phongCu.MaTrangThaiPhong = (phongCu.MaTrangThaiPhong == PhongTrangThaiCodes.DangO) ? PhongTrangThaiCodes.DonDep : PhongTrangThaiCodes.Trong;

                var dvs = await _db.DichVuChiTiets.Where(dv => dv.MaDatPhong == maDatPhong && dv.MaPhong == maPhongCu).ToListAsync();
                foreach (var dv in dvs) dv.MaPhong = maPhongMoi;
                if (hdctCu != null) hdctCu.MaPhong = maPhongMoi;

                _db.DatPhongChiTiets.Add(new DatPhongChiTiet
                {
                    MaDatPhong = maDatPhong,
                    MaPhong = maPhongMoi,
                    NgayNhan = ctCu.NgayNhan,
                    NgayTra = ctCu.NgayTra,
                    DonGia = phongMoi.MaLoaiPhongNavigation.GiaPhong,
                    MaNhanVien = ctCu.MaNhanVien
                });
                _db.DatPhongChiTiets.Remove(ctCu);
            }
            else
            {
                await EnsurePhongAvailableAsync(maPhongMoi, now, ctCu.NgayTra, maDatPhong);
                DateTime oldNgayTra = ctCu.NgayTra;
                ctCu.NgayTra = now;
                if (phongCu != null) phongCu.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
                phongMoi.MaTrangThaiPhong = PhongTrangThaiCodes.DangO;

                _db.DatPhongChiTiets.Add(new DatPhongChiTiet
                {
                    MaDatPhong = maDatPhong,
                    MaPhong = maPhongMoi,
                    NgayNhan = now,
                    NgayTra = oldNgayTra,
                    DonGia = phongMoi.MaLoaiPhongNavigation.GiaPhong,
                    MaNhanVien = ctCu.MaNhanVien
                });

                if (hdctCu != null)
                {
                    hdctCu.SoDem = TinhToanHoaDonService.TinhSoDem(ctCu.NgayNhan, now);
                    _db.HoaDonChiTiets.Add(new HoaDonChiTiet
                    {
                        MaHoaDon = hdctCu.MaHoaDon,
                        MaDatPhong = maDatPhong,
                        MaPhong = maPhongMoi,
                        SoDem = TinhToanHoaDonService.TinhSoDem(now, oldNgayTra)
                    });
                }
            }
            await _db.SaveChangesAsync();
            await CapNhatLaiTongTienHoaDonAsync(maDatPhong); // Đã sửa lỗi dấu cách
            await tx.CommitAsync();
            MessageBox.Show("Đổi phòng thành công!");
        }
        catch (Exception ex) { await tx.RollbackAsync(); throw; }
    }

    public async Task GiaHanAsync(string maDatPhong, string maPhong, DateTime ngayTraMoi)
    {
        var ct = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhong) ?? throw new Exception("Null");
        if (ngayTraMoi.Date <= ct.NgayTra.Date) throw new ArgumentException("Ngày mới không hợp lệ.");
        await EnsurePhongAvailableAsync(maPhong, ct.NgayTra, ngayTraMoi, maDatPhong);
        ct.NgayTra = ngayTraMoi;
        var hdct = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhong);
        if (hdct != null) hdct.SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ngayTraMoi);
        await _db.SaveChangesAsync();
        await CapNhatLaiTongTienHoaDonAsync(maDatPhong); // Đã sửa lỗi dấu cách
    }
    #endregion

    #region HỦY PHÒNG (IMPLEMENT CẢ 2 OVERLOADS)
    public async Task HuyDatPhongAsync(string maDatPhong, string maNhanVien)
    {
        // Gọi sang bản chi tiết với lý do mặc định
        await HuyDatPhongAsync(maDatPhong, "Nhân viên hủy đơn", 0);
    }

    public async Task HuyDatPhongAsync(string maDatPhong, string lyDo, decimal tienHoanTra = 0)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var dp = await _db.DatPhongs.Include(d => d.DatPhongChiTiets).FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong) ?? throw new Exception("Null");

            // Log chi phí nếu có tiền trả lại (Hoặc mất cọc)
            if (dp.TienCoc > 0) await GhiNhanChiPhiHoanCocAsync(maDatPhong, dp.TienCoc ?? 0, "Hệ thống");

            foreach (var ct in dp.DatPhongChiTiets)
            {
                var p = await _db.Phongs.FindAsync(ct.MaPhong);
                if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;
            }
            dp.TrangThai = DatPhongTrangThaiTexts.DaHuy;
            await _db.SaveChangesAsync(); await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task HoanThanhDonDepAsync(string maPhong)
    {
        var p = await _db.Phongs.FindAsync(maPhong);
        if (p != null) { p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong; await _db.SaveChangesAsync(); }
    }
    #endregion

    #region PRIVATE HELPERS
    private async Task EnsurePhongAvailableAsync(string maPhong, DateTime ngayNhan, DateTime ngayTra, string? excludeMaDatPhong = null)
    {
        bool conflict = await _db.DatPhongChiTiets.Where(c => c.MaPhong == maPhong && (excludeMaDatPhong == null || c.MaDatPhong != excludeMaDatPhong))
            .Join(_db.DatPhongs, c => c.MaDatPhong, d => d.MaDatPhong, (c, d) => new { c, d })
            .AnyAsync(x => x.d.TrangThai != DatPhongTrangThaiTexts.DaTraPhong && x.d.TrangThai != DatPhongTrangThaiTexts.DaHuy && x.c.NgayNhan < ngayTra && x.c.NgayTra > ngayNhan);
        if (conflict) throw new InvalidOperationException($"Phòng {maPhong} bị trùng lịch.");
    }

    private async Task CapNhatLaiTongTienHoaDonAsync(string maDatPhong)
    {
        var hd = await _db.HoaDons.Include(h => h.MaDatPhongNavigation).Include(h => h.MaKhuyenMaiNavigation).FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);
        if (hd == null) return;
        var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
        decimal tp = chiTiets.Sum(ct => ct.DonGia * TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra));
        var res = TinhToanHoaDonService.TinhToanToanBo(tp, hd.TienDichVu ?? 0, hd.Vat ?? 0, hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0, hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "", hd.MaDatPhongNavigation?.TienCoc ?? 0, await _db.ThanhToans.Where(t => t.MaHoaDon == hd.MaHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0);
        hd.TienPhong = res.TienPhong; hd.TongThanhToan = res.TongThanhToan;
        await _db.SaveChangesAsync();
    }

    private async Task GhiNhanChiPhiHoanCocAsync(string maDatPhong, decimal soTien, string maNhanVien)
    {
        var lastCp = await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync();
        _db.ChiPhis.Add(new ChiPhi { MaChiPhi = MaHelper.Next("CP", lastCp), MaLoaiCp = "LCP007", MaNhanVien = maNhanVien, TenChiPhi = $"Hoàn cọc đơn {maDatPhong}", SoTien = soTien, NgayChiPhi = TimeHelper.GetVietnamTime() });
    }



    #endregion
}