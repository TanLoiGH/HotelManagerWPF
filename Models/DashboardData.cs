using System;
using System.Collections.Generic;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class DashboardData
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal TongChiPhi { get; set; }
    public decimal LoiNhuan { get; set; }
    public List<ChiPhiSummary> ChiPhiByLoai { get; set; } = [];
    public Dictionary<string, int> PhongStats { get; set; } = [];
    public Dictionary<string, int> KhachStats { get; set; } = [];
    public Dictionary<string, int> TopDichVu { get; set; } = [];

    public string DoanhThuText => DoanhThu.ToString("N0") + " ₫";
    public string LoiNhuanText => LoiNhuan.ToString("N0") + " ₫";
    public string TongChiPhiText => TongChiPhi.ToString("N0") + " ₫";
}