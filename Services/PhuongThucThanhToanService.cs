using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record PhuongThucThanhToanItem(string MaPttt, string TenPhuongThuc, int SoGiaoDich);

public class PhuongThucThanhToanService
{
    private readonly QuanLyKhachSanContext _db;
    public PhuongThucThanhToanService(QuanLyKhachSanContext db) => _db = db;

    private static string NextPttt(string? lastMa)
    {
        const string prefix = "PTTT";
        if (string.IsNullOrWhiteSpace(lastMa) || !lastMa.StartsWith(prefix))
            return "PTTT01";

        var numeric = lastMa[prefix.Length..];
        int pad = Math.Max(2, numeric.Length);

        if (int.TryParse(numeric, out int n))
            return $"{prefix}{(n + 1).ToString($"D{pad}")}";

        return "PTTT01";
    }

    public async Task<List<PhuongThucThanhToanItem>> LayDanhSachAsync()
    {
        return await _db.PhuongThucThanhToans
            .AsNoTracking()
            .OrderBy(p => p.MaPttt)
            .Select(p => new PhuongThucThanhToanItem(
                p.MaPttt,
                p.TenPhuongThuc,
                p.ThanhToans.Count()))
            .ToListAsync();
    }

    public async Task TaoMoiAsync(string tenPhuongThuc, bool isActive = true)
    {
        var lastMa = await _db.PhuongThucThanhToans
            .OrderByDescending(p => p.MaPttt)
            .Select(p => p.MaPttt)
            .FirstOrDefaultAsync();

        _db.PhuongThucThanhToans.Add(new PhuongThucThanhToan
        {
            MaPttt = NextPttt(lastMa),
            TenPhuongThuc = tenPhuongThuc,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maPttt, string tenPhuongThuc, bool? isActive = null)
    {
        var item = await _db.PhuongThucThanhToans.FindAsync(maPttt);
        if (item == null) return;

        item.TenPhuongThuc = tenPhuongThuc;
        if (isActive.HasValue) item.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaAsync(string maPttt)
    {
        var item = await _db.PhuongThucThanhToans.FindAsync(maPttt);
        if (item == null) return false;

        bool coGiaoDich = await _db.ThanhToans.AnyAsync(t => t.MaPttt == item.MaPttt);
        if (coGiaoDich)
        {
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.PhuongThucThanhToans.Remove(item);
        await _db.SaveChangesAsync();
        return false;
    }
}
