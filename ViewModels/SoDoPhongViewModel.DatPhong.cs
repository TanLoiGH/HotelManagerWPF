using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

// Phần logic Đặt phòng, Nhận phòng, Đổi phòng và Hủy phòng
public partial class SoDoPhongViewModel
{
    #region PROPERTIES - ĐẶT PHÒNG & THÔNG TIN LƯU TRÚ

    private DateTime _ngayNhan = DateTime.Today;
    private DateTime _ngayTra = DateTime.Today.AddDays(1);
    private string _totalPriceText = "0 ₫";
    private decimal _tienCoc = 0;
    private List<TienNghiItem> _roomAmenities = new();

    public DateTime NgayNhan
    {
        get => _ngayNhan;
        set
        {
            if (SetProperty(ref _ngayNhan, value)) CapNhatTienTamTinh();
        }
    }

    public DateTime NgayTra
    {
        get => _ngayTra;
        set
        {
            if (SetProperty(ref _ngayTra, value)) CapNhatTienTamTinh();
        }
    }

    public string TotalPriceText
    {
        get => _totalPriceText;
        set => SetProperty(ref _totalPriceText, value);
    }

    public decimal TienCoc
    {
        get => _tienCoc;
        set
        {
            if (value < 0) value = 0;
            decimal total = TinhTongTienPhongTamThoi();
            if (value > total) value = total;
            if (SetProperty(ref _tienCoc, value)) CapNhatTienTamTinh();
        }
    }

    public List<TienNghiItem> RoomAmenities
    {
        get => _roomAmenities;
        set => SetProperty(ref _roomAmenities, value);
    }

    // Thông tin dành cho phòng đã đặt (Reserved - PTT05)
    private string _reservedGuestName = "";
    private string _reservedNgayNhan = "";
    private string _reservedNgayTra = "";
    private string _reservedTienCoc = "—";

    public string ReservedGuestName
    {
        get => _reservedGuestName;
        set => SetProperty(ref _reservedGuestName, value);
    }

    public string ReservedNgayNhan
    {
        get => _reservedNgayNhan;
        set => SetProperty(ref _reservedNgayNhan, value);
    }

    public string ReservedNgayTra
    {
        get => _reservedNgayTra;
        set => SetProperty(ref _reservedNgayTra, value);
    }

    public string ReservedTienCoc
    {
        get => _reservedTienCoc;
        set => SetProperty(ref _reservedTienCoc, value);
    }

    #endregion

    #region PROPERTIES - THÔNG TIN KHÁCH HÀNG (Dùng cho Form đặt phòng)

    private KhachHang? _selectedKhach;

    public KhachHang? SelectedKhach
    {
        get => _selectedKhach;
        set
        {
            if (SetProperty(ref _selectedKhach, value))
            {
                if (_selectedKhach != null)
                {
                    // 1. Điền tên lên ô Search & Đóng danh sách Dropdown
                    _khachHangSearchText = _selectedKhach.TenKhachHang;
                    OnPropertyChanged(nameof(KhachHangSearchText));
                    IsSearchOpen = false;

                    // 2. Hiện badge Hạng khách (VD: Khách VIP, Khách quen...)
                    CoHangKhachHang = true;
                    LoaiKhachHienThi = _selectedKhach.MaLoaiKhachNavigation?.TenLoaiKhach ?? "Hạng mặc định";

                    // 3. AUTO-FILL THÔNG TIN VÀO FORM NHẬP LIỆU
                    NewKhachHoTen = _selectedKhach.TenKhachHang;

                    // (Lưu ý: Chỗ này bro gõ đúng tên thuộc tính SĐT và CCCD trong DB của bro nhé)
                    NewKhachSdt = _selectedKhach.DienThoai ?? "";
                    NewKhachDiaChi = _selectedKhach.DiaChi ?? "";
                    NewKhachQuocTich = _selectedKhach.QuocTich ?? "Việt Nam";

                    // 4. AUTO-SWITCH FORM THEO QUỐC TỊCH
                    IsKhachNuocNgoai = !string.IsNullOrEmpty(_selectedKhach.QuocTich) &&
                                       _selectedKhach.QuocTich.Trim().ToLower() != "việt nam";

                    if (IsKhachNuocNgoai)
                    {
                        NewKhachPassport = _selectedKhach.Passport ?? "";
                        NewKhachVisa = _selectedKhach.Visa ?? "";
                        NewKhachCccd = ""; // Clear CCCD
                    }
                    else
                    {
                        NewKhachCccd = _selectedKhach.Cccd ?? "";
                        NewKhachPassport = "";
                        NewKhachVisa = "";
                    }

                    // 5. Tự động bung Expander ra cho nhân viên xem data đã được điền chưa
                    IsKhachExpanded = true;
                }
                else
                {
                    CoHangKhachHang = false;
                }
            }
        }
    }

    private bool _isKhachExpanded;

    public bool IsKhachExpanded
    {
        get => _isKhachExpanded;
        set => SetProperty(ref _isKhachExpanded, value);
    }

    // 1. Khai báo danh sách kết quả tìm kiếm và trạng thái mở ListBox
    private ObservableCollection<KhachHang> _khachHangResults = new();

    public ObservableCollection<KhachHang> KhachHangResults
    {
        get => _khachHangResults;
        set => SetProperty(ref _khachHangResults, value);
    }

    private bool _isSearchOpen;

    public bool IsSearchOpen
    {
        get => _isSearchOpen;
        set => SetProperty(ref _isSearchOpen, value);
    }

    // Các thuộc tính phụ cho UI
    private bool _coHangKhachHang;

    public bool CoHangKhachHang
    {
        get => _coHangKhachHang;
        set => SetProperty(ref _coHangKhachHang, value);
    }

    private string _loaiKhachHienThi = "";

    public string LoaiKhachHienThi
    {
        get => _loaiKhachHienThi;
        set => SetProperty(ref _loaiKhachHienThi, value);
    }

    #region LOGIC TÌM KIẾM KHÁCH HÀNG + SMART AUTO-FILL

    private string _khachHangSearchText = "";

    public string KhachHangSearchText
    {
        get => _khachHangSearchText;
        set
        {
            if (SetProperty(ref _khachHangSearchText, value))
            {
                // 1. Gọi hàm tìm kiếm DB 
                TimKiemKhachHangAsync(value);

                // 2. SMART AUTO-FILL NÂNG CẤP (Phân biệt SĐT / CCCD / Passport)
                if (SelectedKhach == null || SelectedKhach.TenKhachHang != value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        string cleanText = value.Replace(" ", "").Trim();
                        bool isNumber = cleanText.All(char.IsDigit);

                        if (isNumber)
                        {
                            // TH1: Số điện thoại (Đúng 10 số và bắt đầu bằng số 0)
                            if (cleanText.Length == 10 && cleanText.StartsWith("0"))
                            {
                                NewKhachSdt = cleanText;
                                NewKhachCccd = ""; // Clear ô CCCD đi để tránh rác
                            }
                            // TH2: Căn cước công dân (12 số) hoặc CMND cũ (9 số)
                            else if (cleanText.Length == 12 || cleanText.Length == 9)
                            {
                                NewKhachCccd = cleanText;
                                NewKhachSdt = "";
                                IsKhachNuocNgoai = false; // Tự động set khách VN
                            }
                            // TH3: Lỡ gõ thiếu/dư số (Fallback) -> Ngắn thì cho vào SĐT, dài cho vào CCCD
                            else
                            {
                                if (cleanText.Length <= 11) NewKhachSdt = cleanText;
                                else NewKhachCccd = cleanText;
                            }
                        }
                        else
                        {
                            // TH4: Nhận diện Passport (1 chữ cái + 7 số, VD: B1234567, C7654321)
                            if (System.Text.RegularExpressions.Regex.IsMatch(cleanText, @"^[a-zA-Z]\d{7}$"))
                            {
                                NewKhachPassport = cleanText.ToUpper();
                                IsKhachNuocNgoai = true; // Auto gạt Checkbox Khách nước ngoài luôn!
                                NewKhachCccd = "";
                            }
                            else
                            {
                                // TH5: Chứa chữ bình thường -> Đẩy vào Họ tên
                                NewKhachHoTen = value;
                            }
                        }

                        IsKhachExpanded = true; // Tự bung form
                    }
                    else
                    {
                        // Clear toàn bộ form khi xóa trắng ô tìm kiếm
                        NewKhachHoTen = "";
                        NewKhachSdt = "";
                        NewKhachCccd = "";
                        NewKhachPassport = "";
                    }
                }
            }
        }
    }

    #endregion


    private string _newKhachSdt = "";

    public string NewKhachSdt
    {
        get => _newKhachSdt;
        set => SetProperty(ref _newKhachSdt, value);
    }

    private string _newKhachCccd = "";

    public string NewKhachCccd
    {
        get => _newKhachSdt;
        set => SetProperty(ref _newKhachSdt, value);
    }

    // Hàm tìm kiếm khách hàng
    private async void TimKiemKhachHangAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            KhachHangResults.Clear();
            IsSearchOpen = false;
            CoHangKhachHang = false;
            SelectedKhach = null;
            return;
        }

        try
        {
            // Chạy ngầm tránh đơ UI
            using var db = new QuanLyKhachSanContext();
            var kw = keyword.ToLower();

            // Tìm theo tên hoặc SĐT (Lấy top 5 cho nhẹ)
            var results = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.KhachHangs
                    .Include(k => k.MaLoaiKhachNavigation)
                    .Where(k => k.TenKhachHang.ToLower().Contains(kw) || k.DienThoai.Contains(kw) ||
                                k.Cccd.Contains(kw))
                    .Take(5)
            );

            Application.Current.Dispatcher.Invoke(() =>
            {
                KhachHangResults.Clear();
                foreach (var k in results)
                {
                    KhachHangResults.Add(k);
                }

                IsSearchOpen = KhachHangResults.Any();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi tìm kiếm khách hàng", ex);
        }
    }

    private string _newKhachDiaChi = "";

    public string NewKhachDiaChi
    {
        get => _newKhachDiaChi;
        set => SetProperty(ref _newKhachDiaChi, value);
    }

    private string _newKhachHoTen = "";

    public string NewKhachHoTen
    {
        get => _newKhachHoTen;
        set => SetProperty(ref _newKhachHoTen, value);
    }

    private bool _isKhachNuocNgoai;

    public bool IsKhachNuocNgoai
    {
        get => _isKhachNuocNgoai;
        set => SetProperty(ref _isKhachNuocNgoai, value);
    }

    private string _newKhachPassport = "";

    public string NewKhachPassport
    {
        get => _newKhachPassport;
        set => SetProperty(ref _newKhachPassport, value);
    }

    private string _newKhachVisa = "";

    public string NewKhachVisa
    {
        get => _newKhachVisa;
        set => SetProperty(ref _newKhachVisa, value);
    }

    private string _newKhachQuocTich = "Việt Nam";

    public string NewKhachQuocTich
    {
        get => _newKhachQuocTich;
        set => SetProperty(ref _newKhachQuocTich, value);
    }

    #endregion

    #region LOGIC TÍNH TOÁN & XỬ LÝ CHỌN PHÒNG

    private decimal TinhTongTienPhongTamThoi()
    {
        var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
        if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
            selectedRooms.Add(SelectedRoom);

        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;
        return selectedRooms.Sum(r => r.GiaPhong) * soDem;
    }

    public void CapNhatTienTamTinh()
    {
        var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
        if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
            selectedRooms.Add(SelectedRoom);

        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;

        decimal tongTienPhong = selectedRooms.Sum(r => r.GiaPhong) * soDem;
        decimal conLai = tongTienPhong - TienCoc;

        string suffix = selectedRooms.Count > 1 ? $" ({selectedRooms.Count} phòng)" : "";

        if (TienCoc > 0)
            TotalPriceText = $"{tongTienPhong:N0} ₫ \n Cọc: {TienCoc:N0} ₫ \n Còn: {conLai:N0}₫ {suffix}";
        else
            TotalPriceText = $"{tongTienPhong:N0} ₫{suffix}";
    }

    private void BatDauXuLyKhiChonPhong(PhongCardViewModel vm)
    {
        _ctsChonPhong?.Cancel();
        _ctsChonPhong = new CancellationTokenSource();
        int phien = Interlocked.Increment(ref _phienChonPhong);
        _ = XuLyKhiChonPhongAsync(vm, phien, _ctsChonPhong.Token);
    }

    private async Task XuLyKhiChonPhongAsync(PhongCardViewModel vm, int phien, CancellationToken token)
    {
        try
        {
            await _khoaPhong.WaitAsync(token);
            try
            {
                var amenities = await _roomService.LayTienNghiPhongAsync(vm.MaPhong);
                if (token.IsCancellationRequested || phien != _phienChonPhong) return;

                RoomAmenities = amenities
                    .Select(a => new TienNghiItem { TenTienNghi = a.MaTienNghiNavigation.TenTienNghi }).ToList();

                if (vm.MaTrangThaiPhong == "PTT05") // Đã đặt
                {
                    var booking = await _roomService.LayDatPhongChoNhanTheoPhongAsync(vm.MaPhong);
                    if (token.IsCancellationRequested || phien != _phienChonPhong) return;

                    if (booking != null)
                    {
                        ReservedGuestName = booking.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "—";
                        ReservedNgayNhan = booking.NgayNhan.ToString("dd/MM/yyyy");
                        ReservedNgayTra = booking.NgayTra.ToString("dd/MM/yyyy");

                        decimal tienCocDaThu = booking.MaDatPhongNavigation?.TienCoc ?? 0;
                        ReservedTienCoc = tienCocDaThu > 0 ? $"{tienCocDaThu:N0} ₫" : "Chưa thu";
                        SelectedMaDatPhong = booking.MaDatPhong;
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
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi tải chi tiết phòng", ex);
        }

        // Reset Form nhập liệu khách hàng mỗi khi chọn phòng mới để tránh nhầm lẫn
        SelectedKhach = null;
        KhachHangSearchText = "";
        NewKhachHoTen = "";
        NewKhachSdt = "";
        NewKhachCccd = "";
        NewKhachDiaChi = "";
        TienCoc = 0;
        CapNhatTienTamTinh();
    }

    #endregion

    #region EXECUTE COMMANDS (THỰC THI NGHIỆP VỤ)

    private async Task ThucHienDatPhongAsync()
    {
        try
        {
            var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
            if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
                selectedRooms.Add(SelectedRoom);

            var danhSachPhongDat = selectedRooms
                .GroupBy(p => p.MaPhong).Select(g => g.First())
                .Select(p => (p.MaPhong, NgayNhan, NgayTra)).ToList();

            if (!danhSachPhongDat.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất một phòng trống.", "Thông báo", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (NgayTra.Date <= NgayNhan.Date)
            {
                MessageBox.Show("Ngày trả phải sau ngày nhận ít nhất 1 ngày.", "Thông báo", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
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
                    MessageBox.Show("Vui lòng nhập tên khách hàng.", "Thông báo");
                    return;
                }

                var passport = IsKhachNuocNgoai ? NewKhachPassport : null;
                var visa = IsKhachNuocNgoai ? NewKhachVisa : null;
                var quocTich = IsKhachNuocNgoai ? NewKhachQuocTich : "Việt Nam";

                await _khoaKhachHang.WaitAsync();
                try
                {
                    khachMucTieu = await _khachHangService.TimHoacTaoAsync(
                        KhachHangSearchText, NewKhachSdt, NewKhachCccd, null, "LKH01", NewKhachDiaChi, passport, visa,
                        quocTich);
                }
                finally
                {
                    _khoaKhachHang.Release();
                }
            }

            var maNhanVien = AppSession.MaNhanVien ?? "NV001";
            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.TaoDatPhongAsync(khachMucTieu.MaKhachHang, danhSachPhongDat, maNhanVien, TienCoc,
                    1);
            }
            finally
            {
                _khoaDatPhong.Release();
            }

            MessageBox.Show("Đặt phòng thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearAllSelectedRooms();
            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi Đặt Phòng", ex);
            MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi");
        }
    }

    private async Task ThucHienCheckInRiengLeAsync()
    {
        if (SelectedRoom == null || string.IsNullOrEmpty(SelectedRoom.MaDatPhong)) return;

        var xacNhan = MessageBox.Show($"Xác nhận khách nhận phòng {SelectedRoom.MaPhong}?", "Nhận phòng",
            MessageBoxButton.YesNo);
        if (xacNhan != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.CheckInPhongRiengLeAsync(SelectedRoom.MaDatPhong, SelectedRoom.MaPhong,
                    AppSession.MaNhanVien ?? "NV001");
            }
            finally
            {
                _khoaDatPhong.Release();
            }

            MessageBox.Show("Nhận phòng thành công!");
            IsLoading = false;
            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ThucHienDoiPhongAsync()
    {
        if (SelectedRoom == null || SelectedRoom.MaTrangThaiPhong != "PTT02")
        {
            MessageBox.Show("Vui lòng chọn phòng đang có khách ở để đổi.");
            return;
        }

        string maDatPhong;
        using (var db = new QuanLyKhachSanContext())
        {
            var ct = await db.DatPhongChiTiets.FirstOrDefaultAsync(c =>
                c.MaPhong == SelectedRoom.MaPhong && c.MaDatPhongNavigation.TrangThai == "Đang ở");
            if (ct == null) return;
            maDatPhong = ct.MaDatPhong;
        }

        var dialog = new DoiPhongDialog(maDatPhong, SelectedRoom.MaPhong);
        if (dialog.ShowDialog() == true) await TaiDuLieuAsync();
    }

    private async Task ThucHienHuyDatPhongAsync()
    {
        if (SelectedRoom == null || string.IsNullOrEmpty(SelectedRoom.MaDatPhong)) return;

        decimal tienCoc;
        using (var db = new QuanLyKhachSanContext())
        {
            var dp = await db.DatPhongs.FindAsync(SelectedRoom.MaDatPhong);
            tienCoc = dp?.TienCoc ?? 0;
        }

        var dialog = new HuyPhongDialog(tienCoc, false) { Title = "HỦY TOÀN BỘ ĐƠN ĐẶT PHÒNG" };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.HuyDatPhongAsync(SelectedRoom.MaDatPhong, AppSession.MaNhanVien ?? "NV001",
                    dialog.LyDoHuy, dialog.TienHoanTra);
            }
            finally
            {
                _khoaDatPhong.Release();
            }

            MessageBox.Show("Đã hủy đơn đặt phòng!");
            IsLoading = false;
            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ThucHienHuyPhongRiengLeAsync()
    {
        if (SelectedRoom == null || string.IsNullOrEmpty(SelectedRoom.MaDatPhong)) return;

        var dialog = new HuyPhongDialog(0, true) { Title = $"HỦY LẺ PHÒNG {SelectedRoom.MaPhong}" };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.HuyPhongRiengLeAsync(SelectedRoom.MaDatPhong, SelectedRoom.MaPhong,
                    AppSession.MaNhanVien ?? "NV001", dialog.LyDoHuy);
            }
            finally
            {
                _khoaDatPhong.Release();
            }

            MessageBox.Show("Đã hủy lẻ phòng này!");
            IsLoading = false;
            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ThucHienHoanThanhDonDepAsync()
    {
        if (SelectedRoom == null) return;
        try
        {
            await _datPhongService.HoanThanhDonDepAsync(SelectedRoom.MaPhong);
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    #endregion
}