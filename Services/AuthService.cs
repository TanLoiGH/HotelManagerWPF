using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Services
{
    public class AuthService
    {
        public TaiKhoan? Login(string username, string password)
        {
            using var db = new QuanLyKhachSanContext();

            return db.TaiKhoans
                .FirstOrDefault(tk =>
                    tk.TenDangNhap == username &&
                    tk.MatKhau == password &&
                    tk.IsActive == true);
        }
    }
}
