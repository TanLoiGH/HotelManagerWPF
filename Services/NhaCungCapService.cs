using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class NhaCungCapService
{
    private readonly QuanLyKhachSanContext _db;

    public NhaCungCapService(QuanLyKhachSanContext db) => _db = db;

    public async Task<List<NhaCungCap>> LayDanhSachAsync()
    {
        return await _db.NhaCungCaps
            .AsNoTracking()
            .OrderBy(n => n.TenNcc)
            .ToListAsync();
    }

    public async Task TaoMoiAsync(
        string tenNcc,
        string? dienThoai,
        string? email,
        string? diaChi,
        string? nguoiLienHe,
        bool isActive)
    {
        var lastMa = await _db.NhaCungCaps
            .OrderByDescending(n => n.MaNcc)
            .Select(n => n.MaNcc)
            .FirstOrDefaultAsync();

        _db.NhaCungCaps.Add(new NhaCungCap
        {
            MaNcc = MaHelper.Next("NCC", lastMa),
            TenNcc = tenNcc,
            DienThoai = dienThoai,
            Email = email,
            DiaChi = diaChi,
            NguoiLienHe = nguoiLienHe,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(
        string maNcc,
        string tenNcc,
        string? dienThoai,
        string? email,
        string? diaChi,
        string? nguoiLienHe,
        bool isActive)
    {
        var ncc = await _db.NhaCungCaps.FindAsync(maNcc);
        if (ncc == null) return;

        ncc.TenNcc = tenNcc;
        ncc.DienThoai = dienThoai;
        ncc.Email = email;
        ncc.DiaChi = diaChi;
        ncc.NguoiLienHe = nguoiLienHe;
        ncc.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaAsync(string maNcc)
    {
        var ncc = await _db.NhaCungCaps.FindAsync(maNcc);
        if (ncc == null) return false;

        bool coLienQuan = await _db.TienNghis.AnyAsync(t => t.MaNcc == ncc.MaNcc)
                       || await _db.ChiPhis.AnyAsync(c => c.MaNcc == ncc.MaNcc);

        if (coLienQuan)
        {
            ncc.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.NhaCungCaps.Remove(ncc);
        await _db.SaveChangesAsync();
        return false;
    }
}
