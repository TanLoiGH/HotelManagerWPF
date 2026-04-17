using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi;         // Chứa App
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers; // Chứa AppSession
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

//Đặt phòng
public partial class SoDoPhongViewModel
{
    private DateTime _ngayNhan = DateTime.Today;
    private DateTime _ngayTra = DateTime.Today.AddDays(1);
    private string _totalPriceText = "0 ₫";
    private decimal _tienCoc = 0; // Biến Tiền Cọc mới
    private List<TienNghiItem> _roomAmenities = new();

    public DateTime NgayNhan { get => _ngayNhan; set { if (SetProperty(ref _ngayNhan, value)) CapNhatTienTamTinh(); } }
    public DateTime NgayTra { get => _ngayTra; set { if (SetProperty(ref _ngayTra, value)) CapNhatTienTamTinh(); } }
    public string TotalPriceText { get => _totalPriceText; set => SetProperty(ref _totalPriceText, value); }
    public decimal TienCoc { get => _tienCoc; set => SetProperty(ref _tienCoc, value); } // Property Tiền Cọc
    public List<TienNghiItem> RoomAmenities { get => _roomAmenities; set => SetProperty(ref _roomAmenities, value); }

    // PTT05 (Reserved) properties
    private string _reservedGuestName = "";
    private string _reservedNgayNhan = "";
    private string _reservedNgayTra = "";
    private string _reservedTienCoc = "—";

    public string ReservedGuestName { get => _reservedGuestName; set => SetProperty(ref _reservedGuestName, value); }
    public string ReservedNgayNhan { get => _reservedNgayNhan; set => SetProperty(ref _reservedNgayNhan, value); }
    public string ReservedNgayTra { get => _reservedNgayTra; set => SetProperty(ref _reservedNgayTra, value); }
    public string ReservedTienCoc { get => _reservedTienCoc; set => SetProperty(ref _reservedTienCoc, value); }

    private void CapNhatTienTamTinh()
    {
        // Lấy danh sách các phòng đang được tích chọn
        var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();

        // Nếu không có phòng nào được tích, nhưng có SelectedRoom đang được click, thì tính cho 1 phòng đó
        if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
            selectedRooms.Add(SelectedRoom);

        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;

        // Tổng tiền = Tổng giá các phòng được chọn * số đêm
        decimal tongTien = selectedRooms.Sum(r => r.GiaPhong) * soDem;

        if (selectedRooms.Count > 1)
            TotalPriceText = $"{tongTien:N0} ₫ ({selectedRooms.Count} phòng)";
        else
            TotalPriceText = $"{tongTien:N0} ₫";
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

                        // Hiển thị tiền cọc nếu đã thu
                        decimal tienCocDaThu = booking.MaDatPhongNavigation?.TienCoc ?? 0;
                        ReservedTienCoc = tienCocDaThu > 0 ? $"{tienCocDaThu:N0} ₫" : "Chưa thu";
                        SelectedMaDatPhong = booking.MaDatPhong; // Lưu lại mã để hủy
                    }
                }
            }
            finally
            {
                _khoaPhong.Release();
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải chi tiết phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // RESET FORM SAU KHI CHỌN PHÒNG
        SelectedKhach = null;
        KhachHangSearchText = "";
        NewKhachHoTen = "";
        NewKhachQuocTich = "Việt Nam";
        NgayNhan = DateTime.Today;
        NgayTra = DateTime.Today.AddDays(1);
        TienCoc = 0; // Đưa tiền cọc về 0 để không bị lưu nhầm từ thao tác trước
        CapNhatTienTamTinh();
    }

    private async Task ThucHienDatPhongAsync()
    {
        try
        {
            // 1. Thu thập tất cả phòng được chọn (từ Checkbox)
            var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();

            // 2. Fallback: Nếu không tích cái nào, lấy phòng đang chọn ở Detail Panel
            if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
                selectedRooms.Add(SelectedRoom);

            // SỬA: Check mảng thay vì check cứng SelectedRoom
            if (!selectedRooms.Any())
            {
                MessageBox.Show("Vui lòng chọn phòng trước khi đặt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NgayNhan.Date < DateTime.Today)
            {
                MessageBox.Show("Ngày nhận không được ở quá khứ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NgayTra.Date < NgayNhan.Date)
            {
                MessageBox.Show("Ngày trả phải sau hoặc bằng ngày nhận.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                var quocTich = IsKhachNuocNgoai ? NewKhachQuocTich : (string.IsNullOrWhiteSpace(NewKhachQuocTich) ? null : NewKhachQuocTich);

                _ctsTimKhach?.Cancel();
                await _khoaKhachHang.WaitAsync();
                try
                {
                    khachMucTieu = await _khachHangService.TimHoacTaoAsync(
                        KhachHangSearchText, NewKhachSdt, NewKhachCccd, null, NewKhachDiaChi, passport, visa, quocTich);
                }
                finally
                {
                    _khoaKhachHang.Release();
                }
            }

            // SỬA: Chuyển toàn bộ danh sách phòng đã chọn vào mảng đặt phòng
            var danhSachPhongDat = selectedRooms.Select(p => (p.MaPhong, NgayNhan, NgayTra)).ToList();

            // Lấy mã nhân viên đăng nhập để gán là nhân viên lập phiếu đặt
            var maNhanVienDat = AppSession.MaNhanVien ?? "NV001";
            await _khoaDatPhong.WaitAsync();
            try
            {
                // GỌI SERVICE TRUYỀN VÀO MÃ NHÂN VIÊN VÀ TIỀN CỌC
                await _datPhongService.TaoDatPhongAsync(khachMucTieu.MaKhachHang, danhSachPhongDat, maNhanVienDat, TienCoc, 1);
            }
            finally
            {
                _khoaDatPhong.Release();
            }

            MessageBox.Show("Đặt phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            foreach (var p in _allPhongs) p.IsSelected = false;
            SelectedRoom = null;
            TienCoc = 0; // Reset tiền cọc
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            string errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += "\n\nChi tiết từ DB:\n" + ex.InnerException.Message;
            }
            MessageBox.Show($"Lỗi đặt phòng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

            var maNhanVien = AppSession.MaNhanVien;
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

    private async Task ThucHienDoiPhongAsync()
    {
        // Đảm bảo đang chọn một phòng có khách (Đang ở)
        if (SelectedRoom == null || SelectedRoom.MaTrangThaiPhong != "PTT02")
        {
            MessageBox.Show("Vui lòng chọn một phòng đang có khách (Đang ở) để chuyển phòng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string maDatPhongActive = "";
        using (var db = new QuanLyKhachSanContext())
        {
            // Tìm Mã Đặt Phòng của phòng đang ở hiện tại
            var ct = await db.DatPhongChiTiets
                .Include(c => c.MaDatPhongNavigation)
                .FirstOrDefaultAsync(c => c.MaPhong == SelectedRoom.MaPhong
                                       && c.MaDatPhongNavigation.TrangThai == "Đang ở");

            if (ct == null)
            {
                MessageBox.Show("Không tìm thấy đơn đặt phòng đang ở cho phòng này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            maDatPhongActive = ct.MaDatPhong;
        }

        // Mở Dialog
        var dialog = new DoiPhongDialog(maDatPhongActive, SelectedRoom.MaPhong);
        if (dialog.ShowDialog() == true)
        {
            // Nếu chuyển thành công, load lại danh sách phòng trên Sơ đồ
            await TaiDuLieuAsync();
        }
    }

    private async Task ThucHienHuyDatPhongAsync()
    {
        if (string.IsNullOrEmpty(SelectedMaDatPhong)) return;
        if (!ConfirmHelper.Confirm("Xác nhận hủy đơn đặt phòng này? Tiền cọc (nếu có) sẽ được tính vào Chi phí hoàn trả.", "Hủy đặt phòng")) return;

        try
        {
            await _datPhongService.HuyDatPhongAsync(SelectedMaDatPhong, AppSession.MaNhanVien ?? "NV001");
            MessageBox.Show("Đã hủy đặt phòng thành công.");
            await TaiDuLieuAsync();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
    }

    private async Task ThucHienHoanThanhDonDepAsync()
    {
        if (SelectedRoom == null) return;
        try
        {
            await _datPhongService.HoanThanhDonDepAsync(SelectedRoom.MaPhong);
            await TaiDuLieuAsync();
        }
        catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
    }

}