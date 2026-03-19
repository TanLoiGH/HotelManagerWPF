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
        List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)> rooms)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        foreach (var (maPhong, ngayNhan, ngayTra) in rooms)
            await EnsurePhongAvailableAsync(maPhong, ngayNhan, ngayTra);

        var lastMa = await _db.DatPhongs
            .OrderByDescending(d => d.MaDatPhong)
            .Select(d => d.MaDatPhong)
            .FirstOrDefaultAsync();

        var dp = new DatPhong
        {
            MaDatPhong = CodeHelper.Next("DP", lastMa),
            MaKhachHang = maKhachHang,
            NgayDat = DateTime.Now,
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
    }

    public async Task HuyDatPhongAsync(string maDatPhong, string lyDo)
    {
        var dp = await _db.DatPhongs
            .Include(d => d.DatPhongChiTiets)
            .FirstOrDefaultAsync(d => d.MaDatPhong == maDatPhong)
            ?? throw new KeyNotFoundException("Không tìm thấy đặt phòng");

        dp.TrangThai = "Đã hủy";
        foreach (var ct in dp.DatPhongChiTiets)
        {
            var phong = await _db.Phongs.FindAsync(ct.MaPhong);
            if (phong != null) phong.MaTrangThaiPhong = "PTT01";
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
}




