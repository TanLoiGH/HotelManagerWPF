using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class HoaDonService : IHoaDonService
{
    private readonly QuanLyKhachSanContext _db;
    private readonly IKhachHangService _khachHangSvc;

    // Giữ nguyên Semaphore để đảm bảo an toàn Threading trong WPF
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    // Senior Note: Khai báo Hằng số để tránh sai sót Magic String
    private const string LOAI_GIAO_DICH_THANH_TOAN = "Thanh toán cuối";
    private const string LOAI_GIAO_DICH_HOAN_TIEN = "Hoàn tiền";
    private const string MA_LOAI_CP_HOAN_TIEN = "LCP007";

    public HoaDonService(QuanLyKhachSanContext db, IKhachHangService khachHangSvc)
    {
        _db = db;
        _khachHangSvc = khachHangSvc;
    }

    #region IMPLEMENT CÁC HÀM CÒN THIẾU MÀ INTERFACE YÊU CẦU

    public async Task<HoaDon?> LayHoaDonThanhToanAsync(string maHoaDon) => await LayHoaDonChiTietAsync(maHoaDon);
    public async Task<HoaDon?> LayHoaDonDeInAsync(string maHoaDon) => await LayHoaDonChiTietAsync(maHoaDon);

    public async Task<bool> ThanhToanAsync(string maHoaDon, decimal soTien, string maPTTT, string nguoiThu,
        string loaiGiaoDich = LOAI_GIAO_DICH_THANH_TOAN, string? noiDung = null)
    {
        var res = await ThanhToanVaTraKetQuaAsync(maHoaDon, soTien, maPTTT, nguoiThu, loaiGiaoDich, noiDung);
        return res.KetQua == KetQuaThanhToan.HoanTat;
    }

    public async Task<int> CapNhatVatChoHoaDonDangMoAsync(decimal vatPercent)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hds = await _db.HoaDons.Where(h => h.TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan).ToListAsync();
            foreach (var h in hds) h.Vat = vatPercent;

            return await _db.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> CapNhatKhuyenMaiAsync(string maHoaDon, string? maKhuyenMai)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null) return false;

            // Cập nhật mã khuyến mãi mới
            hd.MaKhuyenMai = string.IsNullOrWhiteSpace(maKhuyenMai) ? null : maKhuyenMai;

            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ThongTinThanhToan> DongBoTrangThaiThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
            if (hd == null)
                return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Không tìm thấy hóa đơn {maHoaDon}");

            // ✅ SENIOR FIX: Cập nhật trạng thái hóa đơn thành Đã thanh toán và lưu lại!
            hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
            await _db.SaveChangesAsync();

            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, 0, 0, "Chốt hóa đơn thành công!");
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi đồng bộ thanh toán", ex);
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0, $"Lỗi hệ thống: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region CÁC HÀM QUERIES

    public async Task<List<HoaDon>> LayHoaDonsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<HoaDon?> LayHoaDonChiTietAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Senior Refactor: Định dạng lại LINQ để dễ đọc, dễ bảo trì
            return await _db.HoaDons
                .AsNoTracking()
                .Include(h => h.MaDatPhongNavigation)
                .ThenInclude(d => d!.MaKhachHangNavigation)
                .Include(h => h.HoaDonChiTiets)
                .ThenInclude(ct => ct.DatPhongChiTiet)
                .Include(h => h.DichVuChiTiets)
                .ThenInclude(dv => dv.MaDichVuNavigation)
                .Include(h => h.MaKhuyenMaiNavigation)
                .Include(h => h.ThanhToans)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<ThanhToan>> LayLichSuThanhToanAsync(string maHoaDon)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.ThanhToans
                .AsNoTracking()
                .Include(t => t.MaPtttNavigation)
                .Where(t => t.MaHoaDon == maHoaDon)
                .OrderBy(t => t.NgayThanhToan)
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<PhuongThucThanhToanDto>> LayDanhSachPhuongThucThanhToanAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _db.PhuongThucThanhToans
                .Select(p => new PhuongThucThanhToanDto { MaPTTT = p.MaPttt, TenPhuongThuc = p.TenPhuongThuc })
                .ToListAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region NGHIỆP VỤ CHÍNH

    public async Task<HoaDon> XuatHoaDonAsync(string maDatPhong, string maNhanVien, string? maKhuyenMai = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var dp = await _db.DatPhongs.FindAsync(maDatPhong)
                     ?? throw new KeyNotFoundException($"Không tìm thấy đơn đặt phòng {maDatPhong}");

            var chiTiets = await _db.DatPhongChiTiets.Where(c => c.MaDatPhong == maDatPhong).ToListAsync();
            decimal tienPhong = chiTiets.Sum(ct => ct.DonGia * TinhToanService.ThoiGianLuuTru(ct.NgayNhan, ct.NgayTra));

            var km = await _db.KhuyenMais.FirstOrDefaultAsync(k => k.MaKhuyenMai == maKhuyenMai && k.IsActive == true);

            // Tối ưu: Chỉ gọi cấu hình VAT 1 lần
            decimal vatHienTai = CaiDatService.Load().VatPercent;

            var res = TinhToanService.TinhToanToanBo(
                tienPhong: tienPhong,
                tienDichVu: 0,
                vatPercent: vatHienTai,
                giaTriKm: km?.GiaTriKm ?? 0,
                loaiKm: km?.LoaiKhuyenMai ?? "",
                tienCoc: dp.TienCoc ?? 0,
                tongDaThuLichSu: 0);

            var lastMa = await _db.HoaDons.OrderByDescending(h => h.MaHoaDon).Select(h => h.MaHoaDon)
                .FirstOrDefaultAsync();
            var hd = new HoaDon
            {
                MaHoaDon = MaHelper.Next("HD", lastMa),
                MaDatPhong = maDatPhong,
                MaNhanVien = maNhanVien,
                NgayLap = TimeHelper.GetVietnamTime(),
                TienPhong = res.TienPhong,
                TienDichVu = 0,
                Vat = vatHienTai,
                MaKhuyenMai = maKhuyenMai,
                TongThanhToan = res.TongThanhToan,
                TrangThai = HoaDonTrangThaiTexts.ChuaThanhToan
            };

            _db.HoaDons.Add(hd);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return hd;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ThongTinThanhToan> ThanhToanVaTraKetQuaAsync(string maHoaDon, decimal soTien, string maPTTT,
        string nguoiThu, string loaiGiaoDich = LOAI_GIAO_DICH_THANH_TOAN, string? noiDung = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var hd = await _db.HoaDons
                         .Include(h => h.MaDatPhongNavigation)
                         .ThenInclude(dp => dp.MaKhachHangNavigation) // ✅ BỔ SUNG: Kéo Data Khách hàng lên để cộng điểm
                         .Include(h => h.MaDatPhongNavigation)
                         .ThenInclude(dp => dp.DatPhongChiTiets) // (Dành cho logic Hoàn tiền của bạn)
                         .Include(h => h.MaKhuyenMaiNavigation)
                         .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                     ?? throw new KeyNotFoundException($"Không tìm thấy hóa đơn {maHoaDon}");

            if (loaiGiaoDich == LOAI_GIAO_DICH_HOAN_TIEN && soTien > 0)
                soTien = -soTien; // Đảo dấu nếu là hoàn tiền

            string nextTt = MaHelper.Next("TT",
                await _db.ThanhToans.OrderByDescending(t => t.MaThanhToan).Select(t => t.MaThanhToan)
                    .FirstOrDefaultAsync());

            _db.ThanhToans.Add(new ThanhToan
            {
                MaThanhToan = nextTt,
                MaHoaDon = maHoaDon,
                MaPttt = maPTTT,
                SoTien = soTien,
                LoaiGiaoDich = loaiGiaoDich,
                NgayThanhToan = TimeHelper.GetVietnamTime(),
                NguoiThu = nguoiThu,
                NoiDung = noiDung
            });

            if (loaiGiaoDich == LOAI_GIAO_DICH_HOAN_TIEN)
            {
                string? maPhongDauTien = hd.MaDatPhongNavigation?.DatPhongChiTiets.FirstOrDefault()?.MaPhong;
                await GhiNhanChiPhiHoanTienAsync(maHoaDon, Math.Abs(soTien), nguoiThu, maPhongDauTien);
            }

            // =================================================================================
            // ✅ TÍCH LŨY & NÂNG HẠNG KHÁCH HÀNG (CHỈ THÊM ĐOẠN NÀY VÀO CODE CỦA BẠN)
            // =================================================================================
            if (soTien > 0 && loaiGiaoDich != LOAI_GIAO_DICH_HOAN_TIEN)
            {
                var khachHang = hd.MaDatPhongNavigation?.MaKhachHangNavigation;
                if (khachHang != null)
                {
                    // 1. Cộng dồn tiền vào tổng tích lũy
                    khachHang.TongTichLuy = (khachHang.TongTichLuy ?? 0) + soTien;

                    // 2. Kiểm tra xem số tiền mới có đủ lên cấp (VIP) không
                    var hangKhachMoi = await _db.LoaiKhaches
                        .AsNoTracking()
                        .Where(l => l.IsActive == true && khachHang.TongTichLuy >= l.NguongTichLuy)
                        .OrderByDescending(l => l.NguongTichLuy)
                        .FirstOrDefaultAsync();

                    // 3. Nếu đủ cấp mới -> Tự động Update hạng
                    if (hangKhachMoi != null && hangKhachMoi.MaLoaiKhach != khachHang.MaLoaiKhach)
                    {
                        khachHang.MaLoaiKhach = hangKhachMoi.MaLoaiKhach;
                    }
                }
            }
            // =================================================================================

            var lichSu = await _db.ThanhToans.Where(t => t.MaHoaDon == maHoaDon).SumAsync(t => (decimal?)t.SoTien) ?? 0;

            var res = TinhToanService.TinhToanToanBo(
                tienPhong: hd.TienPhong ?? 0,
                tienDichVu: hd.TienDichVu ?? 0,
                vatPercent: hd.Vat ?? 0,
                giaTriKm: hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0,
                loaiKm: hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "",
                tienCoc: hd.MaDatPhongNavigation?.TienCoc ?? 0,
                tongDaThuLichSu: lichSu);

            if (res.ConLai <= 0) hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, res.TongThanhToan - res.ConLai, res.ConLai,
                "Thanh toán thành công");
        }
        catch (Exception ex)
        {
            return new ThongTinThanhToan(KetQuaThanhToan.TuChoi, 0, 0,
                $"Lỗi thanh toán: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task TraPhongAsync(string maHoaDon, string maNhanVien, DateTime? thoiDiem = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var hd = await _db.HoaDons
                         .Include(h => h.MaKhuyenMaiNavigation) // Bổ sung Include này để Fix Bug mất khuyến mãi
                         .Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                         .Include(h => h.HoaDonChiTiets)
                         .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                     ?? throw new KeyNotFoundException("Không tìm thấy thông tin hóa đơn để trả phòng.");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiem ?? TimeHelper.GetVietnamTime());

            if (hd.MaDatPhongNavigation != null)
            {
                foreach (var ct in hd.MaDatPhongNavigation.DatPhongChiTiets)
                {
                    var p = await _db.Phongs.FindAsync(ct.MaPhong);
                    if (p != null) p.MaTrangThaiPhong = PhongTrangThaiCodes.DonDep;
                }

                hd.MaDatPhongNavigation.TrangThai = DatPhongTrangThaiTexts.DaTraPhong;
            }

            hd.TrangThai = HoaDonTrangThaiTexts.DaThanhToan;
            await _db.SaveChangesAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ThongTinThanhToan> CapNhatTienPhongKhiTraSomAsync(string maHoaDon, DateTime thoiDiemTraPhong)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Senior Fix: Bắt buộc phải Include Khuyến Mãi, nếu không sẽ bị reset mất khuyến mãi
            var hd = await _db.HoaDons
                         .Include(h => h.MaKhuyenMaiNavigation)
                         .Include(h => h.MaDatPhongNavigation).ThenInclude(d => d!.DatPhongChiTiets)
                         .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon)
                     ?? throw new KeyNotFoundException($"Không tìm thấy hóa đơn {maHoaDon}");

            await CapNhatTienPhongTheoThoiDiemTraPhongAsync(hd, thoiDiemTraPhong);
            return new ThongTinThanhToan(KetQuaThanhToan.HoanTat, 0, 0, "Cập nhật thành công");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task CapNhatTienPhongTheoThoiDiemTraPhongAsync(HoaDon hd, DateTime thoiDiem)
    {
        decimal tienPhong = 0;
        foreach (var ct in hd.MaDatPhongNavigation!.DatPhongChiTiets)
        {
            DateTime thoiDiemTinh = thoiDiem < ct.NgayTra ? thoiDiem : ct.NgayTra;
            int soDem = TinhToanService.ThoiGianLuuTru(ct.NgayNhan, thoiDiemTinh);
            tienPhong += ct.DonGia * soDem;

            if (thoiDiem < ct.NgayTra) ct.NgayTra = thoiDiem;
        }

        // Senior Fix: Lấy đúng khuyến mãi từ Object thay vì hardcode 0 và ""
        decimal kmGiaTri = hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0;
        string kmLoai = hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "";

        var res = TinhToanService.TinhToanToanBo(
            tienPhong: tienPhong,
            tienDichVu: hd.TienDichVu ?? 0,
            vatPercent: hd.Vat ?? 0,
            giaTriKm: kmGiaTri,
            loaiKm: kmLoai,
            tienCoc: hd.MaDatPhongNavigation?.TienCoc ?? 0,
            tongDaThuLichSu: 0);

        hd.TienPhong = res.TienPhong;
        hd.TongThanhToan = res.TongThanhToan;
        await _db.SaveChangesAsync();
    }

    public async Task<int> DamBaoHoaDonChiTietAsync(string maHoaDon)
    {
        // TODO: Chờ Implement nếu có nghiệp vụ quét lại chi tiết
        return await Task.FromResult(0);
    }

    public async Task HuyHoaDonAsync(string maHoaDon)
    {
        // TODO: Cập nhật trạng thái hóa đơn thành Đã Hủy
    }

    private async Task GhiNhanChiPhiHoanTienAsync(string maHoaDon, decimal soTien, string maNhanVien, string? maPhong)
    {
        var loaiCp = await _db.LoaiChiPhis.FirstOrDefaultAsync(l =>
            l.MaLoaiCp == MA_LOAI_CP_HOAN_TIEN || l.TenLoaiCp.Contains("Hoàn tiền"));
        if (loaiCp == null)
        {
            loaiCp = new LoaiChiPhi { MaLoaiCp = MA_LOAI_CP_HOAN_TIEN, TenLoaiCp = "Hoàn tiền hóa đơn" };
            _db.LoaiChiPhis.Add(loaiCp);
            await _db.SaveChangesAsync();
        }

        string nextCp = MaHelper.Next("CP",
            await _db.ChiPhis.OrderByDescending(c => c.MaChiPhi).Select(c => c.MaChiPhi).FirstOrDefaultAsync());

        _db.ChiPhis.Add(new ChiPhi
        {
            MaChiPhi = nextCp,
            MaLoaiCp = loaiCp.MaLoaiCp,
            MaNhanVien = maNhanVien,
            MaPhong = maPhong,
            TenChiPhi = $"Hoàn tiền thừa khách (HĐ: {maHoaDon})",
            SoTien = soTien,
            NgayChiPhi = TimeHelper.GetVietnamTime(),
            GhiChu = "Tự động ghi nhận khi hoàn tiền thừa cho khách."
        });
    }

    #endregion
}