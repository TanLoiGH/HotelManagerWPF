using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public partial class SoDoPhongViewModel : BaseViewModel
{
    private readonly RoomService _roomService;
    private readonly KhachHangService _khachHangService;
    private readonly DatPhongService _datPhongService;

    private readonly SemaphoreSlim _khoaPhong = new(1, 1);
    private readonly SemaphoreSlim _khoaKhachHang = new(1, 1);
    private readonly SemaphoreSlim _khoaDatPhong = new(1, 1);

    private CancellationTokenSource? _ctsChonPhong;
    private int _phienChonPhong;

    // ── Collections ────────────────────────────────────────────────────────
    private List<PhongCardViewModel> _allPhongs = new();
    private readonly ListCollectionView _filteredRooms;
    public ICollectionView FilteredRooms => _filteredRooms;

    // ── Filter & Search state ───────────────────────────────────────────────
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _filteredRooms.Refresh();
        }
    }

    private string _selectedFilter = "all";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
                _filteredRooms.Refresh();
        }
    }

    // ── Detail state ──────────────────────────────────────────────
    private PhongCardViewModel? _selectedRoom;
    public PhongCardViewModel? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value))
            {
                OnPropertyChanged(nameof(IsRoomSelected));
                OnPropertyChanged(nameof(IsRoomAvailable));
                OnPropertyChanged(nameof(IsRoomReserved));
                OnPropertyChanged(nameof(IsRoomOccupied));
                if (value != null) BatDauXuLyKhiChonPhong(value);
            }
        }
    }

    public bool IsRoomSelected => SelectedRoom != null;
    public bool IsRoomAvailable => SelectedRoom?.MaTrangThaiPhong == "PTT01";
    public bool IsRoomReserved => SelectedRoom?.MaTrangThaiPhong == "PTT05";
    public bool IsRoomOccupied => SelectedRoom != null && !IsRoomAvailable && !IsRoomReserved;

    public string RoomCountText => $"{_allPhongs.Count} phòng";

    // ── Loading state ───────────────────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ── Commands ────────────────────────────────────────────────────────────
    public ICommand FilterCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectKhachCommand { get; }
    public ICommand DatPhongCommand { get; }
    public ICommand CheckInCommand { get; }

    // ── Constructor ─────────────────────────────────────────────────────────
    public SoDoPhongViewModel(RoomService roomService, KhachHangService khachHangService, DatPhongService datPhongService)
    {
        _roomService = roomService;
        _khachHangService = khachHangService;
        _datPhongService = datPhongService;

        _filteredRooms = (ListCollectionView)CollectionViewSource.GetDefaultView(_allPhongs);
        _filteredRooms.Filter = LocPhong;

        _filteredRooms.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PhongCardViewModel.Tang)));
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.Tang), ListSortDirection.Ascending));
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.SoPhongSort), ListSortDirection.Ascending));

        FilterCommand = new RelayCommand(p => SelectedFilter = p?.ToString() ?? "all");
        RefreshCommand = new RelayCommand(async _ => await TaiDuLieuAsync());
        SelectKhachCommand = new RelayCommand(p => SelectedKhach = p as KhachHang);
        DatPhongCommand = new RelayCommand(async _ => await ThucHienDatPhongAsync());
        CheckInCommand = new RelayCommand(async _ => await ThucHienNhanPhongAsync());
    }

    public async Task TaiDuLieuAsync()
    {
        IsLoading = true;
        try
        {
            await _khoaPhong.WaitAsync();
            try
            {
                var rooms = await _roomService.LayDanhSachPhongChiTietAsync();
                var activeBookings = await _roomService.LayChiTietDatPhongDangHoatDongAsync();

                _allPhongs.Clear();
                foreach (var p in rooms)
                {
                    var booking = activeBookings.FirstOrDefault(b => b.MaPhong == p.MaPhong);
                    _allPhongs.Add(new PhongCardViewModel
                    {
                        MaPhong = p.MaPhong,
                        TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                        TenTrangThai = p.MaTrangThaiPhongNavigation?.TenTrangThai ?? "",
                        MaTrangThaiPhong = p.MaTrangThaiPhong ?? "PTT01",
                        SoNguoiToiDa = p.MaLoaiPhongNavigation.SoNguoiToiDa ?? 0,
                        GiaPhong = p.MaLoaiPhongNavigation.GiaPhong,
                        Tang = LayTangTuMaPhong(p.MaPhong),
                        GuestName = booking?.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang
                    });
                }
                _filteredRooms.Refresh();
                OnPropertyChanged(nameof(RoomCountText));
            }
            finally
            {
                _khoaPhong.Release();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải phòng: {ex.Message}", "Lỗi");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool LocPhong(object obj)
    {
        if (obj is not PhongCardViewModel vm) return false;

        bool matchesFilter = _selectedFilter == "all" || vm.MaTrangThaiPhong == _selectedFilter;
        if (!matchesFilter) return false;

        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var kw = _searchText.Trim().ToLower();
        return vm.MaPhong.ToLower().Contains(kw) || vm.TenLoaiPhong.ToLower().Contains(kw);
    }

    private static int LayTangTuMaPhong(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong)) return 0;
        var s = maPhong.Trim();
        if (s.Length >= 2 && char.IsLetter(s[0]) && int.TryParse(s[1].ToString(), out int f1)) return f1;
        if (s.Length >= 1 && int.TryParse(s[0].ToString(), out int f0)) return f0;
        return 0;
    }
}

// Giữ nguyên 2 class phụ trợ này ở file gốc
public class TienNghiItem { public string TenTienNghi { get; set; } = ""; }

public class PhongCardViewModel
{
    // ... (Giữ nguyên toàn bộ code của PhongCardViewModel như bạn đã viết) ...
    public string MaPhong { get; set; } = "";
    public string TenLoaiPhong { get; set; } = "";
    public string TenTrangThai { get; set; } = "";
    public string MaTrangThaiPhong { get; set; } = "";
    public int SoNguoiToiDa { get; set; }
    public decimal GiaPhong { get; set; }
    public string GiaPhongText => GiaPhong.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    public int Tang { get; set; }

    public int SoPhongSort
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MaPhong)) return 0;
            var digits = new string(MaPhong.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : 0;
        }
    }

    public SolidColorBrush CardBackground => new SolidColorBrush(Color.FromRgb(255, 255, 255));
    public SolidColorBrush BadgeBackground => MaTrangThaiPhong switch { "PTT01" => new SolidColorBrush(Color.FromRgb(209, 250, 229)), "PTT02" => new SolidColorBrush(Color.FromRgb(255, 228, 230)), "PTT03" => new SolidColorBrush(Color.FromRgb(254, 243, 199)), "PTT04" => new SolidColorBrush(Color.FromRgb(241, 245, 249)), "PTT05" => new SolidColorBrush(Color.FromRgb(224, 231, 255)), _ => new SolidColorBrush(Color.FromRgb(241, 245, 249)), };
    public SolidColorBrush BadgeForeground => MaTrangThaiPhong switch { "PTT01" => new SolidColorBrush(Color.FromRgb(16, 185, 129)), "PTT02" => new SolidColorBrush(Color.FromRgb(225, 29, 72)), "PTT03" => new SolidColorBrush(Color.FromRgb(245, 158, 11)), "PTT04" => new SolidColorBrush(Color.FromRgb(100, 116, 139)), "PTT05" => new SolidColorBrush(Color.FromRgb(99, 102, 241)), _ => new SolidColorBrush(Color.FromRgb(100, 116, 139)), };
    public Color ShadowColor => MaTrangThaiPhong switch { "PTT01" => Color.FromRgb(16, 185, 129), "PTT02" => Color.FromRgb(225, 29, 72), "PTT03" => Color.FromRgb(245, 158, 11), "PTT05" => Color.FromRgb(99, 102, 241), _ => Color.FromRgb(37, 99, 235), };
    public string StatusIcon => MaTrangThaiPhong switch { "PTT01" => "✨", "PTT02" => "👤", "PTT03" => "🧹", "PTT04" => "🛠️", "PTT05" => "📅", _ => "ℹ️" };
    public string? GuestName { get; set; }
    public string InfoText => !string.IsNullOrEmpty(GuestName) ? GuestName : "Chưa có thông tin";
    public string CaptionText => $"{TenLoaiPhong}";
}