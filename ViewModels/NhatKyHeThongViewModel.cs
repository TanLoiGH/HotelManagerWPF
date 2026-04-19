using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels
{
    public class NhatKyHeThongViewModel : BaseViewModel
    {
        private readonly QuanLyKhachSanContext _db;
        private List<HeThongNhatKy> _allLogs = new();
        private ObservableCollection<HeThongNhatKy> _filteredLogs = new();

        public ObservableCollection<HeThongNhatKy> FilteredLogs
        {
            get => _filteredLogs;
            set { _filteredLogs = value; OnPropertyChanged(); }
        }

        public NhatKyHeThongViewModel(QuanLyKhachSanContext db)
        {
            _db = db;
            _ = LoadDataAsync(); // Gọi bất đồng bộ nhưng không block UI
        }

        public async Task LoadDataAsync()
        {
            // Sử dụng AsNoTracking để tối ưu hiệu năng vì trang này chỉ xem
            var logs = await _db.HeThongNhatKys
                .AsNoTracking()
                .OrderByDescending(x => x.ThoiGian)
                .Take(500)
                .ToListAsync();

            _allLogs = logs;
            FilteredLogs = new ObservableCollection<HeThongNhatKy>(logs);
        }

        public void Filter(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                FilteredLogs = new ObservableCollection<HeThongNhatKy>(_allLogs);
                return;
            }

            var lowerKey = keyword.ToLower();
            var result = _allLogs.Where(x =>
                (x.ThaoTac != null && x.ThaoTac.ToLower().Contains(lowerKey)) ||
                (x.MaNhanVien != null && x.MaNhanVien.ToLower().Contains(lowerKey)) ||
                (x.ChiTiet != null && x.ChiTiet.ToLower().Contains(lowerKey))).ToList();

            FilteredLogs = new ObservableCollection<HeThongNhatKy>(result);
        }
    }
}