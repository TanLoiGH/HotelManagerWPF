using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Services.Interfaces
{
    public interface IKhuyenMaiService
    {
        // Lấy toàn bộ danh sách Khuyến mãi từ Database
        Task<List<KhuyenMai>> GetAllKhuyenMaiAsync();
    }
}