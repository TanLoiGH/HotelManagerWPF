using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class TienNghiService
{
    private readonly QuanLyKhachSanContext _db;
    public TienNghiService(QuanLyKhachSanContext db) => _db = db;

    public async Task<List<TienNghiTrangThai>> LayTrangThaiTienNghiAsync()
        => await _db.TienNghiTrangThais
            .AsNoTracking()
            .OrderBy(t => t.MaTrangThai)
            .ToListAsync();

    public async Task<List<NhaCungCap>> LayNhaCungCapDangHoatDongAsync()
        => await _db.NhaCungCaps
            .AsNoTracking()
            .Where(n => n.IsActive == true)
            .OrderBy(n => n.TenNcc)
            .ToListAsync();

    public async Task<List<TienNghi>> LayDanhMucTienNghiAsync()
        => await _db.TienNghis
            .AsNoTracking()
            .Include(t => t.MaNccNavigation)
            .Include(t => t.TienNghiPhongs)
            .OrderBy(t => t.MaTienNghi)
            .ToListAsync();

    public async Task TaoMoiTienNghiAsync(
        string tenTienNghi,
        string? maNcc,
        DateOnly? hanBaoHanh,
        int tongSoLuong,
        string? donViTinh,
        bool isActive)
    {
        var lastMa = await _db.TienNghis
            .OrderByDescending(t => t.MaTienNghi)
            .Select(t => t.MaTienNghi)
            .FirstOrDefaultAsync();

        _db.TienNghis.Add(new TienNghi
        {
            MaTienNghi = MaHelper.Next("TN", lastMa),
            TenTienNghi = tenTienNghi,
            MaNcc = maNcc,
            HanBaoHanh = hanBaoHanh,
            TongSoLuong = tongSoLuong,
            DonViTinh = donViTinh,
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatTienNghiAsync(
        string maTienNghi,
        string tenTienNghi,
        string? maNcc,
        DateOnly? hanBaoHanh,
        int tongSoLuong,
        string? donViTinh,
        bool isActive)
    {
        var item = await _db.TienNghis.FindAsync(maTienNghi);
        if (item == null) return;

        item.TenTienNghi = tenTienNghi;
        item.MaNcc = maNcc;
        item.HanBaoHanh = hanBaoHanh;
        item.TongSoLuong = tongSoLuong;
        item.DonViTinh = donViTinh;
        item.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task XoaTienNghiAsync(string maTienNghi)
    {
        bool used = await _db.TienNghiPhongs.AnyAsync(t => t.MaTienNghi == maTienNghi);
        if (used)
            throw new InvalidOperationException("Tiện nghi đã được gán vào phòng, không thể xóa. Hãy tắt Hoạt động (Off).");

        var item = await _db.TienNghis.FindAsync(maTienNghi);
        if (item == null) return;

        _db.TienNghis.Remove(item);
        await _db.SaveChangesAsync();
    }

    public async Task CapNhatTrangThaiAsync(
        string maPhong, string maTienNghi, string maTrangThai)
    {
        var item = await _db.TienNghiPhongs.FindAsync(maPhong, maTienNghi);
        var tienNghi = await _db.TienNghis.FindAsync(maTienNghi);

        if (item == null)
        {
            // Cấp mới tiện nghi vào phòng
            if (tienNghi != null)
            {
                if ((tienNghi.TongSoLuong ?? 0) <= 0)
                    throw new InvalidOperationException($"Tiện nghi {tienNghi.TenTienNghi} đã hết trong kho.");

                tienNghi.TongSoLuong--;
            }

            _db.TienNghiPhongs.Add(new TienNghiPhong
            {
                MaPhong = maPhong,
                MaTienNghi = maTienNghi,
                MaTrangThai = maTrangThai
            });
        }
        else
        {
            // Nếu trạng thái cũ là bình thường mà trạng thái mới là hỏng/mất/thanh lý
            if (item.MaTrangThai != maTrangThai && maTrangThai == "TNTT04")
            {
                // Logic xử lý khi thanh lý (nếu có)
            }

            item.MaTrangThai = maTrangThai;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<TienNghiPhongViewModel>> GetTienNghiPhongAsync(string maPhong)
        => await _db.TienNghiPhongs
            .Include(t => t.MaTienNghiNavigation)
                .ThenInclude(tn => tn.MaNccNavigation)
            .Include(t => t.MaTrangThaiNavigation)
            .Where(t => t.MaPhong == maPhong)
            .Select(t => new TienNghiPhongViewModel
            {
                MaTienNghi = t.MaTienNghi,
                TenTienNghi = t.MaTienNghiNavigation.TenTienNghi,
                HanBaoHanh = t.MaTienNghiNavigation.HanBaoHanh,
                MaNcc = t.MaTienNghiNavigation.MaNcc,
                TenNCC = t.MaTienNghiNavigation.MaNccNavigation != null
                         ? t.MaTienNghiNavigation.MaNccNavigation.TenNcc : "—",
                TenTrangThai = t.MaTrangThaiNavigation != null
                               ? t.MaTrangThaiNavigation.TenTrangThai ?? "" : "",
                MaTrangThai = t.MaTrangThai ?? "TNTT01",
                CanBaoTri = t.MaTrangThai == "TNTT02" || t.MaTrangThai == "TNTT03"
            })
            .ToListAsync();

    public async Task<List<TienNghi>> GetAllTienNghiAsync()
        => await _db.TienNghis
            .Include(t => t.MaNccNavigation)
            .Where(t => t.IsActive == true)
            .ToListAsync();
}