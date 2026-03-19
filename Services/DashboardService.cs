using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Dtos;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DashboardService
{
    private readonly QuanLyKhachSanContext _db;
    public DashboardService(QuanLyKhachSanContext db) => _db = db;

    public async Task<DashboardData> GetDashboardAsync(DateTime tuNgay, DateTime denNgay)
    {
        var doanhThu = await _db.HoaDons
            .Where(h => h.TrangThai == "Đã thanh toán"
                     && h.NgayLap >= tuNgay
                     && h.NgayLap <= denNgay)
            .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;

        var chiPhi = await _db.ChiPhis
            .Include(c => c.MaLoaiCpNavigation)
            .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
            .GroupBy(c => c.MaLoaiCpNavigation!.TenLoaiCp)
            .Select(g => new ChiPhiSummary
            {
                Loai = g.Key ?? "Khác",
                Tong = g.Sum(c => c.SoTien)
            })
            .ToListAsync();

        var phongStats = await _db.Phongs
            .Include(p => p.MaTrangThaiPhongNavigation)
            .GroupBy(p => p.MaTrangThaiPhongNavigation!.TenTrangThai)
            .Select(g => new { TenTT = g.Key ?? "Không rõ", SoPhong = g.Count() })
            .ToDictionaryAsync(x => x.TenTT, x => x.SoPhong);

        var khachStats = await _db.KhachHangs
            .Include(k => k.MaLoaiKhachNavigation)
            .GroupBy(k => k.MaLoaiKhachNavigation!.TenLoaiKhach)
            .Select(g => new { Hang = g.Key ?? "Khác", SoKhach = g.Count() })
            .ToDictionaryAsync(x => x.Hang, x => x.SoKhach);

        var topDichVu = await _db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.NgaySuDung >= tuNgay && d.NgaySuDung <= denNgay)
            .GroupBy(d => d.MaDichVuNavigation!.TenDichVu)
            .Select(g => new { TenDV = g.Key ?? "", Tong = g.Sum(d => d.SoLuong) })
            .OrderByDescending(g => g.Tong)
            .Take(5)
            .ToDictionaryAsync(x => x.TenDV, x => x.Tong);

        return new DashboardData
        {
            TuNgay = tuNgay,
            DenNgay = denNgay,
            DoanhThu = doanhThu,
            TongChiPhi = chiPhi.Sum(c => c.Tong),
            LoiNhuan = doanhThu - chiPhi.Sum(c => c.Tong),
            ChiPhiByLoai = chiPhi,
            PhongStats = phongStats,
            KhachStats = khachStats,
            TopDichVu = topDichVu
        };
    }
}




