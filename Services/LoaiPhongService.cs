using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record LoaiPhongItem(
    string MaLoaiPhong,
    string TenLoaiPhong,
    int? SoNguoiToiDa,
    decimal GiaPhong,
    int SoPhong);

public class LoaiPhongService
{
    private readonly QuanLyKhachSanContext _db;

    // Senior Note: Gom Magic String "LP" vào hằng số
    private const string PREFIX_LOAI_PHONG = "LP";

    public LoaiPhongService(QuanLyKhachSanContext db) => _db = db;

    #region TRUY VẤN DỮ LIỆU

    public async Task<List<LoaiPhongItem>> LayDanhSachAsync()
    {
        return await _db.LoaiPhongs
            .AsNoTracking()
            .OrderBy(lp => lp.MaLoaiPhong)
            .Select(lp => new LoaiPhongItem(
                lp.MaLoaiPhong,
                lp.TenLoaiPhong ?? string.Empty,
                lp.SoNguoiToiDa,
                lp.GiaPhong,
                // Count trực tiếp từ Navigation Property rất tiện lợi
                lp.Phongs.Count()
            ))
            .ToListAsync();
    }

    #endregion

    #region THAO TÁC DỮ LIỆU (CRUD)

    public async Task TaoMoiAsync(string tenLoaiPhong, int? soNguoiToiDa, decimal giaPhong)
    {
        // Senior Fix: Bổ sung Validation logic chặt chẽ hơn
        if (string.IsNullOrWhiteSpace(tenLoaiPhong))
            throw new ArgumentException("Tên loại phòng không được để trống.");

        if (giaPhong < 0)
            throw new ArgumentException("Giá phòng không được phép nhỏ hơn 0.");

        if (soNguoiToiDa.HasValue && soNguoiToiDa.Value <= 0)
            throw new ArgumentException("Số người tối đa phải lớn hơn 0.");

        var lastMa = await _db.LoaiPhongs
            .OrderByDescending(lp => lp.MaLoaiPhong)
            .Select(lp => lp.MaLoaiPhong)
            .FirstOrDefaultAsync();

        _db.LoaiPhongs.Add(new LoaiPhong
        {
            MaLoaiPhong = MaHelper.Next(PREFIX_LOAI_PHONG, lastMa),
            TenLoaiPhong = tenLoaiPhong.Trim(),
            SoNguoiToiDa = soNguoiToiDa,
            GiaPhong = giaPhong,
        });

        await _db.SaveChangesAsync();
    }

    public async Task CapNhatAsync(string maLoaiPhong, string tenLoaiPhong, int? soNguoiToiDa, decimal giaPhong)
    {
        // Senior Fix: Validation đầu vào
        if (string.IsNullOrWhiteSpace(tenLoaiPhong))
            throw new ArgumentException("Tên loại phòng không được để trống.");

        if (giaPhong < 0)
            throw new ArgumentException("Giá phòng không được phép nhỏ hơn 0.");

        if (soNguoiToiDa.HasValue && soNguoiToiDa.Value <= 0)
            throw new ArgumentException("Số người tối đa phải lớn hơn 0.");

        // Senior Fix: Ném lỗi thay vì return im lặng
        var lp = await _db.LoaiPhongs.FindAsync(maLoaiPhong)
                 ?? throw new KeyNotFoundException($"Không tìm thấy loại phòng mã {maLoaiPhong}.");

        lp.TenLoaiPhong = tenLoaiPhong.Trim();
        lp.SoNguoiToiDa = soNguoiToiDa;
        lp.GiaPhong = giaPhong;

        await _db.SaveChangesAsync();
    }

    public async Task XoaAsync(string maLoaiPhong)
    {
        var lp = await _db.LoaiPhongs.FindAsync(maLoaiPhong)
                 ?? throw new KeyNotFoundException($"Không tìm thấy loại phòng mã {maLoaiPhong} để xóa.");

        // Senior Fix: Ràng buộc toàn vẹn dữ liệu (Foreign Key Check)
        // Tuyệt đối KHÔNG được xóa Loại Phòng nếu đang có Phòng vật lý (vd: Phòng 101, Phòng 102) dùng Loại này.
        bool dangSuDung = await _db.Phongs.AnyAsync(p => p.MaLoaiPhong == maLoaiPhong);
        if (dangSuDung)
        {
            throw new InvalidOperationException(
                $"Không thể xóa loại phòng '{lp.TenLoaiPhong}' vì đang có phòng vật lý thuộc loại này.\nVui lòng chuyển các phòng đó sang loại khác trước khi xóa.");
        }

        _db.LoaiPhongs.Remove(lp);
        await _db.SaveChangesAsync();
    }

    #endregion
}