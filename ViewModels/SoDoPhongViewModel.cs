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

public class SoDoPhongViewModel : BaseViewModel
{
    private readonly RoomService _roomService;
    private readonly KhachHangService _khachHangService;
    private readonly DatPhongService _datPhongService;
    private readonly SemaphoreSlim _khoaPhong = new(1, 1);
    private readonly SemaphoreSlim _khoaKhachHang = new(1, 1);
    private readonly SemaphoreSlim _khoaDatPhong = new(1, 1);

    private CancellationTokenSource? _ctsTimKhach;
    private int _phienTimKhach;

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

    // ── Detail & Booking state ──────────────────────────────────────────────
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

    // Booking form properties
    private DateTime _ngayNhan = DateTime.Today;
    private DateTime _ngayTra = DateTime.Today.AddDays(1);
    private string _khachHangSearchText = "";
    private KhachHang? _selectedKhach;
    private ObservableCollection<KhachHang> _khachHangResults = new();
    private string _totalPriceText = "0 ₫";
    private List<TienNghiItem> _roomAmenities = new();

    public DateTime NgayNhan { get => _ngayNhan; set { if (SetProperty(ref _ngayNhan, value)) CapNhatTienTamTinh(); } }
    public DateTime NgayTra { get => _ngayTra; set { if (SetProperty(ref _ngayTra, value)) CapNhatTienTamTinh(); } }
    public string KhachHangSearchText
    {
        get => _khachHangSearchText;
        set
        {
            if (SetProperty(ref _khachHangSearchText, value))
                BatDauTimKhachHang(value);
        }
    }
    public KhachHang? SelectedKhach { get => _selectedKhach; set { if (SetProperty(ref _selectedKhach, value)) CapNhatTienTamTinh(); } }
    public ObservableCollection<KhachHang> KhachHangResults => _khachHangResults;
    public string TotalPriceText { get => _totalPriceText; set => SetProperty(ref _totalPriceText, value); }
    public List<TienNghiItem> RoomAmenities { get => _roomAmenities; set => SetProperty(ref _roomAmenities, value); }

    private string _newKhachTen = "";
    private string _newKhachSdt = "";
    private string _newKhachCccd = "";
    private string _newKhachDiaChi = "";
    private string _newKhachPassport = "";
    private string _newKhachVisa = "";
    private string _newKhachQuocTich = "";

    public string NewKhachTen { get => _newKhachTen; set => SetProperty(ref _newKhachTen, value); }
    public string NewKhachSdt { get => _newKhachSdt; set => SetProperty(ref _newKhachSdt, value); }
    public string NewKhachCccd { get => _newKhachCccd; set => SetProperty(ref _newKhachCccd, value); }
    public string NewKhachDiaChi { get => _newKhachDiaChi; set => SetProperty(ref _newKhachDiaChi, value); }

    public string NewKhachPassport
    {
        get => _newKhachPassport;
        set => SetProperty(ref _newKhachPassport, value);
    }

    public string NewKhachVisa
    {
        get => _newKhachVisa;
        set => SetProperty(ref _newKhachVisa, value);
    }

    public string NewKhachQuocTich
    {
        get => _newKhachQuocTich;
        set
        {
            if (SetProperty(ref _newKhachQuocTich, value))
            {
                OnPropertyChanged(nameof(IsKhachNuocNgoai));
                OnPropertyChanged(nameof(IsKhachTrongNuoc));

                if (!IsKhachNuocNgoai)
                {
                    NewKhachPassport = "";
                    NewKhachVisa = "";
                }
                else
                {
                    NewKhachCccd = "";
                }
            }
        }
    }

    public bool IsKhachNuocNgoai => !IsQuocTichVietNam(NewKhachQuocTich);
    public bool IsKhachTrongNuoc => !IsKhachNuocNgoai;

    // PTT05 (Reserved) properties
    private string _reservedGuestName = "";
    private string _reservedNgayNhan = "";
    private string _reservedNgayTra = "";
    private string _reservedTienCoc = "—";

    public string ReservedGuestName { get => _reservedGuestName; set => SetProperty(ref _reservedGuestName, value); }
    public string ReservedNgayNhan { get => _reservedNgayNhan; set => SetProperty(ref _reservedNgayNhan, value); }
    public string ReservedNgayTra { get => _reservedNgayTra; set => SetProperty(ref _reservedNgayTra, value); }
    public string ReservedTienCoc { get => _reservedTienCoc; set => SetProperty(ref _reservedTienCoc, value); }

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

        // Group by floor
        _filteredRooms.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PhongCardViewModel.Tang)));
        // Sort by floor and room number
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.Tang), ListSortDirection.Ascending));
        _filteredRooms.SortDescriptions.Add(new SortDescription(nameof(PhongCardViewModel.SoPhongSort), ListSortDirection.Ascending));

        FilterCommand = new RelayCommand(p => SelectedFilter = p?.ToString() ?? "all");
        RefreshCommand = new RelayCommand(async _ => await TaiDuLieuAsync());
        SelectKhachCommand = new RelayCommand(p => SelectedKhach = p as KhachHang);
        DatPhongCommand = new RelayCommand(async _ => await ThucHienDatPhongAsync());
        CheckInCommand = new RelayCommand(async _ => await ThucHienNhanPhongAsync());
    }

    private static bool IsQuocTichVietNam(string? quocTich)
    {
        if (string.IsNullOrWhiteSpace(quocTich)) return true;
        var s = quocTich.Trim().ToLowerInvariant();
        return s == "vn" ||
               s.Contains("viet") ||
               s.Contains("việt") ||
               s.Contains("vietnam") ||
               s.Contains("việt nam") ||
               s.Contains("vietnamese");
    }

    // Tải danh sách phòng và tình trạng đặt phòng hiện tại từ cơ sở dữ liệu.
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

    private void BatDauXuLyKhiChonPhong(PhongCardViewModel vm)
    {
        _ctsChonPhong?.Cancel();
        _ctsChonPhong = new CancellationTokenSource();
        int phien = Interlocked.Increment(ref _phienChonPhong);
        _ = XuLyKhiChonPhongAsync(vm, phien, _ctsChonPhong.Token);
    }

    // Cập nhật toàn bộ thông tin panel chi tiết khi người dùng chọn một phòng.
    private async Task XuLyKhiChonPhongAsync(PhongCardViewModel vm, int phien, CancellationToken token)
    {
        try
        {
            await _khoaPhong.WaitAsync(token);
            try
            {
                var amenities = await _roomService.LayTienNghiPhongAsync(vm.MaPhong);
                if (token.IsCancellationRequested || phien != _phienChonPhong) return;

                RoomAmenities = amenities.Select(a => new TienNghiItem { TenTienNghi = a.MaTienNghiNavigation.TenTienNghi }).ToList();

                if (vm.MaTrangThaiPhong == "PTT05")
                {
                    var booking = await _roomService.LayDatPhongChoNhanTheoPhongAsync(vm.MaPhong);
                    if (token.IsCancellationRequested || phien != _phienChonPhong) return;

                    if (booking != null)
                    {
                        ReservedGuestName = booking.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "—";
                        ReservedNgayNhan = booking.NgayNhan.ToString("dd/MM/yyyy");
                        ReservedNgayTra = booking.NgayTra.ToString("dd/MM/yyyy");
                        ReservedTienCoc = "Chưa thu";
                    }
                }
            }
            finally
            {
                _khoaPhong.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải chi tiết phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SelectedKhach = null;
        KhachHangSearchText = "";
        NgayNhan = DateTime.Today;
        NgayTra = DateTime.Today.AddDays(1);
        CapNhatTienTamTinh();
    }

    private void BatDauTimKhachHang(string tuKhoa)
    {
        _ctsTimKhach?.Cancel();
        _ctsTimKhach = new CancellationTokenSource();
        int phien = Interlocked.Increment(ref _phienTimKhach);
        _ = TimKhachHangAsync(tuKhoa, phien, _ctsTimKhach.Token);
    }

    // Tìm khách hàng theo từ khóa, dùng ngưỡng tối thiểu 2 ký tự để giảm truy vấn thừa.
    private async Task TimKhachHangAsync(string tuKhoa, int phien, CancellationToken token)
    {
        if (tuKhoa.Length < 2)
        {
            _khachHangResults.Clear();
            return;
        }

        try
        {
            await Task.Delay(250, token);
            if (token.IsCancellationRequested || phien != _phienTimKhach) return;

            await _khoaKhachHang.WaitAsync(token);
            try
            {
                var ketQua = await _khachHangService.SearchKhachHangAsync(tuKhoa);
                if (token.IsCancellationRequested || phien != _phienTimKhach) return;

                _khachHangResults.Clear();
                foreach (var khach in ketQua) _khachHangResults.Add(khach);
            }
            finally
            {
                _khoaKhachHang.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tìm khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Tính tiền tạm tính theo số đêm và giá phòng hiện tại.
    private void CapNhatTienTamTinh()
    {
        if (SelectedRoom == null) return;
        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;
        decimal tongTien = SelectedRoom.GiaPhong * soDem;
        TotalPriceText = $"{tongTien:N0} ₫";
    }

    // Tạo đặt phòng mới cho khách đã chọn hoặc khách mới nhập nhanh.
    private async Task ThucHienDatPhongAsync()
    {
        try
        {
            if (SelectedRoom == null)
            {
                MessageBox.Show("Vui lòng chọn phòng trước khi đặt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            KhachHang khachMucTieu;
            if (SelectedKhach != null)
            {
                khachMucTieu = SelectedKhach;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(KhachHangSearchText))
                {
                    MessageBox.Show("Vui lòng chọn hoặc nhập tên khách hàng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (IsKhachNuocNgoai)
                {
                    if (string.IsNullOrWhiteSpace(NewKhachQuocTich))
                    {
                        MessageBox.Show("Vui lòng nhập quốc tịch cho khách nước ngoài.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(NewKhachPassport) && string.IsNullOrWhiteSpace(NewKhachVisa))
                    {
                        MessageBox.Show("Khách nước ngoài cần nhập Passport hoặc Visa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                var passport = IsKhachNuocNgoai ? NewKhachPassport : null;
                var visa = IsKhachNuocNgoai ? NewKhachVisa : null;
                var quocTich = IsKhachNuocNgoai
                    ? NewKhachQuocTich
                    : (string.IsNullOrWhiteSpace(NewKhachQuocTich) ? null : NewKhachQuocTich);

                _ctsTimKhach?.Cancel(); // ngăn truy vấn tìm khách chạy song song
                await _khoaKhachHang.WaitAsync();
                try
                {
                    khachMucTieu = await _khachHangService.TimHoacTaoAsync(
                        KhachHangSearchText,
                        NewKhachSdt,
                        NewKhachCccd,
                        null,
                        NewKhachDiaChi,
                        passport,
                        visa,
                        quocTich);
                }
                finally
                {
                    _khoaKhachHang.Release();
                }


            }

            var danhSachPhongDat = new List<(string MaPhong, DateTime NgayNhan, DateTime NgayTra)>
            {
                (SelectedRoom.MaPhong, NgayNhan, NgayTra)
            };

            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.TaoDatPhongAsync(khachMucTieu.MaKhachHang, danhSachPhongDat);
            }
            finally
            {
                _khoaDatPhong.Release();
            }
            MessageBox.Show("Đặt phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đặt phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Xác nhận nhận phòng cho booking đang ở trạng thái chờ nhận phòng.
    private async Task ThucHienNhanPhongAsync()
    {
        if (SelectedRoom == null) return;
        try
        {
            DatPhongChiTiet? booking;
            await _khoaPhong.WaitAsync();
            try
            {
                booking = await _roomService.LayDatPhongChoNhanTheoPhongAsync(SelectedRoom.MaPhong);
            }
            finally
            {
                _khoaPhong.Release();
            }
            if (booking == null) return;
            var maNhanVien = App.CurrentUser?.MaNhanVien ?? AppSession.MaNhanVien;
            if (string.IsNullOrWhiteSpace(maNhanVien))
            {
                MessageBox.Show("Không xác định được nhân viên hiện tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.CheckInAsync(booking.MaDatPhong, maNhanVien);
            }
            finally
            {
                _khoaDatPhong.Release();
            }
            MessageBox.Show("Nhận phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi nhận phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Lọc phòng theo trạng thái và từ khóa tìm kiếm.
    private bool LocPhong(object obj)
    {
        if (obj is not PhongCardViewModel vm) return false;

        bool matchesFilter = _selectedFilter == "all" || vm.MaTrangThaiPhong == _selectedFilter;
        if (!matchesFilter) return false;

        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var kw = _searchText.Trim().ToLower();
        return vm.MaPhong.ToLower().Contains(kw) || vm.TenLoaiPhong.ToLower().Contains(kw);
    }

    // Tách tầng từ mã phòng, ví dụ P101 -> 1, 201 -> 2.
    private static int LayTangTuMaPhong(string maPhong)
    {
        if (string.IsNullOrWhiteSpace(maPhong)) return 0;
        var s = maPhong.Trim();
        if (s.Length >= 2 && char.IsLetter(s[0]) && int.TryParse(s[1].ToString(), out int f1)) return f1;
        if (s.Length >= 1 && int.TryParse(s[0].ToString(), out int f0)) return f0;
        return 0;
    }
}

public class TienNghiItem { public string TenTienNghi { get; set; } = ""; }

public class PhongCardViewModel
{
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

    public SolidColorBrush BadgeBackground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(209, 250, 229)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(255, 228, 230)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(241, 245, 249)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(224, 231, 255)),
        _ => new SolidColorBrush(Color.FromRgb(241, 245, 249)),
    };

    public SolidColorBrush BadgeForeground => MaTrangThaiPhong switch
    {
        "PTT01" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
        "PTT02" => new SolidColorBrush(Color.FromRgb(225, 29, 72)),
        "PTT03" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        "PTT04" => new SolidColorBrush(Color.FromRgb(100, 116, 139)),
        "PTT05" => new SolidColorBrush(Color.FromRgb(99, 102, 241)),
        _ => new SolidColorBrush(Color.FromRgb(100, 116, 139)),
    };

    public Color ShadowColor => MaTrangThaiPhong switch
    {
        "PTT01" => Color.FromRgb(16, 185, 129),
        "PTT02" => Color.FromRgb(225, 29, 72),
        "PTT03" => Color.FromRgb(245, 158, 11),
        "PTT05" => Color.FromRgb(99, 102, 241),
        _ => Color.FromRgb(37, 99, 235),
    };

    public string StatusIcon => MaTrangThaiPhong switch
    {
        "PTT01" => "✨",
        "PTT02" => "👤",
        "PTT03" => "🧹",
        "PTT04" => "🛠️",
        "PTT05" => "📅",
        _ => "ℹ️"
    };

    public string? GuestName { get; set; }
    public string InfoText => !string.IsNullOrEmpty(GuestName) ? GuestName : "Chưa có thông tin";
    public string CaptionText => $"{TenLoaiPhong}";
}
