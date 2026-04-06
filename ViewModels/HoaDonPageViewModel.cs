using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class HoaDonPageViewModel : BaseViewModel
{
    private readonly HoaDonService _hoaDonService;
    private string _searchText = "";
    private string _filterStatus = "";
    private ObservableCollection<HoaDonRowViewModel> _allHoaDon = new();
    private ListCollectionView _filteredHoaDon;

    public HoaDonPageViewModel(HoaDonService hoaDonService)
    {
        _hoaDonService = hoaDonService;
        _filteredHoaDon = (ListCollectionView)CollectionViewSource.GetDefaultView(_allHoaDon);
        _filteredHoaDon.Filter = FilterHoaDon;

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        FilterCommand = new RelayCommand(p => FilterStatus = p?.ToString() ?? "");
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _filteredHoaDon.Refresh();
        }
    }

    public string FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
                _filteredHoaDon.Refresh();
        }
    }

    public ICollectionView FilteredHoaDon => _filteredHoaDon;

    public ICommand LoadDataCommand { get; }
    public ICommand FilterCommand { get; }

    public async Task LoadDataAsync()
    {
        try
        {
            var hds = await _hoaDonService.GetHoaDonsAsync();
            _allHoaDon.Clear();
            foreach (var h in hds)
            {
                _allHoaDon.Add(new HoaDonRowViewModel
                {
                    MaHoaDon = h.MaHoaDon,
                    TenKhachHang = h.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
                    NgayLapText = h.NgayLap?.ToString("dd/MM/yyyy") ?? "",
                    TienPhongText = (h.TienPhong ?? 0).ToString("N0") + " ₫",
                    TienDichVuText = (h.TienDichVu ?? 0).ToString("N0") + " ₫",
                    TongThanhToanText = (h.TongThanhToan ?? 0).ToString("N0") + " ₫",
                    TrangThai = h.TrangThai ?? string.Empty,
                });
            }
            _filteredHoaDon.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool FilterHoaDon(object obj)
    {
        if (obj is not HoaDonRowViewModel vm) return false;

        bool matchesStatus = string.IsNullOrEmpty(_filterStatus) || vm.TrangThai == _filterStatus;
        if (!matchesStatus) return false;

        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var kw = _searchText.Trim().ToLower();
        return vm.MaHoaDon.ToLower().Contains(kw) || vm.TenKhachHang.ToLower().Contains(kw);
    }
}

public class HoaDonRowViewModel
{
    public string MaHoaDon { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayLapText { get; set; } = "";
    public string TienPhongText { get; set; } = "";
    public string TienDichVuText { get; set; } = "";
    public string TongThanhToanText { get; set; } = "";
    public string TrangThai { get; set; } = "";
}
