using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class TienNghiService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Khai báo toàn bộ Hằng số để dễ quản lý, tránh Magic Strings
    private const string PREFIX_TIEN_NGHI = "TN";
    private const string TRANG_THAI_BINH_THUONG = "TNTT01";
    private const string TRANG_THAI_BAO_TRI = "TNTT02";
    private const string TRANG_THAI_HONG = "TNTT03";
    private const string TRANG_THAI_THANH_LY = "TNTT04";

    public TienNghiService(QuanLyKhachSanContext db) => _db = db;

    #region TRUY VẤN DỮ LIỆU & DANH MỤC

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

    public async Task<List<TienNghi>> GetAllTienNghiAsync()
        => await _db.TienNghis
            .AsNoTracking() // Senior Fix: Thêm AsNoTracking vì chỉ để lấy list hiển thị
            .Include(t => t.MaNccNavigation)
            .Where(t => t.IsActive == true)
            .ToListAsync();

    public async Task<List<TienNghiPhongViewModel>> GetTienNghiPhongAsync(string maPhong)
        => await _db.TienNghiPhongs
            .AsNoTracking() // Senior Fix: ViewModel Projection thì bắt buộc phải AsNoTracking để tối ưu
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
                    ? t.MaTienNghiNavigation.MaNccNavigation.TenNcc
                    : "—",
                TenTrangThai = t.MaTrangThaiNavigation != null
                    ? t.MaTrangThaiNavigation.TenTrangThai ?? ""
                    : "",
                MaTrangThai = t.MaTrangThai ?? TRANG_THAI_BINH_THUONG,
                CanBaoTri = t.MaTrangThai == TRANG_THAI_BAO_TRI || t.MaTrangThai == TRANG_THAI_HONG
            })
            .ToListAsync();

    #endregion

    #region THAO TÁC CƠ SỞ DỮ LIỆU KHO (CRUD)

    public async Task TaoMoiTienNghiAsync(
        string tenTienNghi, string? maNcc, DateOnly? hanBaoHanh,
        int tongSoLuong, string? donViTinh, bool isActive)
    {
        // Senior Fix: Validation chặn rác
        if (string.IsNullOrWhiteSpace(tenTienNghi))
            throw new ArgumentException("Tên tiện nghi không được để trống.");
        if (tongSoLuong < 0)
            throw new ArgumentException("Tổng số lượng không thể là số âm.");

        var lastMa = await _db.TienNghis
            .OrderByDescending(t => t.MaTienNghi)
            .Select(t => t.MaTienNghi)
            .FirstOrDefaultAsync();

        _db.TienNghis.Add(new TienNghi
        {
            MaTienNghi = MaHelper.Next(PREFIX_TIEN_NGHI, lastMa),
            TenTienNghi = tenTienNghi.Trim(), // Defensive Programming
            MaNcc = maNcc,
            HanBaoHanh = hanBaoHanh,
            TongSoLuong = tongSoLuong,
            DonViTinh = donViTinh?.Trim(),
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatTienNghiAsync(
        string maTienNghi, string tenTienNghi, string? maNcc,
        DateOnly? hanBaoHanh, int tongSoLuong, string? donViTinh, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(tenTienNghi))
            throw new ArgumentException("Tên tiện nghi không được để trống.");
        if (tongSoLuong < 0)
            throw new ArgumentException("Tổng số lượng không thể là số âm.");

        // Senior Fix: Ngừng nuốt lỗi
        var item = await _db.TienNghis.FindAsync(maTienNghi)
                   ?? throw new KeyNotFoundException($"Không tìm thấy tiện nghi mã {maTienNghi} để cập nhật.");

        item.TenTienNghi = tenTienNghi.Trim();
        item.MaNcc = maNcc;
        item.HanBaoHanh = hanBaoHanh;
        item.TongSoLuong = tongSoLuong;
        item.DonViTinh = donViTinh?.Trim();
        item.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task XoaTienNghiAsync(string maTienNghi)
    {
        bool used = await _db.TienNghiPhongs.AnyAsync(t => t.MaTienNghi == maTienNghi);
        if (used)
            throw new InvalidOperationException(
                "Tiện nghi đã được gán vào phòng, không thể xóa. Hãy tắt trạng thái Hoạt động (Off).");

        var item = await _db.TienNghis.FindAsync(maTienNghi)
                   ?? throw new KeyNotFoundException($"Không tìm thấy tiện nghi mã {maTienNghi} để xóa.");

        _db.TienNghis.Remove(item);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region NGHIỆP VỤ QUẢN LÝ TIỆN NGHI TRONG PHÒNG

    public async Task CapNhatTrangThaiAsync(string maPhong, string maTienNghi, string maTrangThai)
    {
        var item = await _db.TienNghiPhongs.FindAsync(maPhong, maTienNghi);
        var tienNghi = await _db.TienNghis.FindAsync(maTienNghi)
                       ?? throw new KeyNotFoundException($"Không tìm thấy tiện nghi mã {maTienNghi} trong hệ thống.");

        if (item == null)
        {
            // Cấp mới tiện nghi vào phòng
            if ((tienNghi.TongSoLuong ?? 0) <= 0)
                throw new InvalidOperationException($"Tiện nghi '{tienNghi.TenTienNghi}' đã hết trong kho.");

            tienNghi.TongSoLuong--;

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
            if (item.MaTrangThai != maTrangThai && maTrangThai == TRANG_THAI_THANH_LY)
            {
                // Senior Note: Nếu thanh lý, tiện nghi đó bị vứt bỏ, kho cũng không được cộng lại.
                // Tuy nhiên, nên có 1 Table LogChiPhi để ghi nhận khoản mất mát này.
                // TODO: Gọi ChiPhiService ghi nhận khoản phí thanh lý tại đây (nếu quy định).
            }

            item.MaTrangThai = maTrangThai;
        }

        await _db.SaveChangesAsync();
    }

    #endregion
}