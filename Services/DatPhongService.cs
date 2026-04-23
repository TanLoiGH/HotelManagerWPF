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
        bool hdExist =
            await _db.HoaDons.AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);
        if (hdExist) return;

        var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
        if (!chiTiets.Any()) throw new InvalidOperationException($"Không có phòng nào trong đoàn {maDatPhong}.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;
        decimal tienPhong = chiTiets.Sum(ct => ct.DonGia * TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ct.NgayTra));
        decimal vatPercent = CaiDatService.Load().VatPercent;

        var res = TinhToanService.TinhToanToanBo(tienPhong, 0, vatPercent, 0, "", tienCoc, 0);

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
                SoDem = TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ct.NgayTra)
            });
        }

        await _db.SaveChangesAsync();
    }

    #endregion

    #region ĐẶT PHÒNG & CHECK-IN

    public async Task<DatPhong> TaoDatPhongAsync(string maKhachHang,
        List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms, string maNhanVien = null,
        decimal tienCoc = 0, int soNguoi = 1)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var lastMa = await _db.DatPhongs.OrderByDescending(d => d.MaDatPhong).Select(d => d.MaDatPhong)
                .FirstOrDefaultAsync();
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

                // Senior Refactor: Thay FirstAsync bằng FirstOrDefaultAsync để kiểm soát lỗi tốt hơn
                var phong = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation)
                                .FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                            ?? throw new KeyNotFoundException(
                                $"Không tìm thấy dữ liệu phòng {maPhong} trong hệ thống.");

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

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return dp;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            throw new Exception($"Lỗi tạo đặt phòng: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    public async Task CheckInAsync(string maDatPhong, string maNhanVienLeTan)
    {
        await CheckInThongNhatAsync(maDatPhong, null, maNhanVienLeTan);
    }

    public async Task CheckInPhongRiengLeAsync(string maDatPhong, string maPhong, string maNhanVienLeTan)
    {
        await CheckInThongNhatAsync(maDatPhong, maPhong, maNhanVienLeTan);
    }

    private async Task CheckInThongNhatAsync(string maDatPhong, string? maPhong, string maNhanVienLeTan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var dp = await _db.DatPhongs
                         .Include(d => d.DatPhongChiTiets).ThenInclude(c => c.MaPhongNavigation)
                         .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                     ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt phòng.");

            var danhSachCanNhan = new List<DatPhongChiTiet>();

            if (string.IsNullOrWhiteSpace(maPhong))
            {
                danhSachCanNhan = dp.DatPhongChiTiets
                    .Where(c => c.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DaDat)
                    .ToList();
            }
            else
            {
                var ct = dp.DatPhongChiTiets.FirstOrDefault(x => x.MaPhong == maPhong)
                         ?? throw new InvalidOperationException($"Phòng {maPhong} không thuộc đơn {maDatPhong}.");

                if (ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                    throw new InvalidOperationException($"Phòng {maPhong} đã được nhận trước đó rồi!");

                danhSachCanNhan.Add(ct);
            }

            if (!danhSachCanNhan.Any()) return;

            foreach (var ct in danhSachCanNhan)
            {
                ct.MaPhongNavigation.MaTrangThaiPhong = PhongTrangThaiCodes.DangO;
                ct.NgayNhan = TimeHelper.GetVietnamTime();
                ct.MaNhanVien = maNhanVienLeTan;
            }

            if (dp.TrangThai == DatPhongTrangThaiTexts.ChoNhanPhong)
            {
                dp.TrangThai = DatPhongTrangThaiTexts.DangO;
            }

            var hd = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

            bool laHoaDonMoi = false;

            if (hd == null)
            {
                string nextMaHd = MaHelper.Next("HD",
                    await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync());
                hd = new HoaDon
                {
                    MaHoaDon = nextMaHd,
                    MaDatPhong = maDatPhong,
                    MaNhanVien = maNhanVienLeTan,
                    NgayLap = TimeHelper.GetVietnamTime(),
                    TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan,
                    HoaDonChiTiets = new List<HoaDonChiTiet>()
                };
                _db.HoaDons.Add(hd);
                laHoaDonMoi = true;
            }

            bool canTinhLaiBill = false;
            foreach (var ct in danhSachCanNhan)
            {
                if (!hd.HoaDonChiTiets.Any(x => x.MaPhong == ct.MaPhong))
                {
                    var hdct = new HoaDonChiTiet
                    {
                        MaDatPhong = maDatPhong,
                        MaPhong = ct.MaPhong,
                        SoDem = TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ct.NgayTra)
                    };

                    if (laHoaDonMoi) hd.HoaDonChiTiets.Add(hdct);
                    else
                    {
                        hdct.MaHoaDon = hd.MaHoaDon;
                        _db.HoaDonChiTiets.Add(hdct);
                    }

                    canTinhLaiBill = true;
                }
            }

            await _db.SaveChangesAsync();

            if (canTinhLaiBill || laHoaDonMoi)
            {
                await CapNhatLaiTongTienHoaDonAsync(maDatPhong);
            }

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            throw new Exception($"Lỗi Check-in: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    #endregion

    #region ĐỔI PHÒNG & GIA HẠN

    public async Task DoiPhongAsync(string maDatPhong, string maPhongCu, string maPhongMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            // Senior Refactor: Loại bỏ throw new Exception("Null") vô nghĩa
            var ctCu = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x =>
                           x.MaDatPhong == maDatPhong && x.MaPhong == maPhongCu)
                       ?? throw new KeyNotFoundException($"Không tìm thấy lịch sử đặt phòng của phòng {maPhongCu}.");

            DateTime now = TimeHelper.GetVietnamTime();
            var phongCu = await _db.Phongs.FindAsync(maPhongCu);
            var phongMoi = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation)
                               .FirstOrDefaultAsync(p => p.MaPhong == maPhongMoi)
                           ?? throw new KeyNotFoundException($"Không tìm thấy dữ liệu phòng mới {maPhongMoi}.");

            var hdctCu =
                await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhongCu);

            if (now.Date <= ctCu.NgayNhan.Date)
            {
                await EnsurePhongAvailableAsync(maPhongMoi, ctCu.NgayNhan, ctCu.NgayTra, maDatPhong);
                phongMoi.MaTrangThaiPhong = phongCu?.MaTrangThaiPhong ?? PhongTrangThaiCodes.DangO;
                if (phongCu != null)
                    phongCu.MaTrangThaiPhong = (phongCu.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                        ? PhongTrangThaiCodes.DonDep
                        : PhongTrangThaiCodes.Trong;

                var dvs = await _db.DichVuChiTiets.Where(dv => dv.MaDatPhong == maDatPhong && dv.MaPhong == maPhongCu)
                    .ToListAsync();
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
                    hdctCu.SoDem = TinhToanService.ThoiGianLuuTru(ctCu.NgayNhan, now);
                    _db.HoaDonChiTiets.Add(new HoaDonChiTiet
                    {
                        MaHoaDon = hdctCu.MaHoaDon,
                        MaDatPhong = maDatPhong,
                        MaPhong = maPhongMoi,
                        SoDem = TinhToanService.ThoiGianLuuTru(now, oldNgayTra)
                    });
                }
            }

            await _db.SaveChangesAsync();
            await CapNhatLaiTongTienHoaDonAsync(maDatPhong);
            await tx.CommitAsync();
            MessageBox.Show("Đổi phòng thành công!");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            throw new Exception($"Lỗi đổi phòng: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    public async Task GiaHanAsync(string maDatPhong, string maPhong, DateTime ngayTraMoi)
    {
        var ct = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhong)
                 ?? throw new KeyNotFoundException($"Không tìm thấy phòng {maPhong} trong đơn {maDatPhong}.");

        if (ngayTraMoi.Date <= ct.NgayTra.Date) throw new ArgumentException("Ngày mới không hợp lệ.");

        await EnsurePhongAvailableAsync(maPhong, ct.NgayTra, ngayTraMoi, maDatPhong);
        ct.NgayTra = ngayTraMoi;

        var hdct = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h =>
            h.MaDatPhong == maDatPhong && h.MaPhong == maPhong);
        if (hdct != null) hdct.SoDem = TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ngayTraMoi);

        await _db.SaveChangesAsync();
        await CapNhatLaiTongTienHoaDonAsync(maDatPhong);
    }

    #endregion

    #region HỦY ĐẶT PHÒNG (HỢP NHẤT LOGIC)

    public async Task HuyDatPhongAsync(string maDatPhong, string maNhanVien, string lyDo, decimal? tienHoanTra = null)
    {
        await HuyThongNhatAsync(maDatPhong, null, maNhanVien, lyDo, tienHoanTra);
    }

    public async Task HuyPhongRiengLeAsync(string maDatPhong, string maPhong, string maNhanVien, string lyDo)
    {
        await HuyThongNhatAsync(maDatPhong, maPhong, maNhanVien, lyDo, 0);
    }

    private async Task HuyThongNhatAsync(string maDatPhong, string? maPhong, string maNhanVien, string lyDo,
        decimal? tienHoanTra)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var dp = await _db.DatPhongs
                         .Include(d => d.DatPhongChiTiets).ThenInclude(c => c.MaPhongNavigation)
                         .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                     ?? throw new KeyNotFoundException("Không tìm thấy đơn đặt phòng.");

            bool laHuyToanBo = string.IsNullOrWhiteSpace(maPhong) || dp.DatPhongChiTiets.Count <= 1;

            if (laHuyToanBo)
            {
                if (dp.DatPhongChiTiets.Any(ct => ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO))
                {
                    throw new InvalidOperationException(
                        "Đoàn này đang có phòng đã nhận khóa (Đang ở).\nKhông thể hủy toàn bộ đơn!\n\nVui lòng chọn các phòng khách không đến để 'Hủy riêng lẻ'.");
                }

                decimal soTienHoan = tienHoanTra ?? (dp.TienCoc ?? 0);
                if (soTienHoan > 0)
                    await GhiNhanChiPhiHoanCocAsync(maDatPhong, soTienHoan, maNhanVien);

                dp.GhiChu = string.IsNullOrWhiteSpace(dp.GhiChu)
                    ? $"Lý do hủy: {lyDo}"
                    : $"{dp.GhiChu} | Lý do hủy: {lyDo}";

                foreach (var ct in dp.DatPhongChiTiets)
                {
                    var p = ct.MaPhongNavigation;
                    if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;
                }

                dp.TrangThai = DatPhongTrangThaiTexts.DaHuy;

                var hd = await _db.HoaDons.FirstOrDefaultAsync(h =>
                    h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);
                if (hd != null) hd.TrangThai = HoaDonTrangThaiTexts.DaHuy;
            }
            else
            {
                var ct = dp.DatPhongChiTiets.FirstOrDefault(x => x.MaPhong == maPhong)
                         ?? throw new KeyNotFoundException($"Phòng {maPhong} không thuộc đơn {maDatPhong}.");

                if (ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                {
                    throw new InvalidOperationException(
                        $"Phòng {maPhong} đã Check-in và đang có khách ở.\nKhông thể Hủy phòng. Vui lòng thao tác 'Trả phòng'!");
                }

                var p = ct.MaPhongNavigation;
                if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;

                dp.GhiChu = string.IsNullOrWhiteSpace(dp.GhiChu)
                    ? $"Hủy riêng P.{maPhong}: {lyDo}"
                    : $"{dp.GhiChu} | Hủy P.{maPhong}: {lyDo}";

                _db.DatPhongChiTiets.Remove(ct);

                var hd = await _db.HoaDons.Include(h => h.HoaDonChiTiets)
                    .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

                if (hd != null)
                {
                    var hdct = hd.HoaDonChiTiets.FirstOrDefault(h => h.MaPhong == maPhong);
                    if (hdct != null) _db.HoaDonChiTiets.Remove(hdct);
                }
            }

            await _db.SaveChangesAsync();

            if (!laHuyToanBo)
            {
                await CapNhatLaiTongTienHoaDonAsync(maDatPhong);
            }

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            throw new Exception($"Lỗi hủy đặt phòng: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    #endregion

    public async Task HoanThanhDonDepAsync(string maPhong)
    {
        var p = await _db.Phongs.FindAsync(maPhong);
        if (p != null)
        {
            p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;
            await _db.SaveChangesAsync();
        }
    }

    #region PRIVATE HELPERS

    private async Task EnsurePhongAvailableAsync(string maPhong, DateTime ngayNhan, DateTime ngayTra,
        string? excludeMaDatPhong = null)
    {
        bool conflict = await _db.DatPhongChiTiets.Where(c =>
                c.MaPhong == maPhong && (excludeMaDatPhong == null || c.MaDatPhong != excludeMaDatPhong))
            .Join(_db.DatPhongs, c => c.MaDatPhong, d => d.MaDatPhong, (c, d) => new { c, d })
            .AnyAsync(x =>
                x.d.TrangThai != DatPhongTrangThaiTexts.DaTraPhong && x.d.TrangThai != DatPhongTrangThaiTexts.DaHuy &&
                x.c.NgayNhan < ngayTra && x.c.NgayTra > ngayNhan);

        if (conflict) throw new InvalidOperationException($"Phòng {maPhong} bị trùng lịch.");
    }

    private async Task CapNhatLaiTongTienHoaDonAsync(string maDatPhong)
    {
        var hd = await _db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

        if (hd == null) return;

        var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
        decimal tienPhongMoi = chiTiets.Sum(ct => ct.DonGia * TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ct.NgayTra));

        // Senior Refactor: Tách các biến ra để code dễ đọc và dễ debug hơn, thay vì gộp chung vào 1 dòng cực dài
        decimal tienDichVu = hd.TienDichVu ?? 0;
        decimal vat = hd.Vat ?? 0;
        decimal giaTriKm = hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0;
        string loaiKm = hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "";
        decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
        decimal tongDaThu =
            await _db.ThanhToans.Where(t => t.MaHoaDon == hd.MaHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;

        var res = TinhToanService.TinhToanToanBo(tienPhongMoi, tienDichVu, vat, giaTriKm, loaiKm, tienCoc,
            tongDaThu);

        hd.TienPhong = res.TienPhong;
        hd.TongThanhToan = res.TongThanhToan;

        await _db.SaveChangesAsync();
    }

    private async Task GhiNhanChiPhiHoanCocAsync(string maDatPhong, decimal soTien, string maNhanVien)
    {
        const string MA_LOAI_CP_HOAN_TIEN = "LCP007";

        var loaiCp = await _db.LoaiChiPhis.FirstOrDefaultAsync(l =>
            l.MaLoaiCp == MA_LOAI_CP_HOAN_TIEN || l.TenLoaiCp.Contains("Hoàn tiền"));
        if (loaiCp == null)
        {
            loaiCp = new LoaiChiPhi { MaLoaiCp = MA_LOAI_CP_HOAN_TIEN, TenLoaiCp = "Hoàn tiền hóa đơn" };
            _db.LoaiChiPhis.Add(loaiCp);
            await _db.SaveChangesAsync();
        }

        var lastCp = await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync();
        _db.ChiPhis.Add(new ChiPhi
        {
            MaChiPhi = MaHelper.Next("CP", lastCp),
            MaLoaiCp = loaiCp.MaLoaiCp,
            MaNhanVien = maNhanVien,
            TenChiPhi = $"Hoàn cọc đơn {maDatPhong}",
            SoTien = soTien,
            NgayChiPhi = TimeHelper.GetVietnamTime()
        });
    }

    #endregion

    #region NGHIỆP VỤ THANH TOÁN & TRẢ PHÒNG (CHECK-OUT)

    // Lấy thông tin đơn đặt phòng đang ở dựa vào mã phòng
    public async Task<DatPhongChiTiet?> LayDatPhongDangOTheoPhongAsync(string maPhong)
    {
        return await _db.DatPhongChiTiets
            .Include(c => c.MaDatPhongNavigation)
            .ThenInclude(dp => dp.MaKhachHangNavigation)
            .FirstOrDefaultAsync(c => c.MaPhong == maPhong
                                      && c.MaDatPhongNavigation != null
                                      && c.MaDatPhongNavigation.TrangThai == DatPhongTrangThaiTexts.DangO);
    }

    // Lấy danh sách dịch vụ (nước, mì tôm, giặt ủi...) mà khách đã sử dụng
    public async Task<List<DichVuChiTiet>> LayDichVuTheoDatPhongAsync(string maDatPhong)
    {
        return await _db.DichVuChiTiets
            .AsNoTracking()
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.MaDatPhong == maDatPhong)
            .ToListAsync();
    }

    // Thực thi giao dịch Trả phòng
    public async Task ThanhToanVaTraPhongAsync(string maDatPhong, string maPhong, string maNhanVien,
        decimal soTienThuThem)
    {
        // 1. Cập nhật trạng thái Đơn đặt phòng thành Đã thanh toán / Hoàn tất
        var datPhong = await _db.DatPhongs.FindAsync(maDatPhong);
        if (datPhong == null) throw new InvalidOperationException("Không tìm thấy dữ liệu đặt phòng.");

        datPhong.TrangThai = HoaDonTrangThaiTexts.DaThanhToan; // Hoặc trạng thái tương đương trong DB của bạn

        // 2. Chuyển trạng thái phòng từ "Đang ở" sang "Chờ dọn dẹp" (PTT03)
        var phong = await _db.Phongs.FindAsync(maPhong);
        if (phong != null)
        {
            phong.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
        }

        // 3. TẠO HÓA ĐƠN (Nếu database của bạn yêu cầu lưu trữ Hóa đơn tại bước này)
        // var maHoaDon = MaHelper.Next("HD", ...);
        // _db.HoaDons.Add(new HoaDon { ... });

        await _db.SaveChangesAsync();
    }

    #endregion
}