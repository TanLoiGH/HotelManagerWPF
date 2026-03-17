using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

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
            NgayChiPhi = DateTime.Now,
            GhiChu = ghiChu
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<LoaiChiPhiDto>> GetLoaiChiPhiAsync()
        => await _db.LoaiChiPhis
            .Select(l => new LoaiChiPhiDto
            {
                MaLoaiCP = l.MaLoaiCp,
                TenLoaiCP = l.TenLoaiCp
            }).ToListAsync();
}