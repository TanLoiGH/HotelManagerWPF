using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class HoaDonPageViewModel : BaseViewModel, IDisposable
{
    // Caching state
    private static readonly TimeSpan ThoiGianHetHanCache = TimeSpan.FromSeconds(20);
    private static DateTime _thoiDiemTaiCache = DateTime.MinValue;
    private static List<HoaDonDongViewModel> _cacheHoaDon = new();

    // Giữ lại bộ lọc khi chuyển trang
    private static string _tuKhoaTimKiemGiuLai = "";
    private static string _trangThaiLocGiuLai = "";

    private readonly IHoaDonService _hoaDonService;
    private readonly DispatcherTimer _boDemTimKiem;

    private string _tuKhoaTimKiem = "";
    private string _trangThaiLoc = "";
    private bool _isLoading;

    private ObservableCollection<HoaDonDongViewModel> _tatCaHoaDon = new();
    private ListCollectionView _hoaDonDaLoc;

    public HoaDonPageViewModel(IHoaDonService hoaDonService)
    {
        _hoaDonService = hoaDonService;

        _hoaDonDaLoc = (ListCollectionView)CollectionViewSource.GetDefaultView(_tatCaHoaDon);
        _hoaDonDaLoc.Filter = LocHoaDon;

        // Senior Fix: Khởi tạo Debouncer an toàn
        _boDemTimKiem = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _boDemTimKiem.Tick += BoDemTimKiem_Tick;

        // Phục hồi trạng thái cũ
        _tuKhoaTimKiem = _tuKhoaTimKiemGiuLai;
        _trangThaiLoc = _trangThaiLocGiuLai;

        TaiDuLieuCommand = new AsyncRelayCommand(async _ => await TaiDuLieuAsync());
        LocTrangThaiCommand = new RelayCommand(p => TrangThaiLoc = p?.ToString() ?? "");
    }

    #region PROPERTIES

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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

    #endregion

    #region COMMANDS

    public ICommand TaiDuLieuCommand { get; }
    public ICommand LocTrangThaiCommand { get; }

    #endregion

    #region MAIN LOGIC

    public async Task TaiDuLieuAsync(bool buocTaiMoi = false)
    {
        if (IsLoading) return;

        try
        {
            if (!buocTaiMoi && CacheHopLe())
            {
                CapNhatDanhSachHoaDon(_cacheHoaDon);
                return;
            }

            IsLoading = true; // Bật cờ loading báo cho UI quay mòng mòng

            var hoaDons = await _hoaDonService.LayHoaDonsAsync();
            var danhSach = await Task.Run(() => ChuyenHoaDon(hoaDons)); // Offload việc mapping sang thread nền

            // Cập nhật Cache
            _cacheHoaDon = danhSach;
            _thoiDiemTaiCache = TimeHelper.GetVietnamTime();

            CapNhatDanhSachHoaDon(danhSach);
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi tải hóa đơn", ex);
            MessageBox.Show($"Lỗi tải danh sách hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            IsLoading = false; // Tắt cờ loading
        }
    }

    private static List<HoaDonDongViewModel> ChuyenHoaDon(List<HoaDon> hoaDons)
    {
        var danhSach = new List<HoaDonDongViewModel>(hoaDons.Count);
        foreach (var hoaDon in hoaDons)
        {
            // 1. Lấy dữ liệu gốc
            bool isChuaThanhToan = hoaDon.TrangThai == "Chưa thanh toán";
            decimal tienPhong = hoaDon.TienPhong ?? 0;
            decimal tienDichVu = hoaDon.TienDichVu ?? 0;
            decimal tongThanhToan = hoaDon.TongThanhToan ?? 0;

            // 2. Logic UX: Ẩn số "0 đ" vô duyên khi bill chưa chốt
            string tienPhongStr = (tienPhong == 0 && isChuaThanhToan) ? "—" : $"{tienPhong:N0} ₫";
            string tienDvStr = (tienDichVu == 0 && isChuaThanhToan) ? "—" : $"{tienDichVu:N0} ₫";
            string tongTienStr = (tongThanhToan == 0 && isChuaThanhToan) ? "Đang cập nhật..." : $"{tongThanhToan:N0} ₫";
            danhSach.Add(new HoaDonDongViewModel
            {
                MaHoaDon = hoaDon.MaHoaDon,
                TenKhachHang = hoaDon.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "(Không có KH)",
                NgayLapHienThi = hoaDon.NgayLap?.ToString("dd/MM/yyyy") ?? "",

                TienPhong = tienPhong,
                Vat = hoaDon.Vat ?? 0,

                TienPhongHienThi = tienPhongStr,
                TienDichVuHienThi = tienDvStr,
                TongThanhToanHienThi = tongTienStr,

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

    private void BoDemTimKiem_Tick(object? sender, EventArgs e)
    {
        _boDemTimKiem.Stop();
        _hoaDonDaLoc.Refresh();
        LuuTrangThaiLoc();
    }

    private void LuuTrangThaiLoc()
    {
        _tuKhoaTimKiemGiuLai = _tuKhoaTimKiem;
        _trangThaiLocGiuLai = _trangThaiLoc;
    }

    private static bool CacheHopLe()
        => _cacheHoaDon.Count > 0 && (TimeHelper.GetVietnamTime() - _thoiDiemTaiCache) <= ThoiGianHetHanCache;

    private bool LocHoaDon(object obj)
    {
        if (obj is not HoaDonDongViewModel vm) return false;

        bool khopTrangThai = string.IsNullOrEmpty(_trangThaiLoc) || vm.TrangThai == _trangThaiLoc;
        if (!khopTrangThai) return false;

        if (string.IsNullOrWhiteSpace(_tuKhoaTimKiem)) return true;

        var tuKhoa = _tuKhoaTimKiem.Trim().ToLower();
        return vm.MaHoaDon.ToLower().Contains(tuKhoa) || vm.TenKhachHang.ToLower().Contains(tuKhoa);
    }

    public void Dispose()
    {
        // Senior Fix: Dọn dẹp Timer để chống rác bộ nhớ khi đóng View
        _boDemTimKiem.Stop();
        _boDemTimKiem.Tick -= BoDemTimKiem_Tick;
    }

    #endregion
}

public class HoaDonDongViewModel
{
    // Senior Refactor: Tái sử dụng Brush và Đóng băng (Freeze) chúng 
    // để GPU render list dữ liệu cả ngàn dòng vẫn mượt mà.
    private static readonly SolidColorBrush BrushChuaThanhToan = CreateFrozenBrush(250, 204, 21);
    private static readonly SolidColorBrush BrushDaThanhToan = CreateFrozenBrush(34, 197, 94);
    private static readonly SolidColorBrush BrushDaHuy = CreateFrozenBrush(148, 163, 184);
    private static readonly SolidColorBrush BrushMacDinh = CreateFrozenBrush(59, 130, 246);

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze(); // Chìa khóa tăng hiệu năng WPF nằm ở hàm này!
        return brush;
    }

    public string MaHoaDon { get; set; } = "";
    public string TenKhachHang { get; set; } = "";
    public string NgayLapHienThi { get; set; } = "";

    public decimal TienPhong { get; set; }
    public decimal Vat { get; set; }

    public string TienPhongHienThi { get; set; } = "";
    public string TienDichVuHienThi { get; set; } = "";
    public string TongThanhToanHienThi { get; set; } = "";
    public string TrangThai { get; set; } = "";

    public string TienVatHienThi => (TienPhong * (Vat / 100m)).ToString("N0") + " ₫";

    public SolidColorBrush StatusColor => TrangThai switch
    {
        HoaDonTrangThaiTexts.ChuaThanhToan => BrushChuaThanhToan,
        HoaDonTrangThaiTexts.DaThanhToan => BrushDaThanhToan,
        HoaDonTrangThaiTexts.DaHuy => BrushDaHuy,
        _ => BrushMacDinh
    };
}