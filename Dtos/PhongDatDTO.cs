using System;
using System.Collections.Generic;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Dtos
{
    // Models/DTOs/PhongDatDTO.cs
    public class PhongDatDTO
    {
        public string MaPhong { get; set; } = "";
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public decimal DonGia { get; set; }
    }
}
