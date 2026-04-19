using System.Threading.Tasks;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class AuditService : IAuditService
{
    private readonly QuanLyKhachSanContext _db;
    public AuditService(QuanLyKhachSanContext db) => _db = db;

    public async Task LogAsync(string thaoTac, string chiTiet)
    {
        var log = new HeThongNhatKy
        {
            MaNhanVien = AppSession.MaNhanVien,
            ThaoTac = thaoTac,
            ChiTiet = chiTiet,
            ThoiGian = TimeHelper.GetVietnamTime(),
            IpAddress = AppSession.IpAddress ?? "_NOT_FOUND_IP_"
        };
        _db.HeThongNhatKys.Add(log);
        await _db.SaveChangesAsync();
    }
}