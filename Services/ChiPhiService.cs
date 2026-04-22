using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

// DTO phục vụ hiển thị danh sách chi phí
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

    // Senior Note: Gom hằng số Prefix để quản lý tập trung
    private const string PREFIX_CHI_PHI = "CP";
    private const string PREFIX_LOAI_CHI_PHI = "LCP";

    public ChiPhiService(QuanLyKhachSanContext db) => _db = db;

    #region NGHIỆP VỤ PHIẾU CHI PHÍ (EXPENSES)

    public async Task GhiChiPhiAsync(
        string maLoaiCp, string tenChiPhi, decimal soTien,
        string? maNhanVien = null,
        string? maNcc = null,
        string? maPhong = null,
        string? ghiChu = null)
    {
        // Senior Fix: Validation cơ bản cho dòng tiền
        if (string.IsNullOrWhiteSpace(tenChiPhi))
            throw new ArgumentException("Tên chi phí không được để trống.");
        if (soTien < 0)
            throw new ArgumentException("Số tiền chi phí không thể là số âm.");

        var lastMa = await _db.ChiPhis
            .AsNoTracking()
            .OrderByDescending(c => c.MaChiPhi)
            .Select(c => c.MaChiPhi)
            .FirstOrDefaultAsync();

        _db.ChiPhis.Add(new ChiPhi
        {
            MaChiPhi = MaHelper.Next(PREFIX_CHI_PHI, lastMa),
            MaLoaiCp = maLoaiCp,
            MaNhanVien = maNhanVien,
            MaNcc = maNcc,
            MaPhong = maPhong,
            TenChiPhi = tenChiPhi.Trim(),
            SoTien = soTien,
            NgayChiPhi = TimeHelper.GetVietnamTime(),
            GhiChu = ghiChu?.Trim()
        });

        await _db.SaveChangesAsync();
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
                c.MaLoaiCpNavigation.TenLoaiCp ?? "Khác",
                c.SoTien,
                c.NgayChiPhi,
                c.MaNccNavigation != null ? c.MaNccNavigation.TenNcc : "",
                c.MaPhong ?? "",
                c.GhiChu ?? ""))
            .ToListAsync();
    }

    #endregion

    #region QUẢN LÝ DANH MỤC LOẠI CHI PHÍ

    public async Task<List<LoaiChiPhiDto>> GetLoaiChiPhiAsync()
        => await _db.LoaiChiPhis
            .AsNoTracking()
            .Where(l => l.IsActive == true) // Senior Fix: Chỉ lấy các loại đang hoạt động cho UI chọn
            .Select(l => new LoaiChiPhiDto
            {
                MaLoaiCP = l.MaLoaiCp,
                TenLoaiCP = l.TenLoaiCp
            }).ToListAsync();

    public async Task<List<LoaiChiPhi>> LayDanhSachLoaiChiPhiAsync()
    {
        return await _db.LoaiChiPhis
            .AsNoTracking()
            .OrderBy(l => l.MaLoaiCp)
            .ToListAsync();
    }

    public async Task TaoMoiLoaiChiPhiAsync(string tenLoaiCp, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(tenLoaiCp))
            throw new ArgumentException("Tên loại chi phí không được để trống.");

        var lastMa = await _db.LoaiChiPhis
            .AsNoTracking()
            .OrderByDescending(l => l.MaLoaiCp)
            .Select(l => l.MaLoaiCp)
            .FirstOrDefaultAsync();

        _db.LoaiChiPhis.Add(new LoaiChiPhi
        {
            MaLoaiCp = MaHelper.Next(PREFIX_LOAI_CHI_PHI, lastMa),
            TenLoaiCp = tenLoaiCp.Trim(),
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatLoaiChiPhiAsync(string maLoaiCp, string tenLoaiCp, bool? isActive = null)
    {
        if (string.IsNullOrWhiteSpace(tenLoaiCp))
            throw new ArgumentException("Tên loại chi phí không được để trống.");

        // Senior Fix: Không return im lặng nếu không tìm thấy dữ liệu
        var item = await _db.LoaiChiPhis.FindAsync(maLoaiCp)
                   ?? throw new KeyNotFoundException($"Không tìm thấy loại chi phí mã {maLoaiCp}.");

        item.TenLoaiCp = tenLoaiCp.Trim();
        if (isActive.HasValue) item.IsActive = isActive.Value;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaLoaiChiPhiAsync(string maLoaiCp)
    {
        var item = await _db.LoaiChiPhis.FindAsync(maLoaiCp)
                   ?? throw new KeyNotFoundException($"Không tìm thấy loại chi phí mã {maLoaiCp} để xóa.");

        // Senior Note: Kiểm tra xem đã có phiếu chi nào sử dụng loại này chưa
        bool coPhieu = await _db.ChiPhis.AnyAsync(c => c.MaLoaiCp == item.MaLoaiCp);

        if (coPhieu)
        {
            // Nếu đã có dữ liệu -> Chuyển sang ngưng hoạt động (Soft Delete)
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return true; // Trả về true báo hiệu là Soft Delete
        }

        // Nếu chưa có dữ liệu -> Xóa cứng khỏi DB
        _db.LoaiChiPhis.Remove(item);
        await _db.SaveChangesAsync();
        return false; // Trả về false báo hiệu là Hard Delete
    }

    #endregion
}