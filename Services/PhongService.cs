using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System;
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

public class PhongService : IPhongService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom mã trạng thái mặc định của Tiện nghi vào hằng số
    private const string MA_TRANG_THAI_TIEN_NGHI_MAC_DINH = "TNTT01";

    public PhongService(QuanLyKhachSanContext db) => _db = db;

    #region TRUY VẤN DỮ LIỆU SƠ ĐỒ PHÒNG & BOOKING

    public async Task<List<Phong>> LayDanhSachPhongChiTietAsync()
    {
        return await _db.Phongs
            .AsNoTracking()
            .Include(p => p.MaLoaiPhongNavigation)
            .Include(p => p.MaTrangThaiPhongNavigation)
            .OrderBy(p => p.MaPhong)
            .ToListAsync();
    }

    public async Task<List<DatPhongChiTiet>> LayChiTietDatPhongDangHoatDongAsync()
    {
        return await _db.DatPhongChiTiets
            .AsNoTracking()
            .Include(c => c.MaDatPhongNavigation)
            .ThenInclude(dp => dp.MaKhachHangNavigation)
            .Where(c => c.MaDatPhongNavigation.TrangThai == DatPhongTrangThaiTexts.DangO ||
                        c.MaDatPhongNavigation.TrangThai == DatPhongTrangThaiTexts.ChoNhanPhong)
            .ToListAsync();
    }

    public async Task<DatPhongChiTiet?> LayDatPhongChoNhanTheoPhongAsync(string maPhong)
    {
        return await _db.DatPhongChiTiets
            .AsNoTracking()
            .Include(c => c.MaDatPhongNavigation)
            .ThenInclude(dp => dp.MaKhachHangNavigation)
            .Where(c => c.MaPhong == maPhong &&
                        (c.MaDatPhongNavigation!.TrangThai == DatPhongTrangThaiTexts.ChoNhanPhong ||
                         c.MaDatPhongNavigation!.TrangThai == DatPhongTrangThaiTexts.DangO) &&
                        c.MaPhongNavigation.MaTrangThaiPhong == PhongTrangThaiCodes.DaDat)
            .OrderByDescending(c => c.MaDatPhongNavigation.NgayDat)
            .FirstOrDefaultAsync();
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

    #endregion

    #region QUẢN TRỊ CƠ SỞ VẬT CHẤT & TIỆN NGHI

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
                p.MaTrangThaiPhong ?? PhongTrangThaiCodes.Trong,
                p.MaTrangThaiPhongNavigation != null ? (p.MaTrangThaiPhongNavigation.TenTrangThai ?? "") : "",
                p.DatPhongChiTiets.Any() || p.TienNghiPhongs.Any() || p.ChiPhis.Any()))
            .ToListAsync();
    }

    public async Task<List<TienNghiPhong>> LayTienNghiPhongAsync(string maPhong)
    {
        return await _db.TienNghiPhongs
            .AsNoTracking()
            .Include(t => t.MaTienNghiNavigation)
            .Where(t => t.MaPhong == maPhong)
            .OrderBy(t => EF.Functions.Collate(t.MaTienNghiNavigation.TenTienNghi, "Vietnamese_CI_AI"))
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
                t.MaTrangThai ?? MA_TRANG_THAI_TIEN_NGHI_MAC_DINH)) // Senior Fix: Dùng hằng số
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
                MaTrangThai = MA_TRANG_THAI_TIEN_NGHI_MAC_DINH // Senior Fix: Dùng hằng số
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
        var item = await _db.TienNghiPhongs.FindAsync(maPhong, maTienNghi)
                   ?? throw new KeyNotFoundException($"Không tìm thấy tiện nghi {maTienNghi} trong phòng {maPhong}.");

        _db.TienNghiPhongs.Remove(item);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region QUẢN LÝ PHÒNG (CRUD)

    public async Task TaoPhongAsync(string maPhong, string maLoaiPhong, string maTrangThai)
    {
        if (string.IsNullOrWhiteSpace(maPhong))
            throw new ArgumentException("Mã phòng không được để trống.");

        maPhong = maPhong.Trim(); // Defensive Programming

        bool exists = await _db.Phongs.AnyAsync(p => p.MaPhong == maPhong);
        if (exists)
            throw new InvalidOperationException($"Mã phòng '{maPhong}' đã tồn tại trong hệ thống.");

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
        // Senior Fix: Ném lỗi đàng hoàng, không return âm thầm
        var p = await _db.Phongs.FindAsync(maPhong)
                ?? throw new KeyNotFoundException($"Không tìm thấy dữ liệu phòng {maPhong}.");

        bool coDangO = await _db.DatPhongChiTiets
            .AnyAsync(c => c.MaPhong == maPhong && c.MaDatPhongNavigation!.TrangThai == DatPhongTrangThaiTexts.DangO);

        bool coChoNhan = await _db.DatPhongChiTiets
            .AnyAsync(c =>
                c.MaPhong == maPhong && c.MaDatPhongNavigation!.TrangThai == DatPhongTrangThaiTexts.ChoNhanPhong);

        // Senior Fix: Sửa lỗi text Tiếng Việt không dấu thành có dấu chuẩn mực để hiển thị lên MessageBox
        if (coDangO && maTrangThai != PhongTrangThaiCodes.DangO)
            throw new InvalidOperationException("Phòng đang có khách ở, không thể chuyển sang trạng thái khác.");

        if (!coDangO && coChoNhan && maTrangThai != PhongTrangThaiCodes.DaDat)
            throw new InvalidOperationException("Phòng đang được khách đặt trước, chỉ có thể để trạng thái 'Đã đặt'.");

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
            throw new InvalidOperationException(
                $"Phòng {maPhong} đã phát sinh dữ liệu (Hóa đơn, Khách thuê, hoặc Tiện nghi), không thể xóa.");

        var p = await _db.Phongs.FindAsync(maPhong)
                ?? throw new KeyNotFoundException($"Không tìm thấy phòng {maPhong} để xóa.");

        _db.Phongs.Remove(p);
        await _db.SaveChangesAsync();
    }

    #endregion
}