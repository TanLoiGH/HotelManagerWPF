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

// Senior Refactor: Chuyển toàn bộ DTO class sang Record cho gọn và an toàn (Immutable)
public record RoomStatusStat(string MaTrangThai, string TenTrangThai, int Count);

public record TopServiceStat(string TenDichVu, int Count);

public record ExpenseStat(string Loai, decimal Total);

public class DashboardService
{
    private readonly QuanLyKhachSanContext _db;

    public DashboardService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    #region TỔNG HỢP DOANH THU & CHI PHÍ

    public async Task<decimal> GetDoanhThuAsync(DateTime tuNgay, DateTime denNgay)
    {
        return await _db.HoaDons
            .AsNoTracking()
            .Where(h => h.TrangThai == HoaDonTrangThaiTexts.DaThanhToan
                        && h.NgayLap >= tuNgay
                        && h.NgayLap <= denNgay)
            .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;
    }

    public async Task<decimal> GetChiPhiAsync(DateTime tuNgay, DateTime denNgay)
    {
        return await _db.ChiPhis
            .AsNoTracking()
            .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
            .SumAsync(c => (decimal?)c.SoTien) ?? 0;
    }

    public async Task<List<(int Year, int Month, decimal Total)>> GetMonthlyRevenueAsync(int monthsCount = 12)
    {
        var now = TimeHelper.GetVietnamTime();

        var startDate = now.AddMonths(-monthsCount + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        var rawData = await _db.HoaDons
            .AsNoTracking()
            .Where(h => h.TrangThai == HoaDonTrangThaiTexts.DaThanhToan
                        && h.NgayLap != null
                        && h.NgayLap >= startDate)
            .GroupBy(h => new { h.NgayLap.Value.Year, h.NgayLap.Value.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(h => h.TongThanhToan)
            })
            .ToListAsync();

        var result = new List<(int Year, int Month, decimal Total)>();

        for (int i = monthsCount - 1; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var monthData = rawData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
            result.Add((d.Year, d.Month, monthData?.Total ?? 0m));
        }

        return result;
    }

    #endregion

    #region THỐNG KÊ PHÒNG, DỊCH VỤ VÀ ĐẶT PHÒNG

    public async Task<(int TongPhong, int PhongDangO)> GetRoomStatsAsync()
    {
        int tong = await _db.Phongs.CountAsync();
        int dangO = await _db.Phongs.CountAsync(p => p.MaTrangThaiPhong == PhongTrangThaiCodes.DangO);
        return (tong, dangO);
    }

    public async Task<List<RoomStatusStat>> GetRoomStatusDistributionAsync()
    {
        // Bước 1: Trả về Anonymous object để EF Core dịch sang SQL
        var rawData = await _db.Phongs
            .AsNoTracking()
            .GroupBy(p => new { p.MaTrangThaiPhong, p.MaTrangThaiPhongNavigation!.TenTrangThai })
            .Select(g => new
            {
                MaTrangThai = g.Key.MaTrangThaiPhong,
                TenTrangThai = g.Key.TenTrangThai,
                Count = g.Count()
            })
            .ToListAsync();

        // Bước 2: Mapping sang Record trên Client (RAM)
        return rawData.Select(x => new RoomStatusStat(
            x.MaTrangThai ?? "",
            x.TenTrangThai ?? "",
            x.Count
        )).ToList();
    }

    public async Task<List<TopServiceStat>> GetTopServicesAsync(DateTime tuNgay, DateTime denNgay, int limit = 5)
    {
        // Bước 1: SQL Server tính tổng số lượng
        var rawData = await _db.DichVuChiTiets
            .AsNoTracking()
            .Where(d => d.NgaySuDung >= tuNgay && d.NgaySuDung <= denNgay)
            .GroupBy(d => d.MaDichVuNavigation.TenDichVu)
            .Select(g => new
            {
                TenDichVu = g.Key,
                Count = g.Sum(x => x.SoLuong)
            })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        // Bước 2: Tạo record
        return rawData.Select(x => new TopServiceStat(
            x.TenDichVu ?? "Dịch vụ khác",
            x.Count
        )).ToList();
    }

    public async Task<List<ExpenseStat>> GetExpenseDistributionAsync(DateTime tuNgay, DateTime denNgay)
    {
        // Bước 1
        var rawData = await _db.ChiPhis
            .AsNoTracking()
            .Where(c => c.NgayChiPhi >= tuNgay && c.NgayChiPhi <= denNgay)
            .GroupBy(c => c.MaLoaiCpNavigation.TenLoaiCp)
            .Select(g => new
            {
                TenLoaiCp = g.Key,
                Total = g.Sum(x => x.SoTien)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        // Bước 2
        return rawData.Select(x => new ExpenseStat(
            x.TenLoaiCp ?? "Khác",
            x.Total
        )).ToList();
    }

    public async Task<List<DatPhong>> GetRecentBookingsAsync(int limit = 6)
    {
        return await _db.DatPhongs
            .AsNoTracking()
            .Include(d => d.MaKhachHangNavigation)
            .OrderByDescending(d => d.NgayDat)
            .Take(limit)
            .ToListAsync();
    }

    #endregion
}