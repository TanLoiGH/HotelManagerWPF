using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record PhongQuanTriItem(
    string MaPhong,
    string MaLoaiPhong,
    string TenLoaiPhong,
    string MaTrangThaiPhong,
    string TenTrangThai,
    bool IsUsed);

public record TienNghiPhongQuanTriItem(
    string MaTienNghi,
    string TenTienNghi,
    string MaTrangThai);

public class RoomService
{
    private readonly QuanLyKhachSanContext _db;
    public RoomService(QuanLyKhachSanContext db) => _db = db;

    // Lấy toàn bộ phòng kèm loại phòng và trạng thái để hiển thị sơ đồ.
    public async Task<List<Phong>> LayDanhSachPhongChiTietAsync()
    {
        return await _db.Phongs
            .AsNoTracking()
            .Include(p => p.MaLoaiPhongNavigation)
            .Include(p => p.MaTrangThaiPhongNavigation)
            .OrderBy(p => p.MaPhong)
            .ToListAsync();
    }

    // Lấy các booking còn hiệu lực để gắn tên khách lên thẻ phòng.
    public async Task<List<DatPhongChiTiet>> LayChiTietDatPhongDangHoatDongAsync()
    {
        return await _db.DatPhongChiTiets
            .AsNoTracking()
            .Include(c => c.MaDatPhongNavigation)
                .ThenInclude(dp => dp.MaKhachHangNavigation)
            .Where(c => c.MaDatPhongNavigation.TrangThai == "Đang ở" ||
                        c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng")
            .ToListAsync();
    }

    // Lấy booking chờ nhận phòng gần nhất theo mã phòng.
    public async Task<DatPhongChiTiet?> LayDatPhongChoNhanTheoPhongAsync(string maPhong)
    {
        return await _db.DatPhongChiTiets
            .AsNoTracking()
            .Include(c => c.MaDatPhongNavigation)
                .ThenInclude(dp => dp.MaKhachHangNavigation)
            .Where(c => c.MaPhong == maPhong &&
                        c.MaDatPhongNavigation.TrangThai == "Chờ nhận phòng")
            .OrderByDescending(c => c.MaDatPhongNavigation.NgayDat)
            .FirstOrDefaultAsync();
    }

    // Lấy danh sách tiện nghi của phòng để hiển thị panel chi tiết.
    public async Task<List<TienNghiPhong>> LayTienNghiPhongAsync(string maPhong)
    {
        return await _db.TienNghiPhongs
            .AsNoTracking()
            .Include(t => t.MaTienNghiNavigation)
            .Where(t => t.MaPhong == maPhong)
            .OrderBy(t => EF.Functions.Collate(
                                t.MaTienNghiNavigation.TenTienNghi,
                                "Vietnamese_CI_AI"))
            .ToListAsync();
    }

    public async Task<List<string>> LayDanhSachMaPhongAsync()
    {
        return await _db.Phongs
            .AsNoTracking()
            .OrderBy(p => p.MaPhong)
            .Select(p => p.MaPhong)
            .ToListAsync();
    }

    public async Task<List<LoaiPhong>> LayLoaiPhongAsync()
        => await _db.LoaiPhongs
            .AsNoTracking()
            .OrderBy(l => l.TenLoaiPhong)
            .ToListAsync();

    public async Task<List<PhongTrangThai>> LayTrangThaiPhongAsync()
        => await _db.PhongTrangThais
            .AsNoTracking()
            .OrderByDescending(t => t.TenTrangThai)
            .ToListAsync();

    public async Task<List<PhongQuanTriItem>> LayDanhSachPhongQuanTriAsync()
    {
        return await _db.Phongs
            .AsNoTracking()
            .Include(p => p.MaLoaiPhongNavigation)
            .Include(p => p.MaTrangThaiPhongNavigation)
            .OrderBy(p => p.MaPhong)
            .Select(p => new PhongQuanTriItem(
                p.MaPhong,
                p.MaLoaiPhong,
                p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                p.MaTrangThaiPhong ?? "PTT01",
                p.MaTrangThaiPhongNavigation != null ? (p.MaTrangThaiPhongNavigation.TenTrangThai ?? "") : "",
                p.DatPhongChiTiets.Any() || p.TienNghiPhongs.Any() || p.ChiPhis.Any()))
            .ToListAsync();
    }

    public async Task<List<TienNghiPhongQuanTriItem>> LayTienNghiPhongQuanTriAsync(string maPhong)
    {
        return await _db.TienNghiPhongs
            .AsNoTracking()
            .Include(t => t.MaTienNghiNavigation)
            .Where(t => t.MaPhong == maPhong)
            .OrderBy(t => t.MaTienNghiNavigation.TenTienNghi)
            .Select(t => new TienNghiPhongQuanTriItem(
                t.MaTienNghi,
                t.MaTienNghiNavigation.TenTienNghi,
                t.MaTrangThai ?? "TNTT01"))
            .ToListAsync();
    }

    public async Task CapNhatDanhSachTienNghiPhongAsync(string maPhong, List<string> selectedMaTienNghi)
    {
        var current = await _db.TienNghiPhongs
            .Where(t => t.MaPhong == maPhong)
            .Select(t => t.MaTienNghi)
            .ToListAsync();

        var toAdd = selectedMaTienNghi.Except(current).ToList();
        var toRemove = current.Except(selectedMaTienNghi).ToList();

        foreach (var id in toAdd)
        {
            _db.TienNghiPhongs.Add(new TienNghiPhong
            {
                MaPhong = maPhong,
                MaTienNghi = id,
                MaTrangThai = "TNTT01"
            });
        }

        if (toRemove.Count > 0)
        {
            var removeItems = await _db.TienNghiPhongs
                .Where(t => t.MaPhong == maPhong && toRemove.Contains(t.MaTienNghi))
                .ToListAsync();
            _db.TienNghiPhongs.RemoveRange(removeItems);
        }

        await _db.SaveChangesAsync();
    }

    public async Task GoTienNghiKhoiPhongAsync(string maPhong, string maTienNghi)
    {
        var item = await _db.TienNghiPhongs.FindAsync(maPhong, maTienNghi);
        if (item == null) return;
        _db.TienNghiPhongs.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task TaoPhongAsync(string maPhong, string maLoaiPhong, string maTrangThai)
    {
        bool exists = await _db.Phongs.AnyAsync(p => p.MaPhong == maPhong);
        if (exists)
            throw new InvalidOperationException("Mã phòng đã tồn tại.");

        _db.Phongs.Add(new Phong
        {
            MaPhong = maPhong,
            MaLoaiPhong = maLoaiPhong,
            MaTrangThaiPhong = maTrangThai
        });
        await _db.SaveChangesAsync();
    }

    public async Task CapNhatPhongAsync(string maPhong, string maLoaiPhong, string maTrangThai)
    {
        var p = await _db.Phongs.FindAsync(maPhong);
        if (p == null) return;

        bool coDangO = await _db.DatPhongChiTiets
            .AnyAsync(c => c.MaPhong == maPhong && c.MaDatPhongNavigation!.TrangThai == "Đang ở");

        bool coChoNhan = await _db.DatPhongChiTiets
            .AnyAsync(c => c.MaPhong == maPhong && c.MaDatPhongNavigation!.TrangThai == "Chờ nhận phòng");

        if (coDangO && maTrangThai != "PTT02")
            throw new InvalidOperationException("Phong dang o, khong the chuyen sang trang thai khac.");

        if (!coDangO && coChoNhan && maTrangThai != "PTT05")
            throw new InvalidOperationException("Phong dang duoc dat, chi co the de trang thai 'Da dat'.");

        p.MaLoaiPhong = maLoaiPhong;
        p.MaTrangThaiPhong = maTrangThai;
        await _db.SaveChangesAsync();
    }

    public async Task XoaPhongAsync(string maPhong)
    {
        bool used = await _db.Phongs
            .Where(p => p.MaPhong == maPhong)
            .AnyAsync(p => p.DatPhongChiTiets.Any() || p.TienNghiPhongs.Any() || p.ChiPhis.Any());

        if (used)
            throw new InvalidOperationException("Phòng đã phát sinh dữ liệu, không thể xóa.");

        var p = await _db.Phongs.FindAsync(maPhong);
        if (p == null) return;
        _db.Phongs.Remove(p);
        await _db.SaveChangesAsync();
    }
}
