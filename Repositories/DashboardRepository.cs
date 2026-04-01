using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace QuanLyKhachSan_PhamTanLoi.Repositories
{
    public class DashboardRepository
    {
        private readonly QuanLyKhachSanContext _db;

        public DashboardRepository(QuanLyKhachSanContext db)
        {
            _db = db;
        }

        public async Task<List<VwDoanhThuThang>> GetDoanhThuThangAsync()
        {
            return await _db.VwDoanhThuThangs
                        .OrderBy(x => x.Nam)
                        .ThenBy(x => x.Thang)
                        .ToListAsync(); 
        }

        public async Task<List<VwCoCauChiPhi>> GetChiPhiAsync()
        {
            return await _db.VwCoCauChiPhis.ToListAsync();
        }

        public async Task<List<VwTopDichVu>> GetTopDichVuAsync()
            => await _db.VwTopDichVus
                .OrderByDescending(x => x.TongSuDung)
                .Take(5)
                .ToListAsync();
    }
}
