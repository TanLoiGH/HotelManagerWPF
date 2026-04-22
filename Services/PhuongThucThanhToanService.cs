using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record PhuongThucThanhToanItem(string MaPttt, string TenPhuongThuc, int SoGiaoDich);

public class PhuongThucThanhToanService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom tiền tố thành hằng số
    private const string PREFIX_PTTT = "PTTT";

    public PhuongThucThanhToanService(QuanLyKhachSanContext db) => _db = db;

    // Senior Fix: Đã xóa hoàn toàn hàm `NextPttt` rườm rà. Chúng ta sẽ dùng `MaHelper.Next`

    #region TRUY VẤN DỮ LIỆU

    public async Task<List<PhuongThucThanhToanItem>> LayDanhSachAsync()
    {
        return await _db.PhuongThucThanhToans
            .AsNoTracking()
            .OrderBy(p => p.MaPttt)
            .Select(p => new PhuongThucThanhToanItem(
                p.MaPttt,
                p.TenPhuongThuc,
                // Tối ưu: Đếm trực tiếp số giao dịch thông qua Navigation Property
                p.ThanhToans.Count()))
            .ToListAsync();
    }

    #endregion

    #region THAO TÁC DỮ LIỆU (CRUD)

    public async Task TaoMoiAsync(string tenPhuongThuc, bool isActive = true)
    {
        // Senior Fix: Bắt lỗi nếu tên trống
        if (string.IsNullOrWhiteSpace(tenPhuongThuc))
            throw new ArgumentException("Tên phương thức thanh toán không được để trống.");

        var lastMa = await _db.PhuongThucThanhToans
            .OrderByDescending(p => p.MaPttt)
            .Select(p => p.MaPttt)
            .FirstOrDefaultAsync();

        _db.PhuongThucThanhToans.Add(new PhuongThucThanhToan
        {
            // Senior Fix: Tái sử dụng MaHelper chung của toàn dự án
            MaPttt = MaHelper.Next(PREFIX_PTTT, lastMa),
            TenPhuongThuc = tenPhuongThuc.Trim(),
            IsActive = isActive,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maPttt, string tenPhuongThuc, bool? isActive = null)
    {
        if (string.IsNullOrWhiteSpace(tenPhuongThuc))
            throw new ArgumentException("Tên phương thức thanh toán không được để trống.");

        // Senior Fix: Ném lỗi thay vì return âm thầm
        var item = await _db.PhuongThucThanhToans.FindAsync(maPttt)
                   ?? throw new KeyNotFoundException($"Không tìm thấy phương thức thanh toán mã {maPttt} để cập nhật.");

        item.TenPhuongThuc = tenPhuongThuc.Trim();
        if (isActive.HasValue) item.IsActive = isActive;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> XoaHoacVoHieuHoaAsync(string maPttt)
    {
        var item = await _db.PhuongThucThanhToans.FindAsync(maPttt)
                   ?? throw new KeyNotFoundException($"Không tìm thấy phương thức thanh toán mã {maPttt} để xóa.");

        // Nếu PTTT đã có hóa đơn sử dụng -> Soft Delete (Ngưng hoạt động)
        bool coGiaoDich = await _db.ThanhToans.AnyAsync(t => t.MaPttt == item.MaPttt);
        if (coGiaoDich)
        {
            item.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // Nếu chưa từng sử dụng -> Hard Delete (Xóa hẳn)
        _db.PhuongThucThanhToans.Remove(item);
        await _db.SaveChangesAsync();
        return false;
    }

    #endregion
}