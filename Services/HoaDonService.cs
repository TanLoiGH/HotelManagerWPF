using System.Data;
using Microsoft.EntityFrameworkCore;
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

    public HoaDonService(QuanLyKhachSanContext db, KhachHangService khachHangSvc)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
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

    public async Task<HoaDon> XuatHoaDonAsync(
        string maDatPhong, string maNhanVien,
        string? maKhuyenMai = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != "Đã hủy");

        if (hdExist)
            throw new InvalidOperationException("Đặt phòng này đã có hóa đơn active.");

        var chiTiets = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == maDatPhong)
            .ToListAsync();

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
        // BẮT LỖI 1: Phòng giá 0đ nhưng KHÔNG CÓ khuyến mãi hợp lệ
        if (coPhongChuaNhapGia && khuyenMaiHopLe == null)
        {
            throw new InvalidOperationException("Phát hiện phòng chưa được thiết lập giá (Đơn giá = 0). Nếu khách ở miễn phí, vui lòng áp dụng Mã khuyến mãi 100%!");
        }
        decimal giamGia = 0;
        if (khuyenMaiHopLe != null)
            giamGia = TinhToanHoaDonService.TinhGiamGia(tienPhong, 0, khuyenMaiHopLe.LoaiKhuyenMai ?? "", khuyenMaiHopLe.GiaTriKm ?? 0);

        var lastMa = await _db.HoaDons
            .OrderByDescending(h => h.MaHoaDon)
            .Select(h => h.MaHoaDon)
            .FirstOrDefaultAsync();

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
            TrangThai = "Chưa thanh toán"
        };

        _db.HoaDons.Add(hd);

        foreach (var ct in chiTiets)
        {
            var detail = new HoaDonChiTiet
            {
                MaHoaDon = hd.MaHoaDon,
                MaDatPhong = maDatPhong,
                MaPhong = ct.MaPhong,
                SoDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra)
            };

            _db.HoaDonChiTiets.Add(detail);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

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

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(
           string maHoaDon,
           decimal soTien,
           string maPTTT,
           string nguoiThu,
           string loaiGiaoDich = "Thanh toán cuối",
           string? noiDung = null)
    {
        // 1. Xử lý chuẩn hóa số tiền đầu vào
        if (loaiGiaoDich != "Hoàn tiền" && soTien < 0)
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Từ chối: số tiền không hợp lệ!");

        if (loaiGiaoDich == "Hoàn tiền" && soTien > 0)
            soTien = -soTien; // Phiếu thanh toán phải là số âm để cấn trừ công nợ Hóa Đơn

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            // Thêm Include MaDatPhongNavigation để lấy được Mã Phòng bên dưới
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            var tienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            var tongThanhToanThuc = (hd.TongThanhToan ?? 0) + tienCoc;

            var tongDaThuLichSu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            var tongDaThuHienTai = tongDaThuLichSu + tienCoc;

            // Bắt lỗi logic dòng tiền (Chống "Hoàn tiền" lố hoặc chốt sổ 0đ khi còn nợ)
            if (loaiGiaoDich == "Hoàn tiền" && (tongDaThuHienTai + soTien) < 0)
            {
                await tx.RollbackAsync();
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, tongDaThuHienTai, 0,
                    "Từ chối: Số tiền hoàn lại vượt quá tổng số tiền khách đã thanh toán!");
            }

            var soTienConLai = tongThanhToanThuc - tongDaThuHienTai;
            
            if (soTien == 0 && soTienConLai > 0 && loaiGiaoDich == "Thanh toán cuối")
            {
                await tx.RollbackAsync();
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, tongDaThuHienTai, soTienConLai,
                    "Từ chối: Hóa đơn vẫn còn nợ, không thể chốt sổ bằng giao dịch 0đ!");
            }

            // 2. LƯU GIAO DỊCH VÀO BẢNG THANH TOÁN (Cấn trừ công nợ)
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

                // =======================================================================
                // 3. LOGIC MỚI: NẾU LÀ HOÀN TIỀN -> GHI NHẬN TỰ ĐỘNG VÀO BẢNG CHI PHÍ
                // =======================================================================
                if (loaiGiaoDich == "Hoàn tiền")
                {
                    // TỰ ĐỘNG KIỂM TRA VÀ SINH LOẠI CHI PHÍ LCP007 NẾU CHƯA CÓ
                    var loaiCpHoanTien = await _db.LoaiChiPhis.FirstOrDefaultAsync(l => l.MaLoaiCp == "LCP007" || l.TenLoaiCp.Contains("Hoàn tiền"));
                    string maLoaiCpHopLe;
                    if (loaiCpHoanTien != null)
                    {
                        maLoaiCpHopLe = loaiCpHoanTien.MaLoaiCp;
                    }
                    else
                    {
                        maLoaiCpHopLe = "LCP007";
                        _db.LoaiChiPhis.Add(new LoaiChiPhi { MaLoaiCp = maLoaiCpHopLe, TenLoaiCp = "Hoàn tiền hóa đơn" });
                        await _db.SaveChangesAsync(); // Tự sinh vào DB
                    }

                    var lastCp = await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync();

                    _db.ChiPhis.Add(new ChiPhi
                    {
                        MaChiPhi = MaHelper.Next("CP", lastCp),
                        MaLoaiCp = maLoaiCpHopLe,
                        MaNhanVien = nguoiThu,
                        MaPhong = hd.MaDatPhongNavigation?.DatPhongChiTiets.FirstOrDefault()?.MaPhong,
                        TenChiPhi = $"Hoàn tiền thừa/cọc cho khách (Hóa đơn: {maHoaDon})",
                        SoTien = Math.Abs(soTien), // Bảng Chi Phí luôn ghi nhận số DƯƠNG
                        NgayChiPhi = TimeHelper.GetVietnamTime(),
                        GhiChu = noiDung ?? $"Hệ thống tự động ghi nhận khoản chi do hoàn tiền."
                    });
                }
            }

            // 4. CẬP NHẬT TRẠNG THÁI HÓA ĐƠN
            decimal tongDaThuMoi = tongDaThuHienTai + soTien;
            bool daThuDu = tongDaThuMoi >= tongThanhToanThuc;

            if (daThuDu)
                hd.TrangThai = "Đã thanh toán";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var conLaiCuoiCung = tongThanhToanThuc - tongDaThuMoi;

            return daThuDu
                ? new ThongTinThanhToan(KetQuaThanhToan.HoanTat, tongDaThuMoi, conLaiCuoiCung, "Chốt sổ thành công!")
                : new ThongTinThanhToan(KetQuaThanhToan.GhiNhanChuaDu, tongDaThuMoi, conLaiCuoiCung, "Đã ghi nhận thanh toán, khách chưa thanh toán đủ.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            Logger.LogError("Lỗi", ex);
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Lỗi thanh toán: {ex.Message}");
        }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, "Không tìm thấy hoá đơn.");

            var tongThanhToan = hd.TongThanhToan ?? 0;
            var tongDaThu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            if (tongDaThu >= tongThanhToan && hd.TrangThai != "Đã thanh toán")
            {
                hd.TrangThai = "Đã thanh toán";
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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

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

            if (tongDaThu >= tongThanhToan && hd.TrangThai != "Đã thanh toán")
                hd.TrangThai = "Đã thanh toán";

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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var hd = await _db.HoaDons
                .Include(h => h.MaDatPhongNavigation)
                    .ThenInclude(d => d!.DatPhongChiTiets)
                .Include(h => h.HoaDonChiTiets)
                .Include(h => h.MaKhuyenMaiNavigation)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn");

            if (hd.MaDatPhongNavigation == null || string.IsNullOrWhiteSpace(hd.MaDatPhong))
                throw new InvalidOperationException("Hóa đơn không gắn với đặt phòng.");

            var dp = hd.MaDatPhongNavigation;
            if (dp.TrangThai == "Đã trả phòng")
            {
                await tx.RollbackAsync();
                return;
            }

            // 1. Tính toán lại bill chốt hạ lần cuối trước khi trả phòng
            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? TimeHelper.GetVietnamTime());

            // 2. TÍNH LẠI CÔNG NỢ: Cộng thêm Tiền Cọc vào Tổng Đã Thu
            var tongThuTrongLichSu = await _db.ThanhToans
                .Where(t => t.MaHoaDon == maHoaDon)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            var tienCoc = dp.TienCoc ?? 0;
            var tongDaThu = tongThuTrongLichSu + tienCoc;

            var tongThanhToanThuc = (hd.TongThanhToan ?? 0) + tienCoc;

            // 2. CHECK LOGIC: Bắt buộc nhân viên phải giải quyết tiền dư/nợ qua UI trước
            if (tongDaThu < tongThanhToanThuc)
                throw new InvalidOperationException($"Chưa thanh toán đủ (còn nợ {tongThanhToanThuc - tongDaThu:N0}đ). Vui lòng thanh toán trước khi trả phòng.");  
            if (tongDaThu > tongThanhToanThuc)
                throw new InvalidOperationException($"Khách đang dư {tongDaThu - tongThanhToanThuc:N0}đ. Vui lòng nhấn nút Thanh Toán, chọn 'Hoàn tiền' ở ComboBox để trả lại khách trước khi trả phòng.");

            // 3. Nếu mọi thứ đã khớp (TongDaThu == TongThanhToan), tiến hành trả phòng
            hd.TrangThai = "Đã thanh toán";

            foreach (var ct in dp.DatPhongChiTiets ?? [])
            {
                var p = await _db.Phongs.FindAsync(ct.MaPhong);
                if (p != null) p.MaTrangThaiPhong = "PTT03"; // Chuyển sang dọn dẹp
            }

            dp.TrangThai = "Đã trả phòng";

            if (!string.IsNullOrWhiteSpace(dp.MaKhachHang))
                await _khachHangSvc.NangHangAsync(dp.MaKhachHang, tongThanhToanThuc);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
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
            .Where(h => h.TrangThai == "Chưa thanh toán")
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
            // BẮT BUỘC PHẢI THÊM 2 DÒNG NÀY ĐỂ BẢN IN LẤY ĐƯỢC DATA
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
