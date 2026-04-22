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

    #region TRUY VẤN DỮ LIỆU TIỆN NGHI & DANH MỤC

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

    public async Task<List<TienNghi>> LayTienNghiAsync()
        => await _db.TienNghis
            .AsNoTracking()
            .Include(t => t.MaNccNavigation)
            .Include(t => t.MaDanhMucNavigation)
            .Include(t => t.TienNghiPhongs)
            .OrderBy(t => t.MaTienNghi)
            .ToListAsync();

    public async Task<List<TienNghiDanhMuc>> LayTatCaDanhMucAsync()
        => await _db.TienNghiDanhMucs
            .AsNoTracking()
            .OrderBy(d => d.TenDanhMuc)
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
            .Include(t => t.MaTienNghiNavigation)
            .ThenInclude(tn => tn.MaDanhMucNavigation)
            .Where(t => t.MaPhong == maPhong)
            .Select(t => new TienNghiPhongViewModel
            {
                MaDanhMuc = t.MaTienNghiNavigation.MaDanhMuc,
                TenDanhMuc = t.MaTienNghiNavigation.MaDanhMucNavigation != null
                    ? t.MaTienNghiNavigation.MaDanhMucNavigation.TenDanhMuc
                    : "Chưa phân loại",
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
        string tenTienNghi, string? maDanhMuc, string? maNcc, DateOnly? hanBaoHanh,
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
            MaDanhMuc = maDanhMuc,
            MaNcc = maNcc,
            HanBaoHanh = hanBaoHanh,
            TongSoLuong = tongSoLuong,
            DonViTinh = donViTinh?.Trim(),
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatTienNghiAsync(
        string maTienNghi, string tenTienNghi, string? maDanhMuc, string? maNcc,
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
        item.MaDanhMuc = maDanhMuc;
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

    #region CRUD DANH MỤC TIỆN NGHI

    // 1. Hàm tạo mới
    public async Task<string> TaoMoiDanhMucAsync(string tenDanhMuc, bool isActive)
    {
        var lastMa = await _db.TienNghiDanhMucs
            .OrderByDescending(d => d.MaDanhMuc)
            .Select(d => d.MaDanhMuc)
            .FirstOrDefaultAsync();

        string newMa = MaHelper.Next("TNDM", lastMa);

        var dmMoi = new TienNghiDanhMuc
        {
            MaDanhMuc = newMa,
            TenDanhMuc = tenDanhMuc.Trim(),
            IsActive = isActive
        };

        _db.TienNghiDanhMucs.Add(dmMoi);
        await _db.SaveChangesAsync();

        return newMa;
    }

    // 2. Hàm Cập nhật
    public async Task CapNhatDanhMucAsync(string maDanhMuc, string tenDanhMuc, bool isActive)
    {
        var dm = await _db.TienNghiDanhMucs.FindAsync(maDanhMuc)
                 ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");

        dm.TenDanhMuc = tenDanhMuc.Trim();
        dm.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    // 3. Hàm Xóa
    public async Task XoaDanhMucAsync(string maDanhMuc)
    {
        // Cảnh báo: Phải kiểm tra xem Danh mục này đã được gán cho Tiện nghi nào chưa
        bool isUsed = await _db.TienNghis.AnyAsync(t => t.MaDanhMuc == maDanhMuc);
        if (isUsed)
        {
            throw new InvalidOperationException(
                "Danh mục này đang được sử dụng bởi các Tiện nghi trong kho. Không thể xóa!");
        }

        var dm = await _db.TienNghiDanhMucs.FindAsync(maDanhMuc)
                 ?? throw new KeyNotFoundException("Không tìm thấy danh mục.");

        _db.TienNghiDanhMucs.Remove(dm);
        await _db.SaveChangesAsync();
    }

    #endregion

    #region NGHIỆP VỤ QUẢN LÝ TIỆN NGHI TRONG PHÒNG

    public async Task CapNhatTrangThaiAsync(string maPhong, string maTienNghi, string maTrangThai, bool isThayMoi)
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
            if (isThayMoi)
            {
                if ((tienNghi.TongSoLuong ?? 0) <= 0)
                    throw new InvalidOperationException($"Không đủ '{tienNghi.TenTienNghi}' trong kho để thay mới.");

                tienNghi.TongSoLuong--;
                item.MaTrangThai = TRANG_THAI_BINH_THUONG;
            }
            else
            {
                item.MaTrangThai = maTrangThai;
            }
        }

        await _db.SaveChangesAsync();
    }

    #endregion
}