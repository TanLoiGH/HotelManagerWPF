using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class TienNghiService
{
    private readonly QuanLyKhachSanContext _db;
    public TienNghiService(QuanLyKhachSanContext db) => _db = db;

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
            // (Giả sử TNTT04 là "Đã thanh lý/Mất")
            if (item.MaTrangThai != maTrangThai && maTrangThai == "TNTT04")
            {
                // Không cộng lại kho vì đã hỏng/mất, nhưng có thể cần logic khác
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



