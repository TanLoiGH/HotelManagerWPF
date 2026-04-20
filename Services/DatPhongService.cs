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

    // --- CÁC HÀM VỎ CALL XUỐNG CORE ---

    public async Task CheckInAsync(string maDatPhong, string maNhanVienLeTan)
    {
        // Truyền null cho maPhong để hệ thống tự động Check-in TẤT CẢ các phòng chưa nhận
        await CheckInThongNhatAsync(maDatPhong, null, maNhanVienLeTan);
    }

    public async Task CheckInPhongRiengLeAsync(string maDatPhong, string maPhong, string maNhanVienLeTan)
    {
        // Truyền đích danh mã phòng để Check-in lẻ
        await CheckInThongNhatAsync(maDatPhong, maPhong, maNhanVienLeTan);
    }

    // --- HÀM LÕI (CORE) XỬ LÝ CHECK-IN CHUNG ---

    private async Task CheckInThongNhatAsync(string maDatPhong, string? maPhong, string maNhanVienLeTan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var dp = await _db.DatPhongs
                .Include(d => d.DatPhongChiTiets).ThenInclude(c => c.MaPhongNavigation)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                ?? throw new Exception("Không tìm thấy đơn đặt phòng.");

            // 1. Xác định danh sách phòng cần Check-in
            var danhSachCanNhan = new List<DatPhongChiTiet>();

            if (string.IsNullOrWhiteSpace(maPhong))
            {
                // Nếu Check-in cả đoàn: Chọn ra những phòng CHƯA ĐƯỢC NHẬN (đang ở trạng thái Đã Đặt)
                danhSachCanNhan = dp.DatPhongChiTiets
                    .Where(c => c.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DaDat)
                    .ToList();
            }
            else
            {
                // Nếu Check-in lẻ: Chỉ lấy đúng 1 phòng
                var ct = dp.DatPhongChiTiets.FirstOrDefault(x => x.MaPhong == maPhong)
                    ?? throw new Exception($"Phòng {maPhong} không thuộc đơn {maDatPhong}.");

                if (ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                    throw new Exception($"Phòng {maPhong} đã được nhận trước đó rồi!");

                danhSachCanNhan.Add(ct);
            }

            if (!danhSachCanNhan.Any()) return; // Không có phòng nào cần xử lý thì thoát

            // 2. Đổi trạng thái phòng vật lý & Lưu giờ check-in thực tế
            foreach (var ct in danhSachCanNhan)
            {
                ct.MaPhongNavigation.MaTrangThaiPhong = PhongTrangThaiCodes.DangO;
                ct.NgayNhan = TimeHelper.GetVietnamTime();
                ct.MaNhanVien = maNhanVienLeTan;
            }

            // Đổi trạng thái đoàn sang Đang ở (nếu đang là Chờ nhận phòng)
            if (dp.TrangThai == DatPhongTrangThaiTexts.ChoNhanPhong)
            {
                dp.TrangThai = DatPhongTrangThaiTexts.DangO;
            }

            // ====================================================
            // 3. XỬ LÝ HÓA ĐƠN: NHẬN PHÒNG NÀO, BỎ VÀO BILL PHÒNG ĐÓ
            // ====================================================
            var hd = await _db.HoaDons
                .Include(h => h.HoaDonChiTiets)
                .FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

            bool laHoaDonMoi = false;

            if (hd == null)
            {
                string nextMaHd = MaHelper.Next("HD", await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync());
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
                        SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
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

            if (canTinhLaiBill && !laHoaDonMoi)
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
        var ct = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhong) ?? throw new Exception("Null");
        if (ngayTraMoi.Date <= ct.NgayTra.Date) throw new ArgumentException("Ngày mới không hợp lệ.");
        await EnsurePhongAvailableAsync(maPhong, ct.NgayTra, ngayTraMoi, maDatPhong);
        ct.NgayTra = ngayTraMoi;
        var hdct = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhong);
        if (hdct != null) hdct.SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ngayTraMoi);
        await _db.SaveChangesAsync();
        await CapNhatLaiTongTienHoaDonAsync(maDatPhong);
    }
    #endregion

    #region HỦY ĐẶT PHÒNG (HỢP NHẤT LOGIC)

    // --- CÁC HÀM VỎ (IMPLEMENT TỪ INTERFACE) ---
    // 1. DÀNH CHO HỦY TOÀN BỘ ĐOÀN
    public async Task HuyDatPhongAsync(string maDatPhong, string maNhanVien, string lyDo, decimal? tienHoanTra = null)
    {
        await HuyThongNhatAsync(maDatPhong, null, maNhanVien, lyDo, tienHoanTra);
    }

    // 2. DÀNH CHO HỦY LẺ 1 PHÒNG
    public async Task HuyPhongRiengLeAsync(string maDatPhong, string maPhong, string maNhanVien, string lyDo)
    {
        // Hủy lẻ thì mặc định không hoàn tiền (truyền 0), cấn trừ vào bill tổng
        await HuyThongNhatAsync(maDatPhong, maPhong, maNhanVien, lyDo, 0);
    }

    // --- HÀM LÕI (CORE) XỬ LÝ CHUNG ---

    private async Task HuyThongNhatAsync(string maDatPhong, string? maPhong, string maNhanVien, string lyDo, decimal? tienHoanTra)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var dp = await _db.DatPhongs
                .Include(d => d.DatPhongChiTiets).ThenInclude(c => c.MaPhongNavigation)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                ?? throw new Exception("Không tìm thấy đơn đặt phòng.");

            bool laHuyToanBo = string.IsNullOrWhiteSpace(maPhong) || dp.DatPhongChiTiets.Count <= 1;

            if (laHuyToanBo)
            {
                // ==========================================
                // KỊCH BẢN 1: HỦY TOÀN BỘ ĐƠN ĐẶT PHÒNG
                // ==========================================

                // Check 1 phát ăn ngay: Có phòng nào đã nhận chìa khóa chưa?
                if (dp.DatPhongChiTiets.Any(ct => ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO))
                {
                    throw new Exception("Đoàn này đang có phòng đã nhận khóa (Đang ở).\nKhông thể hủy toàn bộ đơn!\n\nVui lòng chọn các phòng khách không đến để 'Hủy riêng lẻ'.");
                }

                // [SỬA LỖI LOGIC TIỀN]: Nếu tienHoanTra có giá trị (kể cả số 0) thì lấy nó. Nếu là null thì lấy TienCoc mặc định.
                decimal soTienHoan = tienHoanTra ?? (dp.TienCoc ?? 0);
                if (soTienHoan > 0)
                    await GhiNhanChiPhiHoanCocAsync(maDatPhong, soTienHoan, maNhanVien);

                // Lưu vết lý do hủy
                dp.GhiChu = string.IsNullOrWhiteSpace(dp.GhiChu) ? $"Lý do hủy: {lyDo}" : $"{dp.GhiChu} | Lý do hủy: {lyDo}";

                // Trả phòng về Trống
                foreach (var ct in dp.DatPhongChiTiets)
                {
                    var p = ct.MaPhongNavigation;
                    if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;
                }

                dp.TrangThai = DatPhongTrangThaiTexts.DaHuy;

                var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);
                if (hd != null) hd.TrangThai = HoaDonTrangThaiTexts.DaHuy;
            }
            else
            {
                // ==========================================
                // KỊCH BẢN 2: HỦY RIÊNG LẺ 1 PHÒNG TRONG ĐOÀN
                // ==========================================

                var ct = dp.DatPhongChiTiets.FirstOrDefault(x => x.MaPhong == maPhong)
                         ?? throw new Exception($"Phòng {maPhong} không thuộc đơn {maDatPhong}.");

                // 👇 XÓA CÁI CHECK NgayNhan != null ĐI, THAY BẰNG CÁI NÀY 👇
                if (ct.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                {
                    throw new Exception($"Phòng {maPhong} đã Check-in và đang có khách ở.\nKhông thể Hủy phòng. Vui lòng thao tác 'Trả phòng'!");
                }

                var p = ct.MaPhongNavigation;
                if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;

                dp.GhiChu = string.IsNullOrWhiteSpace(dp.GhiChu) ? $"Hủy riêng P.{maPhong}: {lyDo}" : $"{dp.GhiChu} | Hủy P.{maPhong}: {lyDo}";

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
        if (p != null) { p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong; await _db.SaveChangesAsync(); }
    }

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
        var loaiCp = await _db.LoaiChiPhis.FirstOrDefaultAsync(l => l.MaLoaiCp == "LCP007" || l.TenLoaiCp.Contains("Hoàn tiền"));
        if (loaiCp == null)
        {
            loaiCp = new LoaiChiPhi { MaLoaiCp = "LCP007", TenLoaiCp = "Hoàn tiền hóa đơn" };
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
}