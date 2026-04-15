using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record ChiPhiDanhSachItem(
    string MaChiPhi,
    string TenChiPhi,
    string TenLoaiCp,
    decimal SoTien,
    DateTime? NgayChiPhi,
    string TenNcc,
    string MaPhong,
    string GhiChu);

public class ChiPhiService
{
    private readonly QuanLyKhachSanContext _db;
    public ChiPhiService(QuanLyKhachSanContext db) => _db = db;

    public async Task GhiChiPhiAsync(
        string maLoaiCP, string tenChiPhi, decimal soTien,
        string? maNhanVien = null,
        string? maNCC = null,
        string? maPhong = null,
        string? ghiChu = null)
    {
        var lastMa = await _db.ChiPhis
            .OrderByDescending(c => c.MaChiPhi)
            .Select(c => c.MaChiPhi)
            .FirstOrDefaultAsync();

        _db.ChiPhis.Add(new ChiPhi
        {
            MaChiPhi = MaHelper.Next("CP", lastMa),
            MaLoaiCp = maLoaiCP,
            MaNhanVien = maNhanVien,
            MaNcc = maNCC,
            MaPhong = maPhong,
            TenChiPhi = tenChiPhi,
            SoTien = soTien,
            NgayChiPhi = TimeHelper.GetVietnamTime(),
            GhiChu = ghiChu
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<LoaiChiPhiDto>> GetLoaiChiPhiAsync()
        => await _db.LoaiChiPhis
            .AsNoTracking()
            .Select(l => new LoaiChiPhiDto
            {
                MaLoaiCP = l.MaLoaiCp,
                TenLoaiCP = l.TenLoaiCp
            }).ToListAsync();

    public async Task<List<LoaiChiPhi>> LayDanhSachLoaiChiPhiAsync()
    {
        return await _db.LoaiChiPhis
            .AsNoTracking()
            .Include(l => l.ChiPhis)
            .OrderBy(l => l.MaLoaiCp)
            .ToListAsync();
    }

    public async Task TaoMoiLoaiChiPhiAsync(string tenLoaiCp, bool isActive = true)
    {
        var lastMa = await _db.LoaiChiPhis
            .OrderByDescending(l => l.MaLoaiCp)
            .Select(l => l.MaLoaiCp)
            .FirstOrDefaultAsync();

        _db.LoaiChiPhis.Add(new LoaiChiPhi
        {
            MaLoaiCp = MaHelper.Next("LCP", lastMa),
            TenLoaiCp = tenLoaiCp,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatLoaiChiPhiAsync(string maLoaiCp, string tenLoaiCp, bool? isActive = null)
    {
        var item = await _db.LoaiChiPhis.FindAsync(maLoaiCp);
        if (item == null) return;

        item.TenLoaiCp = tenLoaiCp;
        if (isActive.HasValue) item.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaLoaiChiPhiAsync(string maLoaiCp)
    {
        var item = await _db.LoaiChiPhis.FindAsync(maLoaiCp);
        if (item == null) return false;

        bool coPhieu = await _db.ChiPhis.AnyAsync(c => c.MaLoaiCp == item.MaLoaiCp);
        if (coPhieu)
        {
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.LoaiChiPhis.Remove(item);
        await _db.SaveChangesAsync();
        return false;
    }

    public async Task<List<ChiPhiDanhSachItem>> LayChiPhiTheoNgayAsync(DateTime tu, DateTime den)
    {
        return await _db.ChiPhis
            .AsNoTracking()
            .Include(c => c.MaLoaiCpNavigation)
            .Include(c => c.MaNccNavigation)
            .Where(c => c.NgayChiPhi >= tu && c.NgayChiPhi <= den)
            .OrderByDescending(c => c.NgayChiPhi)
            .Select(c => new ChiPhiDanhSachItem(
                c.MaChiPhi,
                c.TenChiPhi,
                c.MaLoaiCpNavigation.TenLoaiCp,
                c.SoTien,
                c.NgayChiPhi,
                c.MaNccNavigation != null ? c.MaNccNavigation.TenNcc : "",
                c.MaPhong ?? "",
                c.GhiChu ?? ""))
            .ToListAsync();
    }
}




