using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class HoaDonService : IHoaDonService
{
    private readonly QuanLyKhachSanContext _db;
    private readonly KhachHangService _khachHangSvc;
    private readonly IAuditService _auditService;

    public HoaDonService(QuanLyKhachSanContext db, KhachHangService khachHangSvc, IAuditService auditService)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
        _auditService = auditService;
    }

    public async Task<List<HoaDon>> LayHoaDonsAsync()
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.HoaDonChiTiets)
            .Include(h => h.DichVuChiTiets)
            .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
            .OrderByDescending(h => h.NgayLap)
            .ToListAsync();
    }

    public async Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != HoaDonTrangThaiTexts.DaHuy);

        if (hdExist)
            throw new InvalidOperationException("Đặt phòng này đã có hóa đơn active.");

        var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
        if (!chiTiets.Any())
            throw new InvalidOperationException("Không có phòng nào trong đặt phòng.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;

        decimal tienPhong = 0;
        bool coPhongChuaNhapGia = false;
        foreach (var ct in chiTiets)
        {
            if (ct.DonGia <= 0) coPhongChuaNhapGia = true;
            int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra);
            tienPhong += ct.DonGia * soDem;
        }

        var khuyenMaiHopLe = await LayKhuyenMaiHopLeAsync(maKhuyenMai);
        if (coPhongChuaNhapGia && khuyenMaiHopLe == null)
            throw new InvalidOperationException("Phát hiện phòng chưa được thiết lập giá. Vui lòng áp dụng Mã khuyến mãi 100%!");

        decimal giamGia = 0;
        if (khuyenMaiHopLe != null)
            giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, 0, khuyenMaiHopLe.LoaiKhuyenMai ?? "", khuyenMaiHopLe.GiaTriKm ?? 0);

        var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon).FirstOrDefaultAsync();
        var newMaHd = MaHelper.Next("HD", lastMa);
        decimal vatPercent = SystemSettingsService.Load().VatPercent;

        var hd = new HoaDon
        {
            MaHoaDon = newMaHd,
            MaDatPhong = maDatPhong,
            MaNhanVien = maNhanVien,
            NgayLap = TimeHelper.GetVietnamTime(),
            TienPhong = tienPhong,
            TienDichVu = 0,
            Vat = vatPercent,
            MaKhuyenMai = maKhuyenMai,
            TongThanhToan = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, 0, vatPercent, tienCoc, giamGia),
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
        await tx.CommitAsync();

        // Ghi log sau khi Commit thành công
        await _auditService.LogAsync("Xuất hóa đơn",
            $"Nhân viên {maNhanVien} đã xuất hóa đơn {newMaHd} cho đặt phòng {maDatPhong}. Tổng tiền: {hd.TongThanhToan:N0}đ");

        return hd;
    }

    public async Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon)
    {
        var hd = await _db.HoaDons
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

        if (string.IsNullOrWhiteSpace(hd.MaDatPhong))
            throw new InvalidOperationException("Hóa đơn không có mã đặt phòng.");

        var chiTietDatPhong = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == hd.MaDatPhong)
            .ToListAsync();

        if (!chiTietDatPhong.Any())
            throw new InvalidOperationException("Đặt phòng không có chi tiết phòng.");

        var existingKeys = await _db.HoaDonChiTiets
            .Where(c => c.MaHoaDon == maHoaDon)
            .Select(c => c.MaDatPhong + "|" + c.MaPhong)
            .ToListAsync();

        var existingSet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = chiTietDatPhong
            .Where(ct => !existingSet.Contains(ct.MaDatPhong + "|" + ct.MaPhong))
            .Select(ct => new HoaDonChiTiet
            {
                MaHoaDon = maHoaDon,
                MaDatPhong = ct.MaDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
            })
            .ToList();

        if (toAdd.Count == 0)
            return 0;

        _db.HoaDonChiTiets.AddRange(toAdd);
        await _db.SaveChangesAsync();
        return toAdd.Count;
    }

    public async Task<bool> ThanhToanAsync(
        string maHoaDon,
        decimal soTien,
        string maPTTT,
        string nguoiThu,
        string loaiGiaoDich = "Thanh toán cuối",
        string? noiDung = null)
    {
        var thongTin = await ThanhToanVaTraKetQuaAsync(maHoaDon, soTien, maPTTT, nguoiThu, loaiGiaoDich, noiDung);
        return thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat;
    }

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu, string loaiGiaoDich = "Thanh toán cuối", string? noiDung = null)
    {
        if (loaiGiaoDich != "Hoàn tiền" && soTien < 0)
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Từ chối: số tiền không hợp lệ!");

        if (loaiGiaoDich == "Hoàn tiền" && soTien > 0) soTien = -soTien;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null) return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            decimal tienPhong = hd.TienPhong ?? 0;
            decimal tienDv = hd.TienDichVu ?? 0;
            decimal vatPercent = hd.Vat ?? 0;
            decimal giamGia = 0;

            var km = hd.MaKhuyenMaiNavigation ?? (!string.IsNullOrWhiteSpace(hd.MaKhuyenMai) ? await _db.KhuyenMais.FindAsync(hd.MaKhuyenMai) : null);
            if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
                giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, tienDv, km.LoaiKhuyenMai ?? "", km.GiaTriKm ?? 0);

            decimal tongThanhToanThuc = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, tienDv, vatPercent, 0, giamGia);
            var tongDaThuLichSu = await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;
            var tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            var tongDaThuHienTai = tongDaThuLichSu + tienCoc;

            if (loaiGiaoDich == "Hoàn tiền" && (tongDaThuHienTai + soTien) < 0)
            {
                await tx.RollbackAsync();
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, tongDaThuHienTai, 0, "Số tiền hoàn lại vượt quá số tiền khách đã trả!");
            }

            if (soTien != 0)
            {
                var lastTt = await _db.ThanhToans.OrderByDescending(t => t.MaThanhToan).Select(t => t.MaThanhToan).FirstOrDefaultAsync();
                _db.ThanhToans.Add(new ThanhToan
                {
                    MaThanhToan = MaHelper.Next("TT", lastTt),
                    MaHoaDon = maHoaDon,
                    MaPttt = maPTTT,
                    SoTien = soTien,
                    LoaiGiaoDich = loaiGiaoDich,
                    NgayThanhToan = TimeHelper.GetVietnamTime(),
                    NguoiThu = nguoiThu,
                    NoiDung = noiDung
                });

                if (loaiGiaoDich == "Hoàn tiền")
                {
                    var loaiCpHoanTien = await _db.LoaiChiPhis.FirstOrDefaultAsync(l => l.MaLoaiCp == "LCP007" || l.TenLoaiCp.Contains("Hoàn tiền"));
                    string maLoaiCpHopLe = loaiCpHoanTien?.MaLoaiCp ?? "LCP007";
                    if (loaiCpHoanTien == null) _db.LoaiChiPhis.Add(new LoaiChiPhi { MaLoaiCp = maLoaiCpHopLe, TenLoaiCp = "Hoàn tiền hóa đơn" });

                    var lastCp = await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync();
                    _db.ChiPhis.Add(new ChiPhi
                    {
                        MaChiPhi = MaHelper.Next("CP", lastCp),
                        MaLoaiCp = maLoaiCpHopLe,
                        MaNhanVien = nguoiThu,
                        MaPhong = hd.MaDatPhongNavigation?.DatPhongChiTiets.FirstOrDefault()?.MaPhong,
                        TenChiPhi = $"Hoàn tiền thừa cho khách (Hóa đơn: {maHoaDon})",
                        SoTien = Math.Abs(soTien),
                        NgayChiPhi = TimeHelper.GetVietnamTime(),
                        GhiChu = noiDung ?? "Hệ thống tự động ghi nhận."
                    });
                }
            }

            decimal tongDaThuMoi = tongDaThuHienTai + soTien;
            bool daThuDu = tongDaThuMoi >= tongThanhToanThuc;
            if (daThuDu) hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Ghi Log nghiệp vụ tài chính
            string chiTietLog = (loaiGiaoDich == "Hoàn tiền")
                ? $"Hoàn tiền: {Math.Abs(soTien):N0}đ cho hóa đơn {maHoaDon}."
                : $"Thanh toán: {soTien:N0}đ cho hóa đơn {maHoaDon}. Hình thức: {maPTTT}.";
            await _auditService.LogAsync(loaiGiaoDich, chiTietLog);

            return daThuDu
                ? new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThuMoi, tongThanhToanThuc - tongDaThuMoi, "Chốt sổ thành công!")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThuMoi, tongThanhToanThuc - tongDaThuMoi, "Khách chưa thanh toán đủ.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.LogError("Lỗi", ex);
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Lỗi: {ex.Message}");
        }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 

        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            var tongThanhToan = hd.TongThanhToan ?? 0;
            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu >= tongThanhToan && hd.TrangThai != HoaDonTrangThaiTexts.DaThanhToan)
            {
                hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            var conLai = tongThanhToan - tongDaThu;
            return tongDaThu >= tongThanhToan
                ? new ThongTinThanhToan(KetQuaThanhToan.DaHoanTat, tongDaThu, conLai, "Hoá đơn đã đủ tiền.")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThu, conLai, "Hoá đơn chưa đủ tiền.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.LogError("Lỗi", ex);
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Lỗi đồng bộ trạng thái: {ex.Message}");
        }
    }

    // Cap nhat tong tien khi khach tra som (khong check out, chi tinh lai so dem + tong tien).
            // Neu sau khi tinh lai tongDaThu >= TongThanhToan thi tu dong dong bo TrangThai = "Đã thanh toán".
            public async Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong)
            {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted); 

        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiemTraPhong);

            var tongThanhToan = hd.TongThanhToan ?? 0;
            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu >= tongThanhToan && hd.TrangThai != HoaDonTrangThaiTexts.DaThanhToan)
                hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var conLai = tongThanhToan - tongDaThu;
            return tongDaThu >= tongThanhToan
                ? new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThu, conLai, "Đã cập nhật tổng tiền và hoá đơn đã đủ tiền.")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThu, conLai, "Đã cập nhật tổng tiền, hoá đơn chưa đủ tiền.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.LogError("Lỗi", ex);

            // Moi chi tiết lỗi thực sự từ Database (InnerException)
            string chiTietLoi = ex.Message;
            if (ex.InnerException != null)
            {
                chiTietLoi += "\nChi tiết từ DB: " + ex.InnerException.Message;
            }

            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Lỗi cập nhật tổng tiền: {chiTietLoi}");
        
        }
    }

    public async Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets).Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon) ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

            var dp = hd.MaDatPhongNavigation;
            if (dp == null || dp.TrangThai == DatPhongTrangThaiTexts.DaTraPhong) { await tx.RollbackAsync(); return; }

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? TimeHelper.GetVietnamTime());

            decimal tienPhong = hd.TienPhong ?? 0, tienDv = hd.TienDichVu ?? 0, vatPercent = hd.Vat ?? 0, giamGia = 0;
            var km = hd.MaKhuyenMaiNavigation;
            if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
                giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, tienDv, km.LoaiKhuyenMai ?? "", km.GiaTriKm ?? 0);

            decimal tongThanhToanThuc = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, tienDv, vatPercent, 0, giamGia);
            var tongDaThu = (await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0) + (dp.TienCoc ?? 0);

            if (tongDaThu < tongThanhToanThuc) throw new InvalidOperationException($"Chưa thanh toán đủ (còn nợ {tongThanhToanThuc - tongDaThu:N0}đ).");
            if (tongDaThu > tongThanhToanThuc) throw new InvalidOperationException($"Khách còn dư {tongDaThu - tongThanhToanThuc:N0}đ. Hãy hoàn tiền trước!");

            hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
            foreach (var ct in dp.DatPhongChiTiets ?? [])
            {
                var p = await _db.Phongs.FindAsync(ct.MaPhong);
                if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
            }
            dp.TrangThai = DatPhongTrangThaiTexts.DaTraPhong;

            if (!string.IsNullOrWhiteSpace(dp.MaKhachHang)) await _khachHangSvc.NangHangAsync(dp.MaKhachHang, tongThanhToanThuc);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _auditService.LogAsync("Trả phòng", $"Nhân viên {maNhanVien} trả phòng HĐ: {maHoaDon}. Doanh thu: {tongThanhToanThuc:N0}đ");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.LogError("Lỗi", ex);
            throw new Exception(ex.Message);
        }
    }

    private async Task CapNhatTienPhongTheoThoiDiemTraPhongAsync(HoaDon hd, DateTime thoiDiem)
    {
        if (string.IsNullOrWhiteSpace(hd.MaDatPhong) || hd.MaDatPhongNavigation == null)
            return;

        var dp = hd.MaDatPhongNavigation;
        var chiTiets = dp.DatPhongChiTiets?.ToList() ?? new List<DatPhongChiTiet>();
        if (chiTiets.Count == 0) return;

        DateTime checkout = thoiDiem;

        decimal tienPhong = TinhToanHoaDonService.TinhTienPhongTheoNgayTra(
            chiTiets,
            checkout,
            capNhatSoDem: (ct, soDem) =>
            {
                var hdct = hd.HoaDonChiTiets.FirstOrDefault(x =>
                    x.MaHoaDon == hd.MaHoaDon &&
                    x.MaDatPhong == ct.MaDatPhong &&
                    x.MaPhong == ct.MaPhong);

                if (hdct != null)
                    hdct.SoDem = soDem;
            },
            capNhatNgayTra: true);

        hd.TienPhong = tienPhong;

        decimal tienDv = hd.TienDichVu ?? 0;
        decimal vatPercent = hd.Vat ?? 10;
        decimal tienCoc = dp.TienCoc ?? 0;

        var km = hd.MaKhuyenMaiNavigation ?? (!string.IsNullOrWhiteSpace(hd.MaKhuyenMai) ? await _db.KhuyenMais.FindAsync(hd.MaKhuyenMai) : null);
        decimal giamGia = 0;
        if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
            giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, tienDv, km.LoaiKhuyenMai ?? "", km.GiaTriKm ?? 0);

        hd.TongThanhToan = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, tienDv, vatPercent, tienCoc, giamGia);

        await _db.SaveChangesAsync();
    }

    public async Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent)
    {
        var hoaDons = await _db.HoaDons
            .Include(h => h.MaDatPhongNavigation)
            .Where(h => h.TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan)
            .ToListAsync();

        if (hoaDons.Count == 0)
            return 0;

        foreach (var hd in hoaDons)
        {
            decimal tienPhong = hd.TienPhong ?? 0;
            decimal tienDv = hd.TienDichVu ?? 0;
            decimal tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;

            decimal tongThanhToan = hd.TongThanhToan ?? 0;
            decimal oldVat = hd.Vat ?? 0;

            decimal tongTruocVat = oldVat > 0
                ? (tongThanhToan + tienCoc) / (1 + oldVat / 100m)
                : tongThanhToan + tienCoc;

            decimal giamGia = (tienPhong + tienDv) - tongTruocVat;
            if (giamGia < 0) giamGia = 0;
            if (giamGia > (tienPhong + tienDv)) giamGia = tienPhong + tienDv;

            hd.Vat = vatPercent;
            hd.TongThanhToan = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, tienDv, vatPercent, tienCoc, giamGia);
        }

        await _db.SaveChangesAsync();
        // Sau khi SaveChanges thành công:
        await _auditService.LogAsync("Cập nhật VAT",
            $"Hệ thống đã cập nhật mức thuế VAT mới ({vatPercent}%) cho {hoaDons.Count} hóa đơn đang mở.");

        return hoaDons.Count;
    }

    public async Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync()
    {
        return await _db.PhuongThucThanhToans
            .Select(p => new PhuongThucThanhToanDto { MaPTTT = p.MaPttt, TenPhuongThuc = p.TenPhuongThuc })
            .ToListAsync();
    }

    public async Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
    }

    public async Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon)
    {
        return await _db.ThanhToans
            .AsNoTracking()
            .Include(t => t.MaPtttNavigation)
            .Where(t => t.MaHoaDon == maHoaDon)
            .OrderBy(t => t.NgayThanhToan)
            .ToListAsync();
    }

    public async Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
            .Include(h => h.MaNhanVienNavigation)
            .Include(h => h.HoaDonChiTiets)
                .ThenInclude(ct => ct.DatPhongChiTiet)
            .Include(h => h.DichVuChiTiets)
                .ThenInclude(dv => dv.MaDichVuNavigation)
            .Include(h => h.MaKhuyenMaiNavigation)
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
    }

    public async Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Include(h => h.MaKhuyenMaiNavigation)
            .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
            .Include(h => h.MaNhanVienNavigation)

            //thông tin khi in
            .Include(h => h.HoaDonChiTiets)
                .ThenInclude(ct => ct.DatPhongChiTiet)
            .Include(h => h.DichVuChiTiets)
                .ThenInclude(dv => dv.MaDichVuNavigation)
            .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
    }

    private async Task<KhuyenMai?> LayKhuyenMaiHopLeAsync(string? maKhuyenMai)
    {
        if (string.IsNullOrWhiteSpace(maKhuyenMai)) return null;
        var km = await _db.KhuyenMais.FindAsync(maKhuyenMai);
        if (km is { IsActive: true } && km.NgayBatDau <= DateTime.Now && DateTime.Now <= km.NgayKetThuc)
            return km;
        return null;
    }

    public async Task HuyHoaDonAsync(string maHoaDon)
    {
        var hd = await _db.HoaDons.FindAsync(maHoaDon);
        if (hd == null) throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
        if (hd.TrangThai == HoaDonTrangThaiTexts.DaThanhToan) throw new InvalidOperationException("Không thể hủy hóa đơn đã thanh toán.");

        hd.TrangThai = HoaDonTrangThaiTexts.DaHuy;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("Hủy hóa đơn", $"Nhân viên {AppSession.MaNhanVien} đã hủy hóa đơn {maHoaDon}.");
    }




}




public enum KetQuaThanhToan
{
    GhiNhanChuaDu,
    HoanTat,
    DaHoanTat,
    TuChoi
}

public sealed record ThongTinThanhToan(
    KetQuaThanhToan KetQua,
    decimal TongDaThu,
    decimal ConLai,
    string ThongDiep);
