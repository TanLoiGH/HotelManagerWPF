using System.Data;
using Microsoft.EntityFrameworkCore;
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
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != "Đã hủy");

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
            TrangThai = "Chưa thanh toán"
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
                x.d.TrangThai != "Đã trả phòng" &&
                x.d.TrangThai != "Đã hủy" &&
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

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
                TrangThai = "Chờ nhận phòng",
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
                    DonGia = phong.MaLoaiPhongNavigation.GiaPhong
                });

                phong.MaTrangThaiPhong = "PTT05"; // Đã đặt
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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var dp = await _db.DatPhongs
                .Include(d => d.DatPhongChiTiets)
                .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin đặt phòng.");

            if (dp.TrangThai != "Chờ nhận phòng")
                throw new InvalidOperationException($"Không thể nhận phòng. Trạng thái hiện tại: {dp.TrangThai}");

            dp.TrangThai = "Đang ở";

            // Cập nhật trạng thái từng phòng trong đoàn sang "Đang ở"
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                if (phong != null) phong.MaTrangThaiPhong = "PTT02"; // Đang có khách

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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
                dp.TrangThai = "Đã hủy";
            }

            // Giải phóng các phòng trong đoàn
            foreach (var ct in dp.DatPhongChiTiets)
            {
                var phong = await _db.Phongs.FindAsync(ct.MaPhong);
                if (phong != null)
                {
                    bool isOccupiedElsewhere = await _db.DatPhongChiTiets
                        .AnyAsync(c => c.MaPhong == ct.MaPhong && c.MaDatPhong != maDatPhong &&
                                      c.MaDatPhongNavigation!.TrangThai == "Đang ở");

                    if (!isOccupiedElsewhere) phong.MaTrangThaiPhong = "PTT01"; // Trống
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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var ct = await _db.DatPhongChiTiets.FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhong)
            ?? throw new KeyNotFoundException("Không tìm thấy chi tiết phòng cần gia hạn.");

        if (ngayTraMoi <= ct.NgayNhan)
            throw new ArgumentException("Ngày trả mới phải sau ngày nhận.");

        await EnsurePhongAvailableAsync(maPhong, ct.NgayNhan, ngayTraMoi, maDatPhong);

        ct.NgayTra = ngayTraMoi;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task DoiPhongAsync(string maDatPhong, string maPhongCu, string maPhongMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ctCu = await _db.DatPhongChiTiets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaDatPhong == maDatPhong && x.MaPhong == maPhongCu)
                ?? throw new KeyNotFoundException("Không tìm thấy phòng cũ trong đơn đặt.");

            // 1. Kiểm tra phòng mới có trống không
            await EnsurePhongAvailableAsync(maPhongMoi, ctCu.NgayNhan, ctCu.NgayTra, maDatPhong);

            var phongMoi = await _db.Phongs.Include(p => p.MaLoaiPhongNavigation).FirstAsync(p => p.MaPhong == maPhongMoi);
            var phongCu = await _db.Phongs.FindAsync(maPhongCu);

            // 2. Cập nhật trạng thái vật lý của phòng
            if (phongCu != null) phongCu.MaTrangThaiPhong = "PTT03"; // Chuyển sang Dọn dẹp
            phongMoi.MaTrangThaiPhong = "PTT02"; // Đang ở

            await _db.SaveChangesAsync(); // Lưu trạng thái vật lý xuống DB trước

            decimal donGiaMoi = phongMoi.MaLoaiPhongNavigation.GiaPhong;

            // 3. RAW SQL SEQUENCE (Khắc phục lỗi Foreign Key)

            // Bước 3.1: Tạo dòng Đặt Phòng mới (Nhân bản dòng cũ nhưng đổi Tên phòng & Giá mới)
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO DAT_PHONG_CHI_TIET (MaDatPhong, MaPhong, NgayNhan, NgayTra, DonGia, MaNhanVien) 
                  SELECT MaDatPhong, {0}, NgayNhan, NgayTra, {1}, MaNhanVien 
                  FROM DAT_PHONG_CHI_TIET 
                  WHERE MaDatPhong = {2} AND MaPhong = {3}",
                maPhongMoi, donGiaMoi, maDatPhong, maPhongCu);

            // Bước 3.2: Dời các Dịch vụ khách đã gọi (nếu có) sang phòng mới
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE DICH_VU_CHI_TIET SET MaPhong = {0} WHERE MaDatPhong = {1} AND MaPhong = {2}",
                maPhongMoi, maDatPhong, maPhongCu);

            // Bước 3.3: Dời Hóa đơn nháp sang phòng mới
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE HOA_DON_CHI_TIET SET MaPhong = {0} WHERE MaDatPhong = {1} AND MaPhong = {2}",
                maPhongMoi, maDatPhong, maPhongCu);

            // Bước 3.4: Xóa dòng Đặt Phòng cũ (Lúc này không còn ai trỏ vào nên xóa thoải mái)
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM DAT_PHONG_CHI_TIET WHERE MaDatPhong = {0} AND MaPhong = {1}",
                maDatPhong, maPhongCu);

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    // 1. Hủy đặt phòng
    public async Task HuyDatPhongAsync(string maDatPhong, string maNhanVien)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
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
                if (p != null) p.MaTrangThaiPhong = "PTT01";
            }

            dp.TrangThai = "Đã hủy";
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
            p.MaTrangThaiPhong = "PTT01"; // Chuyển về Trống
            await _db.SaveChangesAsync();
        }
    }



    #endregion
}