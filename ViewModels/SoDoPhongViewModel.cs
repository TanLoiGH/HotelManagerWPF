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
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

// Định nghĩa class bổ trợ cho Tiện nghi
public class TienNghiItem
{
    public string TenTienNghi { get; set; } = "";
}

public partial class SoDoPhongViewModel : BaseViewModel, IDisposable
{
    private readonly PhongService _roomService;
    private readonly KhachHangService _khachHangService;
    private readonly DatPhongService _datPhongService;

    // Các khóa Semaphore khớp với file DatPhong.cs
    private readonly SemaphoreSlim _khoaPhong = new(1, 1);
    private readonly SemaphoreSlim _khoaKhachHang = new(1, 1);
    private readonly SemaphoreSlim _khoaDatPhong = new(1, 1);

    private CancellationTokenSource? _ctsChonPhong;
    private int _phienChonPhong;

    // Tên biến Collection khớp với file DatPhong.cs (_allPhongs)
    private readonly ObservableCollection<PhongCardViewModel> _allPhongs = new();
    private readonly ListCollectionView _filteredRooms;
    public ICollectionView FilteredRooms => _filteredRooms;

    #region FILTER & SEARCH PROPERTIES

    private string _searchText = "";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value)) _filteredRooms.Refresh();
        }
    }

    private string _selectedFilter = "all";

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value)) _filteredRooms.Refresh();
        }
    }

    #endregion

    #region SELECTED ROOM STATE

    private PhongCardViewModel? _selectedRoom;

    public PhongCardViewModel? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value))
            {
                NotifyRoomStateChanged();
                if (value != null) BatDauXuLyKhiChonPhong(value);
            }
        }
    }

    public bool IsRoomSelected => SelectedRoom != null;
    public bool IsRoomAvailable => SelectedRoom?.MaTrangThaiPhong == PhongTrangThaiCodes.Trong;
    public bool IsRoomReserved => SelectedRoom?.MaTrangThaiPhong == PhongTrangThaiCodes.DaDat;
    public bool IsRoomOccupied => SelectedRoom != null && !IsRoomAvailable && !IsRoomReserved;

    public string RoomCountText => $"{_allPhongs.Count} phòng";

    // Các thuộc tính phục vụ Multi-select
    public List<PhongCardViewModel> SelectedRooms => _allPhongs.Where(p => p.IsSelected).ToList();
    public bool IsMultiSelectMode => SelectedRooms.Count > 1;
    public string SelectionTitle => IsMultiSelectMode ? $"ĐANG CHỌN {SelectedRooms.Count} PHÒNG" : "THÔNG TIN PHÒNG";

    // Mã đặt phòng để hủy (Sửa lỗi CS0103 SelectedMaDatPhong)
    private string _selectedMaDatPhong = "";

    public string SelectedMaDatPhong
    {
        get => _selectedMaDatPhong;
        set => SetProperty(ref _selectedMaDatPhong, value);
    }

    #endregion

    #region LOADING & COMMANDS

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand FilterCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectKhachCommand { get; }
    public ICommand DatPhongCommand { get; }
    public ICommand DoiPhongCommand { get; }
    public ICommand HuyDatPhongCommand { get; }
    public ICommand HuyPhongRiengLeCommand { get; }
    public ICommand HoanThanhDonDepCommand { get; }
    public ICommand CheckInRiengLeCommand { get; }

    #endregion

    public SoDoPhongViewModel(PhongService roomService, KhachHangService khachHangService,
        DatPhongService datPhongService)
    {
        _roomService = roomService;
        _khachHangService = khachHangService;
        _datPhongService = datPhongService;

        _filteredRooms = (ListCollectionView)CollectionViewSource.GetDefaultView(_allPhongs);
        _filteredRooms.Filter = LocPhong;
        _filteredRooms.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PhongCardViewModel.Tang)));
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.Tang),
            ListSortDirection.Ascending));
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.SoPhongSort),
            ListSortDirection.Ascending));

        // Khởi tạo các Command
        FilterCommand = new RelayCommand(p => SelectedFilter = p?.ToString() ?? "all");
        RefreshCommand = new AsyncRelayCommand(async _ => await TaiDuLieuAsync());
        SelectKhachCommand = new RelayCommand(p => SelectedKhach = p as KhachHang);
        DatPhongCommand = new AsyncRelayCommand(async _ => await ThucHienDatPhongAsync());
        CheckInRiengLeCommand = new AsyncRelayCommand(async _ => await ThucHienCheckInRiengLeAsync(),
            _ => SelectedRoom != null && !string.IsNullOrEmpty(SelectedRoom.MaDatPhong));
        DoiPhongCommand = new AsyncRelayCommand(async _ => await ThucHienDoiPhongAsync());
        HuyPhongRiengLeCommand = new AsyncRelayCommand(async _ => await ThucHienHuyPhongRiengLeAsync());
        HuyDatPhongCommand = new AsyncRelayCommand(async _ => await ThucHienHuyDatPhongAsync());
        HoanThanhDonDepCommand = new AsyncRelayCommand(async _ => await ThucHienHoanThanhDonDepAsync());
    }

    public async Task TaiDuLieuAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            await _khoaPhong.WaitAsync();
            try
            {
                var rooms = await _roomService.LayDanhSachPhongChiTietAsync();
                var activeBookings = await _roomService.LayChiTietDatPhongDangHoatDongAsync();
                var dictBookings = activeBookings.ToDictionary(b => b.MaPhong);

                _allPhongs.Clear();
                foreach (var p in rooms)
                {
                    dictBookings.TryGetValue(p.MaPhong, out var booking);

                    var vm = new PhongCardViewModel(p, booking);
                    vm.OnSelectedChanged = () =>
                    {
                        OnPropertyChanged(nameof(SelectedRooms));
                        OnPropertyChanged(nameof(IsMultiSelectMode));
                        OnPropertyChanged(nameof(SelectionTitle));
                        CapNhatTienTamTinh();
                    };
                    _allPhongs.Add(vm);
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
            Logger.LogError("Lỗi sơ đồ phòng", ex);
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

    private void NotifyRoomStateChanged()
    {
        OnPropertyChanged(nameof(IsRoomSelected));
        OnPropertyChanged(nameof(IsRoomAvailable));
        OnPropertyChanged(nameof(IsRoomReserved));
        OnPropertyChanged(nameof(IsRoomOccupied));
    }

    public void ClearAllSelectedRooms()
    {
        foreach (var p in _allPhongs.Where(x => x.IsSelected))
        {
            p.IsSelected = false;
        }

        NotifyRoomStateChanged();
    }

    public void Dispose()
    {
        _ctsChonPhong?.Cancel();
        _ctsChonPhong?.Dispose();
        _khoaPhong.Dispose();
        _khoaKhachHang.Dispose();
        _khoaDatPhong.Dispose();
    }
}

public class PhongCardViewModel : BaseViewModel
{
    public PhongCardViewModel(Phong p, DatPhongChiTiet? booking)
    {
        MaPhong = p.MaPhong;
        TenLoaiPhong = p.MaLoaiPhongNavigation?.TenLoaiPhong ?? "";
        TenTrangThai = p.MaTrangThaiPhongNavigation?.TenTrangThai ?? "";
        MaTrangThaiPhong = p.MaTrangThaiPhong ?? PhongTrangThaiCodes.Trong;
        SoNguoiToiDa = p.MaLoaiPhongNavigation?.SoNguoiToiDa ?? 0;
        GiaPhong = p.MaLoaiPhongNavigation?.GiaPhong ?? 0;
        if (booking != null)
        {
            MaDatPhong = booking.MaDatPhong;
            GuestName = booking.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang;
        }

        Tang = ExtractFloor(MaPhong);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value)) OnSelectedChanged?.Invoke();
        }
    }

    public Action? OnSelectedChanged { get; set; }
    public string MaPhong { get; }
    public string MaDatPhong { get; } = "";
    public string TenLoaiPhong { get; }
    public string TenTrangThai { get; }
    public string MaTrangThaiPhong { get; }
    public int SoNguoiToiDa { get; }
    public decimal GiaPhong { get; }
    public int Tang { get; }
    public string? GuestName { get; }
    public string GiaPhongText => GiaPhong.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    public string InfoText => !string.IsNullOrEmpty(GuestName) ? GuestName : "Phòng trống";
    public int SoPhongSort => int.TryParse(new string(MaPhong.Where(char.IsDigit).ToArray()), out var n) ? n : 0;

    private static int ExtractFloor(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong)) return 0;
        var digits = maPhong.Where(char.IsDigit).ToArray();
        return (digits.Length > 0 && int.TryParse(digits[0].ToString(), out var f)) ? f : 0;
    }
}