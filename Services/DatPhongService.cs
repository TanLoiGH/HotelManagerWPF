using System.Data;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DatPhongService
{
    private readonly QuanLyKhachSanContext _db;
    public DatPhongService(QuanLyKhachSanContext db) => _db = db;

    private async Task TaoHoaDonNeuChuaCoAsync(string maDatPhong, string maNhanVien)
    {
        bool hdExist = await _db.HoaDons
            .AnyAsync(h => h.MaDatPhong == maDatPhong && h.TrangThai != "Đã hủy");

        if (hdExist) return;

        var chiTiets = await _db.DatPhongChiTiets
            .Where(c => c.MaDatPhong == maDatPhong)
            .ToListAsync();

        if (chiTiets.Count == 0)
            throw new InvalidOperationException("Không có phòng nào trong đặt phòng.");

        var dp = await _db.DatPhongs.FindAsync(maDatPhong);
        decimal tienCoc = dp?.TienCoc ?? 0;

        decimal tienPhong = 0;
        foreach (var ct in chiTiets)
        {
            int soDem = TinhToanHoaDonService.TinhSoDem(ct.NgayNhan, ct.NgayTra);
            tienPhong += ct.DonGia * soDem;
        }

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
            NgayLap = DateTime.Now,
            TienPhong = tienPhong,
            TienDichVu = 0,
            Vat = vatPercent,
            MaKhuyenMai = null,
            TongThanhToan = TinhToanHoaDonService.TinhTongThanhToan(tienPhong, 0, vatPercent, tienCoc, 0),
            TrangThai = "Chưa thanh toán"
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
            throw new InvalidOperationException(
                $"Phòng {maPhong} đã có đặt phòng trong khoảng thời gian này.");
    }

    public async Task<DatPhong> TaoDatPhongAsync(
        string maKhachHang,
        List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms,
        string maNhanVien = null,
        decimal tienCoc = 0,
        int soNguoi = 1)
    {
        foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
        {
            if (ngayNhan.Date < DateTime.Today)
                throw new InvalidOperationException($"Ngay nhan cua phong {maPhong} khong duoc o qua khu.");

            if (ngayTra.Date < ngayNhan.Date)
                throw new InvalidOperationException($"Ngay tra cua phong {maPhong} phai sau hoac bang ngay nhan.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        int tongSucChua = 0;
        foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
        {
            await EnsurePhongAvailableAsync(maPhong, ngayNhan, ngayTra);

            var phong = await _db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .FirstAsync(p => p.MaPhong == maPhong);

            tongSucChua += phong.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0;
        }

        if (soNguoi > tongSucChua)
            throw new InvalidOperationException(
                $"Tổng sức chứa các phòng ({tongSucChua} người) không đủ cho {soNguoi} người.");

        var lastMa = await _db.DatPhongs
            .OrderByDescending(d => d.MaDatPhong)
            .Select(d => d.MaDatPhong)
            .FirstOrDefaultAsync();

        var dp = new DatPhong
        {
            MaDatPhong = MaHelper.Next("DP", lastMa),
            MaKhachHang = maKhachHang,
            MaNhanVien = maNhanVien,
            NgayDat = DateTime.Now,
            TienCoc = tienCoc,
            TrangThai = "Chờ nhận phòng"
        };
        _db.DatPhongs.Add(dp);

        foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
        {
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

            phong.MaTrangThaiPhong = "PTT05";
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return dp;
    }

    public async Task CheckInAsync(string maDatPhong, string maNhanVienLeTan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var dp = await _db.DatPhongs
            .Include(d => d.DatPhongChiTiets)
            .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
            ?? throw new KeyNotFoundException("Không tìm thấy đặt phòng");

        if (dp.TrangThai != "Chờ nhận phòng")
            throw new InvalidOperationException($"Trạng thái hiện tại: {dp.TrangThai}");

        dp.TrangThai = "Đang ở";

        foreach (var ct in dp.DatPhongChiTiets)
        {
            ct.MaNhanVien = maNhanVienLeTan;

            var phong = await _db.Phongs.FindAsync(ct.MaPhong);
            if (phong != null) phong.MaTrangThaiPhong = "PTT02";
        }

        await _db.SaveChangesAsync();

        await TaoHoaDonNeuChuaCoAsync(maDatPhong, maNhanVienLeTan);

        await tx.CommitAsync();
    }

    public async Task HuyDatPhongAsync(string maDatPhong, string lyDo, decimal tienHoanTra = 0)
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

        foreach (var ct in dp.DatPhongChiTiets)
        {
            var phong = await _db.Phongs.FindAsync(ct.MaPhong);
            if (phong != null)
            {
                // Kiểm tra xem phòng có đang được sử dụng bởi đặt phòng khác không
                bool isOccupied = await _db.DatPhongChiTiets
                    .AnyAsync(c => c.MaPhong == ct.MaPhong && c.MaDatPhong != maDatPhong &&
                                  c.MaDatPhongNavigation!.TrangThai == "Đang ở");

                if (!isOccupied)
                {
                    phong.MaTrangThaiPhong = "PTT01"; // Trống
                }
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task GiaHanAsync(string maDatPhong, string maPhong, DateTime ngayTraMoi)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var ct = await _db.DatPhongChiTiets.FindAsync(maDatPhong, maPhong)
            ?? throw new KeyNotFoundException("Không tìm thấy chi tiết đặt phòng");

        if (ngayTraMoi <= ct.NgayNhan)
            throw new ArgumentException("Ngày trả mới phải sau ngày nhận");

        await EnsurePhongAvailableAsync(maPhong, ct.NgayNhan, ngayTraMoi, maDatPhong);

        ct.NgayTra = ngayTraMoi;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task DoiPhongAsync(
        string maDatPhong, string maPhongCu, string maPhongMoi,
        DateTime ngayNhan, DateTime ngayTra)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var ctCu = await _db.DatPhongChiTiets.FindAsync(maDatPhong, maPhongCu)
            ?? throw new KeyNotFoundException("Không tìm thấy phòng cũ");

        await EnsurePhongAvailableAsync(maPhongMoi, ngayNhan, ngayTra, maDatPhong);

        var phongMoi = await _db.Phongs
            .Include(p => p.MaLoaiPhongNavigation)
            .FirstAsync(p => p.MaPhong == maPhongMoi);

        var phongCu = await _db.Phongs.FindAsync(maPhongCu);
        if (phongCu != null) phongCu.MaTrangThaiPhong = "PTT03";

        _db.DatPhongChiTiets.Remove(ctCu);
        _db.DatPhongChiTiets.Add(new DatPhongChiTiet
        {
            MaDatPhong = maDatPhong,
            MaPhong = maPhongMoi,
            NgayNhan = ngayNhan,
            NgayTra = ngayTra,
            DonGia = phongMoi.MaLoaiPhongNavigation.GiaPhong,
            MaNhanVien = ctCu.MaNhanVien
        });

        phongMoi.MaTrangThaiPhong = "PTT02";
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<object> GetDepositSummaryAsync()
    {
        var all = await _db.DatPhongs.ToListAsync();
        return new
        {
            TongCocHienTai = all.Where(d => d.TrangThai == "Chờ nhận phòng").Sum(d => d.TienCoc ?? 0),
            TongCocDaKhauTru = all.Where(d => d.TrangThai == "Đã trả phòng").Sum(d => d.TienCoc ?? 0),
            TongCocBiMat = all.Where(d => d.TrangThai == "Đã hủy - Mất cọc").Sum(d => d.TienCoc ?? 0),
            SoLuongDatPhongCoCoc = all.Count(d => d.TienCoc > 0)
        };
    }
}
