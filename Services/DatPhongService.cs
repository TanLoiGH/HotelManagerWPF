using System.Data;
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

    #region LOGIC TẠO HÓA ĐƠN
    private async Task TaoHoaDonNeuChuaCoAsync(string maDatPhong, string maNhanVien)
    {
        // 1. Kiểm tra hóa đơn đã tồn tại cho mã đặt phòng này chưa
        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

        if (hdExist) return;

        // 2. Lấy toàn bộ danh sách phòng trong đoàn
        var chiTiets = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == maDatPhong)
            .ToListAsync();

        if (chiTiets.Count == 0)
            throw new InvalidOperationException("Không có phòng nào trong bản đăng ký đặt phòng.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;

        // 3. Tính toán tổng tiền phòng cho cả đoàn
        decimal tienPhong = 0;
        foreach (var ct in chiTiets)
        {
            int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra);
            tienPhong += ct.DonGia * soDem;
        }

        // 4. Sinh mã hóa đơn mới
        var lastMa = await _db.HoaDons
            .OrderByDescending(h => h.MaHoaDon)
            .Select(h => h.MaHoaDon)
            .FirstOrDefaultAsync();

        var newMaHd = MaHelper.Next("HD", lastMa);
        decimal vatPercent = SystemSettingsService.Load().VatPercent;

        // 5. Tạo vỏ bọc hóa đơn tổng
        var hd = new HoaDon
        {
            MaHoaDon = newMaHd,
            MaDatPhong = maDatPhong,
            MaNhanVien = maNhanVien,
            NgayLap = TimeHelper.GetVietnamTime(),
            TienPhong = tienPhong,
            TienDichVu = 0,
            Vat = vatPercent,
            TongThanhToan = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, 0, vatPercent, tienCoc, 0),
            TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan
        };

        _db.HoaDons.Add(hd);

        // 6. Thêm chi tiết hóa đơn cho từng phòng trong đoàn
        foreach (var ct in chiTiets)
        {
            _db.HoaDonChiTiets.Add(new HoaDonChiTiet
            {
                MaHoaDon = hd.MaHoaDon,
                MaDatPhong = ct.MaDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
            });
        }

        await _db.SaveChangesAsync();
    }
    #endregion

    #region LOGIC ĐẶT PHÒNG (MULTI-ROOM)
    private async Task EnsurePhongAvailableAsync(string maPhong, DateTime ngayNhan, DateTime ngayTra, string? excludeMaDatPhong = null)
    {
        bool conflict = await _db.DatPhongChiTiets
            .Where(c => c.MaPhong == maPhong && (excludeMaDatPhong == null || c.MaDatPhong != excludeMaDatPhong))
            .Join(_db.DatPhongs,
                  c => c.MaDatPhong,
                  d => d.MaDatPhong,
                  (c, d) => new { c, d })
            .AnyAsync(x =>
                x.d.TrangThai != DatPhongTrangThaiTexts.DaTraPhong &&
                x.d.TrangThai != DatPhongTrangThaiTexts.DaHuy &&
                x.c.NgayNhan < ngayTra &&
                x.c.NgayTra > ngayNhan);

        if (conflict)
            throw new InvalidOperationException($"Phòng {maPhong} đã có lịch đặt trùng trong khoảng thời gian này.");
    }

    public async Task<DatPhong> TaoDatPhongAsync(
        string maKhachHang,
        List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms,
        string maNhanVien = null,
        decimal tienCoc = 0,
        int soNguoi = 1)
    {
        // Kiểm tra hợp lệ ngày tháng của tất cả các phòng
        foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
        {
            if (ngayNhan.Date < DateTime.Today)
                throw new InvalidOperationException($"Ngày nhận phòng {maPhong} không được ở quá khứ.");
            if (ngayTra.Date < ngayNhan.Date)
                throw new InvalidOperationException($"Ngày trả phòng {maPhong} phải sau hoặc bằng ngày nhận.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 
        try
        {
            int tongSucChua = 0;
            foreach (var (maPhong, _, _) in rooms)
            {
                var phong = await _db.Phongs
                    .Include(p => p.MaLoaiPhongNavigation)
                    .FirstOrDefaultAsync(p => p.MaPhong == maPhong)
                    ?? throw new Exception($"Không tìm thấy phòng {maPhong}");

                tongSucChua += phong.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0;
            }

            if (soNguoi > tongSucChua)
                throw new InvalidOperationException($"Tổng sức chứa ({tongSucChua} người) không đủ cho đoàn {soNguoi} người.");

            // Sinh mã đặt phòng
            var lastMa = await _db.DatPhongs
                .OrderByDescending(d => d.MaDatPhong)
                .Select(d => d.MaDatPhong)
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

            // Thêm từng phòng vào chi tiết đặt
            foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
            {
                await EnsurePhongAvailableAsync(maPhong, ngayNhan, ngayTra);

                var phong = await _db.Phongs
                    .Include(p => p.MaLoaiPhongNavigation)
                    .FirstAsync(p => p.MaPhong == maPhong);

                _db.DatPhongChiTiets.Add(new DatPhongChiTiet
                {
                    MaDatPhong = dp.MaDatPhong,
                    MaPhong = maPhong,
                    NgayNhan = ngayNhan,
                    NgayTra = ngayTra,
                    DonGia = phong.MaLoaiPhongNavigation.GiaPhong,
                    MaNhanVien = maNhanVien
                });

                phong.MaTrangThaiPhong = PhongTrangThaiCodes.DaDat; // Đã đặt
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return dp;
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    #endregion

    #region CHECK-IN & HỦY PHÒNG
    public async Task CheckInAsync(string maDatPhong, string maNhanVienLeTan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 
        try
        {
            var dp = await _db.DatPhongs
                .Include(d => d.DatPhongChiTiets)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin đặt phòng.");

            if (dp.TrangThai != DatPhongTrangThaiTexts.ChoNhanPhong)
                throw new InvalidOperationException($"Không thể nhận phòng. Trạng thái hiện tại: {dp.TrangThai}");

            dp.TrangThai = DatPhongTrangThaiTexts.DangO;

            // Cập nhật trạng thái từng phòng trong đoàn sang "Đang ở"
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                if (phong != null) phong.MaTrangThaiPhong = PhongTrangThaiCodes.DangO; // Đang có khách

                // Cập nhật lại ngày nhận thực tế là lúc Check-in
                ct.NgayNhan = TimeHelper.GetVietnamTime();
            }

            await _db.SaveChangesAsync();

            // Tự động tạo hóa đơn nháp (Chưa thanh toán) cho đoàn
            await TaoHoaDonNeuChuaCoAsync(maDatPhong, maNhanVienLeTan);

            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); _db.ChangeTracker.Clear(); throw; }
    }

    public async Task HuyDatPhongAsync(string maDatPhong, string lyDo, decimal tienHoanTra = 0)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 
        try
        {
            var dp = await _db.DatPhongs
                .Include(d => d.DatPhongChiTiets)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                ?? throw new KeyNotFoundException("Không tìm thấy đặt phòng");

            // Xác định trạng thái hủy cụ thể dựa trên tiền hoàn trả
            if (dp.TienCoc > 0)
            {
                if (tienHoanTra >= dp.TienCoc)
                    dp.TrangThai = "Đã hủy - Hoàn cọc";
                else if (tienHoanTra > 0)
                    dp.TrangThai = $"Đã hủy - Hoàn cọc {tienHoanTra:N0}";
                else
                    dp.TrangThai = "Đã hủy - Mất cọc";
            }
            else
            {
                dp.TrangThai = DatPhongTrangThaiTexts.DaHuy;
            }

            // Giải phóng các phòng trong đoàn
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                if (phong != null)
                {
                    bool isOccupiedElsewhere = await _db.DatPhongChiTiets
                        .AnyAsync(c => c.MaPhong == ct.MaPhong && c.MaDatPhong != maDatPhong &&
                                      c.MaDatPhongNavigation!.TrangThai == DatPhongTrangThaiTexts.DangO);

                    if (!isOccupiedElsewhere) phong.MaTrangThaiPhong = PhongTrangThaiCodes.Trong; // Trống
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }
    #endregion

    #region GIA HẠN & ĐỔI PHÒNG (SẴN SÀNG CHO CHUYỂN PHÒNG)
    public async Task GiaHanAsync(string maDatPhong, string maPhong, DateTime ngayTraMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var ct = await _db.DatPhongChiTiets
                         .FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhong)
                     ?? throw new KeyNotFoundException("Không tìm thấy chi tiết phòng cần gia hạn.");

            // 1. Kiểm tra ngày gia hạn hợp lệ
            if (ngayTraMoi.Date <= ct.NgayTra.Date)
                throw new ArgumentException("Ngày gia hạn phải nằm sau ngày trả phòng hiện tại.");

            // 2. Kiểm tra xem những ngày xin ở thêm có bị khách khác đặt trước chưa?
            // Chỉ cần check khoảng thời gian dôi ra: từ NgayTra (cũ) đến ngayTraMoi
            await EnsurePhongAvailableAsync(maPhong, ct.NgayTra, ngayTraMoi, maDatPhong);

            // 3. Cập nhật ngày trả mới cho phòng
            ct.NgayTra = ngayTraMoi;

            // Cập nhật lại ngày trả dự kiến của cả đoàn (nếu phòng này gia hạn lâu nhất)
            var dp = await _db.DatPhongs.FindAsync(maDatPhong);
            if (dp != null && ngayTraMoi > dp.NgayTraDuKien)
            {
                dp.NgayTraDuKien = ngayTraMoi;
            }

            // 4. QUAN TRỌNG: Đồng bộ lại Số đêm vào Hóa Đơn để tiền phòng tự động tăng lên
            var hdct = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhong);
            if (hdct != null)
            {
                hdct.SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ngayTraMoi);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task DoiPhongAsync(string maDatPhong, string maPhongCu, string maPhongMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var ctCu = await _db.DatPhongChiTiets
                .FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhongCu)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng cũ trong đơn đặt.");

            DateTime thoiDiemDoiPhong = TimeHelper.GetVietnamTime();

            // BẢO VỆ 1: Khách đổi phòng vào đúng ngày phải trả phòng (Phải gia hạn trước)
            if (thoiDiemDoiPhong.Date >= ctCu.NgayTra.Date)
            {
                throw new InvalidOperationException("Đã đến hạn trả phòng. Nếu muốn đổi phòng để ở thêm, vui lòng thao tác Gia Hạn trước!");
            }

            var hdctCu = await _db.HoaDonChiTiets.FirstOrDefaultAsync(h => h.MaDatPhong == maDatPhong && h.MaPhong == maPhongCu);
            var dichVuCus = await _db.DichVuChiTiets.Where(dv => dv.MaDatPhong == maDatPhong && dv.MaPhong == maPhongCu).ToListAsync();

            // ==============================================================
            // TRƯỜNG HỢP 1: ĐỔI PHÒNG TRONG CÙNG 1 NGÀY (Không tách chặng)
            // ==============================================================
            if (thoiDiemDoiPhong.Date <= ctCu.NgayNhan.Date)
            {
                await EnsurePhongAvailableAsync(maPhongMoi, ctCu.NgayNhan, ctCu.NgayTra, maDatPhong);

                var phongMoi = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation).FirstAsync(p => p.MaPhong == maPhongMoi);
                var phongCu = await _db.Phongs.FindAsync(maPhongCu);

                if (phongCu != null)
                {
                    phongMoi.MaTrangThaiPhong = phongCu.MaTrangThaiPhong;

                    // Trả phòng cũ về trạng thái phù hợp
                    if (phongCu.MaTrangThaiPhong == PhongTrangThaiCodes.DangO)
                        phongCu.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep; // Nếu khách đã vào rồi thì phải dọn dẹp
                    else
                        phongCu.MaTrangThaiPhong = PhongTrangThaiCodes.Trong; // Nếu khách chưa check-in thì trả về Trống
                }

                // 1. Dời Dịch Vụ
                foreach (var dv in dichVuCus)
                {
                    _db.DichVuChiTiets.Add(new DichVuChiTiet
                    {
                        MaHoaDon = dv.MaHoaDon,
                        MaDatPhong = dv.MaDatPhong,
                        MaPhong = maPhongMoi,
                        MaDichVu = dv.MaDichVu,
                        SoLuong = dv.SoLuong,
                        DonGia = dv.DonGia,
                        NgaySuDung = dv.NgaySuDung
                    });
                }
                _db.DichVuChiTiets.RemoveRange(dichVuCus);

                // 2. Dời Hóa Đơn Chi Tiết
                if (hdctCu != null)
                {
                    _db.HoaDonChiTiets.Add(new HoaDonChiTiet
                    {
                        MaHoaDon = hdctCu.MaHoaDon,
                        MaDatPhong = hdctCu.MaDatPhong,
                        MaPhong = maPhongMoi,
                        SoDem = hdctCu.SoDem
                    });
                    _db.HoaDonChiTiets.Remove(hdctCu);
                }

                // 3. Thay thế Đặt Phòng Chi Tiết (Sửa bằng cách Xóa -> Thêm)
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
            // ==============================================================
            // TRƯỜNG HỢP 2: ĐỔI PHÒNG KHI ĐÃ QUA ĐÊM (Bắt buộc Tách chặng)
            // ==============================================================
            else
            {
                await EnsurePhongAvailableAsync(maPhongMoi, thoiDiemDoiPhong, ctCu.NgayTra, maDatPhong);

                var phongMoi = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation).FirstAsync(p => p.MaPhong == maPhongMoi);
                var phongCu = await _db.Phongs.FindAsync(maPhongCu);

                if (phongCu != null) phongCu.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
                phongMoi.MaTrangThaiPhong = PhongTrangThaiCodes.DangO;

                var ngayTraDuKien = ctCu.NgayTra;

                // Chốt sổ phòng cũ (Lúc này chắc chắn Ngày Trả > Ngày Nhận -> Vượt qua được Rule của SQL)
                ctCu.NgayTra = thoiDiemDoiPhong;

                // Mở chặng phòng mới
                _db.DatPhongChiTiets.Add(new DatPhongChiTiet
                {
                    MaDatPhong = maDatPhong,
                    MaPhong = maPhongMoi,
                    NgayNhan = thoiDiemDoiPhong,
                    NgayTra = ngayTraDuKien,
                    DonGia = phongMoi.MaLoaiPhongNavigation.GiaPhong,
                    MaNhanVien = ctCu.MaNhanVien
                });

                // Tách số đêm trên Hóa đơn
                if (hdctCu != null)
                {
                    hdctCu.SoDem = TinhToanHoaDonService.TinhSoDem(ctCu.NgayNhan, thoiDiemDoiPhong);

                    _db.HoaDonChiTiets.Add(new HoaDonChiTiet
                    {
                        MaHoaDon = hdctCu.MaHoaDon,
                        MaDatPhong = maDatPhong,
                        MaPhong = maPhongMoi,
                        SoDem = TinhToanHoaDonService.TinhSoDem(thoiDiemDoiPhong, ngayTraDuKien)
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            // Giữ nguyên dòng ném lỗi để nếu có lỗi khác ta vẫn xem được
            string loiThucSu = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            throw new Exception($"Lỗi Đổi phòng: {loiThucSu}");
        }
    }


    // 1. Hủy đặt phòng
    public async Task HuyDatPhongAsync(string maDatPhong, string maNhanVien)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 
        try
        {
            var dp = await _db.DatPhongs.Include(d => d.DatPhongChiTiets).FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong);
            if (dp == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt phòng.");

            // Ghi nhận chi phí nếu có tiền cọc (Hoàn trả tiền cọc)
            if (dp.TienCoc > 0)
            {
                var loaiCp = await _db.LoaiChiPhis.FirstOrDefaultAsync(l => l.MaLoaiCp == "LCP007" || l.TenLoaiCp.Contains("Hoàn tiền"));
                string maLoaiCp = loaiCp?.MaLoaiCp ?? "LCP007";
                if (loaiCp == null)
                {
                    _db.LoaiChiPhis.Add(new LoaiChiPhi { MaLoaiCp = "LCP007", TenLoaiCp = "Hoàn tiền hóa đơn" });
                    await _db.SaveChangesAsync();
                }

                var lastCp = await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync();
                _db.ChiPhis.Add(new ChiPhi
                {
                    MaChiPhi = MaHelper.Next("CP", lastCp),
                    MaLoaiCp = maLoaiCp,
                    MaNhanVien = maNhanVien,
                    TenChiPhi = $"Hoàn tiền cọc do hủy đơn (Đơn: {maDatPhong})",
                    SoTien = dp.TienCoc ?? 0,
                    NgayChiPhi = TimeHelper.GetVietnamTime(),
                    GhiChu = $"Hủy đặt phòng. Khách: {dp.MaKhachHang}"
                });
            }

            // Cập nhật trạng thái các phòng liên quan về Trống (PTT01)
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var p = await _db.Phongs.FindAsync(ct.MaPhong);
                if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong;
            }

            dp.TrangThai = DatPhongTrangThaiTexts.DaHuy;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    // 2. Hoàn tất dọn dẹp
    public async Task HoanThanhDonDepAsync(string maPhong)
    {
        var p = await _db.Phongs.FindAsync(maPhong);
        if (p != null)
        {
            p.MaTrangThaiPhong = PhongTrangThaiCodes.Trong; // Chuyển về Trống
            await _db.SaveChangesAsync();
        }
    }



    #endregion
}
