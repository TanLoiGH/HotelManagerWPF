using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardData Data { get; set; } = new();

        public ObservableCollection<DoanhThuThangVM> DoanhThu12Thang { get; set; } = [];
        public ObservableCollection<PhongStatusVM> PhongStatus { get; set; } = [];
        public ObservableCollection<TopDichVuVM> TopDichVu { get; set; } = [];
        public ObservableCollection<ChiPhiVM> ChiPhi { get; set; } = [];

        public string DoanhThuText => Data.DoanhThuText;
        public string LoiNhuanText => Data.LoiNhuanText;
        public string TongChiPhiText => Data.TongChiPhiText;

        public int TongPhong => Data.PhongStats.Values.Sum();

        public void ExportDashboardToPdf(DashboardViewModel vm)
        {
            try
            {
                var report = new DashboardReport(vm);
                string fileName = $"BaoCao_{TimeHelper.GetVietnamTime():yyyyMMdd_HHmm}.pdf";
                report.GeneratePdf(fileName);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi", ex);
                System.Diagnostics.Debug.WriteLine("Lỗi xuất báo cáo: " + ex.Message);
            }
        }
    }
}
