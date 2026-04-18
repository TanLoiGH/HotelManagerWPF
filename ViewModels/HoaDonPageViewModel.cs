using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class HoaDonPageViewModel : BaseViewModel
{
    private static readonly TimeSpan ThoiGianHetHanCache = TimeSpan.FromSeconds(20);
    private static DateTime _thoiDiemTaiCache = DateTime.MinValue;
    private static List<HoaDonDongViewModel> _cacheHoaDon = new();
    private static string _tuKhoaTimKiemGiuLai = "";
    private static string _trangThaiLocGiuLai = "";

    private readonly HoaDonService _hoaDonService;
    private readonly DispatcherTimer _boDemTimKiem;
    private string _tuKhoaTimKiem = "";
    private string _trangThaiLoc = "";
    private ObservableCollection<HoaDonDongViewModel> _tatCaHoaDon = new();
    private ListCollectionView _hoaDonDaLoc;

    public HoaDonPageViewModel(HoaDonService hoaDonService)
    {
        _hoaDonService = hoaDonService;
        _hoaDonDaLoc = (ListCollectionView)CollectionViewSource.GetDefaultView(_tatCaHoaDon);
        _hoaDonDaLoc.Filter = LocHoaDon;

        _boDemTimKiem = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _boDemTimKiem.Tick += (_, _) =>
        {
            _boDemTimKiem.Stop();
            _hoaDonDaLoc.Refresh();
            LuuTrangThaiLoc();
        };

        _tuKhoaTimKiem = _tuKhoaTimKiemGiuLai;
        _trangThaiLoc = _trangThaiLocGiuLai;

        TaiDuLieuCommand = new RelayCommand(async _ => await TaiDuLieuAsync());
        LocTrangThaiCommand = new RelayCommand(p => TrangThaiLoc = p?.ToString() ?? "");
    }

    public string TuKhoaTimKiem
    {
        get => _tuKhoaTimKiem;
        set
        {
            if (SetProperty(ref _tuKhoaTimKiem, value))
            {
                _boDemTimKiem.Stop();
                _boDemTimKiem.Start();
            }
        }
    }

    public string TrangThaiLoc
    {
        get => _trangThaiLoc;
        set
        {
            if (SetProperty(ref _trangThaiLoc, value))
            {
                _hoaDonDaLoc.Refresh();
                LuuTrangThaiLoc();
            }
        }
    }

    public ICollectionView HoaDonDaLoc => _hoaDonDaLoc;

    public ICommand TaiDuLieuCommand { get; }
    public ICommand LocTrangThaiCommand { get; }

    public async Task TaiDuLieuAsync(bool buocTaiMoi = false)
    {
        try
        {
            if (!buocTaiMoi && CacheHopLe())
            {
                CapNhatDanhSachHoaDon(_cacheHoaDon);
                return;
            }

            var hoaDons = await _hoaDonService.LayHoaDonsAsync();
            var danhSach = ChuyenHoaDon(hoaDons);

            _cacheHoaDon = danhSach;
            _thoiDiemTaiCache = DateTime.Now;

            CapNhatDanhSachHoaDon(danhSach);
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            MessageBox.Show($"Lỗi tải hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static List<HoaDonDongViewModel> ChuyenHoaDon(List<HoaDon> hoaDons)
    {
        var danhSach = new List<HoaDonDongViewModel>(hoaDons.Count);
        foreach (var hoaDon in hoaDons)
        {
            danhSach.Add(new HoaDonDongViewModel
            {
                MaHoaDon = hoaDon.MaHoaDon,
                TenKhachHang = hoaDon.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
                NgayLapHienThi = hoaDon.NgayLap?.ToString("dd/MM/yyyy") ?? "",
                TienPhongHienThi = (hoaDon.TienPhong ?? 0).ToString("N0") + " ₫",
                TienDichVuHienThi = (hoaDon.TienDichVu ?? 0).ToString("N0") + " ₫",
                TongThanhToanHienThi = (hoaDon.TongThanhToan ?? 0).ToString("N0") + " ₫",
                TrangThai = hoaDon.TrangThai ?? string.Empty,
            });
        }

        return danhSach;
    }

    private void CapNhatDanhSachHoaDon(IEnumerable<HoaDonDongViewModel> danhSach)
    {
        _tatCaHoaDon.Clear();
        foreach (var item in danhSach)
            _tatCaHoaDon.Add(item);

        _hoaDonDaLoc.Refresh();
    }

    private static bool CacheHopLe()
        => _cacheHoaDon.Count > 0 && (DateTime.Now - _thoiDiemTaiCache) <= ThoiGianHetHanCache;

    private void LuuTrangThaiLoc()
    {
        _tuKhoaTimKiemGiuLai = _tuKhoaTimKiem;
        _trangThaiLocGiuLai = _trangThaiLoc;
    }

    private bool LocHoaDon(object obj)
    {
        if (obj is not HoaDonDongViewModel vm) return false;

        bool khopTrangThai = string.IsNullOrEmpty(_trangThaiLoc) || vm.TrangThai == _trangThaiLoc;
        if (!khopTrangThai) return false;

        if (string.IsNullOrWhiteSpace(_tuKhoaTimKiem)) return true;

        var tuKhoa = _tuKhoaTimKiem.Trim().ToLower();
        return vm.MaHoaDon.ToLower().Contains(tuKhoa) || vm.TenKhachHang.ToLower().Contains(tuKhoa);
    }
}

public class HoaDonDongViewModel
{
    public string MaHoaDon { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayLapHienThi { get; set; } = "";
    public string TienPhongHienThi { get; set; } = "";
    public string TienDichVuHienThi { get; set; } = "";
    public string TongThanhToanHienThi { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public SolidColorBrush StatusColor => TrangThai switch
    {
        "Chưa thanh toán" => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
        "Đã thanh toán" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
        "Đã hủy" => new SolidColorBrush(Color.FromRgb(148, 163, 184)),
        _ => new SolidColorBrush(Color.FromRgb(59, 130, 246))
    };
}
