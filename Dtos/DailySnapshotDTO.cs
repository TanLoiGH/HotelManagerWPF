using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Dtos
{
    public class DailySnapshotDTO
    {
        public int SnapshotID { get; set; }
        public DateTime Date { get; set; } // Ngày tạo snapshot
        public decimal TotalRevenue { get; set; } // Doanh thu
        public decimal TotalExpenses { get; set; } // Chi phí
        public int OccupiedRooms { get; set; }   // Phòng đã thuê
        public int AvailableRooms { get; set; }  // Phòng trống
    }
}
