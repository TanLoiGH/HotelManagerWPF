using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace QuanLyKhachSan_PhamTanLoi.Reports.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
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

        public event PropertyChangedEventHandler? PropertyChanged;
        public void ExportDashboardToPdf(DashboardViewModel vm)
        {
            try
            {
                var report = new DashboardReport(vm);
                string fileName = $"BaoCao_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                report.GeneratePdf(fileName);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi xuất báo cáo: " + ex.Message);
            }
        }
    }
}
