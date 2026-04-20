using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class DashboardService
{
    private readonly QuanLyKhachSanContext _db;

    public DashboardService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetDoanhThuAsync(DateTime tuNgay, DateTime denNgay)
    {
        return await _db.HoaDons
            .Where(h => h.TrangThai == HoaDonTrangThaiTexts.DaThanhToan && h.NgayLap >= tuNgay && h.NgayLap <= denNgay)
            .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;
    }

    public async Task<decimal> GetChiPhiAsync(DateTime tuNgay, DateTime denNgay)
    {
        return await _db.ChiPhis
            .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
            .SumAsync(c => (decimal?)c.SoTien) ?? 0;
    }

    public async Task<(int TongPhong, int PhongDangO)> GetRoomStatsAsync()
    {
        int tong = await _db.Phongs.CountAsync();
        int dangO = await _db.Phongs.CountAsync(p => p.MaTrangThaiPhong == PhongTrangThaiCodes.DangO);
        return (tong, dangO);
    }

    public async Task<List<(int Year, int Month, decimal Total)>> GetMonthlyRevenueAsync(int monthsCount = 12)
    {
        var now = TimeHelper.GetVietnamTime();
        var result = new List<(int Year, int Month, decimal Total)>();
        for (int i = monthsCount - 1; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var total = await _db.HoaDons
                .Where(h => h.TrangThai == HoaDonTrangThaiTexts.DaThanhToan && h.NgayLap.HasValue && h.NgayLap.Value.Year == d.Year && h.NgayLap.Value.Month == d.Month)
                .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;
            result.Add((d.Year, d.Month, total));
        }
        return result;
    }

    public async Task<List<RoomStatusStat>> GetRoomStatusDistributionAsync()
    {
        return await _db.Phongs
            .Include(p => p.MaTrangThaiPhongNavigation)
            .GroupBy(p => new { p.MaTrangThaiPhong, p.MaTrangThaiPhongNavigation!.TenTrangThai })
            .Select(g => new RoomStatusStat { MaTrangThai = g.Key.MaTrangThaiPhong ?? "", TenTrangThai = g.Key.TenTrangThai ?? "", Count = g.Count() })
            .ToListAsync();
    }

    public async Task<List<TopServiceStat>> GetTopServicesAsync(DateTime tuNgay, DateTime denNgay, int limit = 5)
    {
        return await _db.DichVuChiTiets
            .Include(d => d.MaDichVuNavigation)
            .Where(d => d.NgaySuDung >= tuNgay && d.NgaySuDung <= denNgay)
            .GroupBy(d => d.MaDichVuNavigation.TenDichVu)
            .Select(g => new TopServiceStat { TenDichVu = g.Key ?? "", Count = g.Sum(x => x.SoLuong) })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ExpenseStat>> GetExpenseDistributionAsync(DateTime tuNgay, DateTime denNgay)
    {
        return await _db.ChiPhis
            .Include(c => c.MaLoaiCpNavigation)
            .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
            .GroupBy(c => c.MaLoaiCpNavigation.TenLoaiCp)
            .Select(g => new ExpenseStat { Loai = g.Key ?? "Khác", Total = g.Sum(x => x.SoTien) })
            .OrderByDescending(x => x.Total)
            .ToListAsync();
    }

    public async Task<List<DatPhong>> GetRecentBookingsAsync(int limit = 6)
    {
        return await _db.DatPhongs
            .Include(d => d.MaKhachHangNavigation)
            .OrderByDescending(d => d.NgayDat)
            .Take(limit)
            .ToListAsync();
    }
}

public class RoomStatusStat { public string MaTrangThai { get; set; } = ""; public string TenTrangThai { get; set; } = ""; public int Count { get; set; } }
public class TopServiceStat { public string TenDichVu { get; set; } = ""; public int Count { get; set; } }
public class ExpenseStat { public string Loai { get; set; } = ""; public decimal Total { get; set; } }
