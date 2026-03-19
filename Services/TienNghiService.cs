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

        if (item == null)
            _db.TienNghiPhongs.Add(new TienNghiPhong
            {
                MaPhong = maPhong,
                MaTienNghi = maTienNghi,
                MaTrangThai = maTrangThai
            });
        else
            item.MaTrangThai = maTrangThai;

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



