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
    public decimal TienCoc
    {
        get => _tienCoc;
        set
        {
            if (value < 0) value = 0;
            var total = TinhTongTienPhongTamThoi(); // Cần viết thêm phương thức này
            if (value > total) value = total;
            if (SetProperty(ref _tienCoc, value))
                CapNhatTienTamTinh();
        }
    }
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

    private decimal TinhTongTienPhongTamThoi()
    {
        var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
        if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
            selectedRooms.Add(SelectedRoom);
        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;
        return selectedRooms.Sum(r => r.GiaPhong) * soDem;
    }

    private void CapNhatTienTamTinh()
    {
        // 1. Lấy danh sách phòng
        var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
        if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
            selectedRooms.Add(SelectedRoom);

        // PHÁT TÍN HIỆU CẬP NHẬT TRẠNG THÁI CHO UI
        OnPropertyChanged(nameof(SelectedRooms));
        OnPropertyChanged(nameof(IsMultiSelectMode));
        OnPropertyChanged(nameof(SelectionTitle));

        // 2. Tính số đêm
        int soDem = (NgayTra - NgayNhan).Days;
        if (soDem < 1) soDem = 1;

        // 3. Tính toán tiền nong
        decimal tongTienPhong = selectedRooms.Sum(r => r.GiaPhong) * soDem;
        decimal conLai = tongTienPhong - TienCoc; // TienCoc là property bạn đã có

        // 4. Định dạng hiển thị
        string suffix = selectedRooms.Count > 1 ? $" ({selectedRooms.Count} phòng)" : "";

        if (TienCoc > 0)
        {
            // Hiển thị dạng: 1.000.000 - Cọc: 200.000 = Còn: 800.000
            TotalPriceText = $"{tongTienPhong:N0} ₫ - Cọc: {TienCoc:N0} ₫ = Còn: {conLai:N0} ₫{suffix}";
        }
        else
        {
            TotalPriceText = $"{tongTienPhong:N0} ₫{suffix}";
        }
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
            Logger.LogError("Lỗi", ex);
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
            // Gom phòng: Đảm bảo không bị trùng lặp khi vừa tick vừa chọn Detail
            var selectedRooms = _allPhongs.Where(p => p.IsSelected && p.MaTrangThaiPhong == "PTT01").ToList();
            if (!selectedRooms.Any() && SelectedRoom != null && IsRoomAvailable)
                selectedRooms.Add(SelectedRoom);

            // Dọn dẹp List (loại bỏ phòng trùng mã)
            var danhSachPhongDat = selectedRooms
                .GroupBy(p => p.MaPhong).Select(g => g.First())
                .Select(p => (p.MaPhong, NgayNhan, NgayTra)).ToList();

            if (!danhSachPhongDat.Any())
            {
                MessageBox.Show("Vui lòng chọn phòng trước khi đặt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NgayNhan.Date < DateTime.Today || NgayTra.Date < NgayNhan.Date)
            {
                MessageBox.Show("Ngày nhận/trả không hợp lệ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            KhachHang khachMucTieu;
            if (SelectedKhach != null)
            {
                khachMucTieu = SelectedKhach;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(KhachHangSearchText)) return;
                var passport = IsKhachNuocNgoai ? NewKhachPassport : null;
                var visa = IsKhachNuocNgoai ? NewKhachVisa : null;
                var quocTich = IsKhachNuocNgoai ? NewKhachQuocTich : null;

                await _khoaKhachHang.WaitAsync();
                try
                {
                    khachMucTieu = await _khachHangService.TimHoacTaoAsync(
                        KhachHangSearchText, NewKhachSdt, NewKhachCccd, null, "LKH01", NewKhachDiaChi, passport, visa, quocTich);
                }
                finally { _khoaKhachHang.Release(); }
            }

            var maNhanVienDat = AppSession.MaNhanVien ?? "NV001"; // LƯU Ý: Phải đảm bảo DB có nhân viên mã này!
            await _khoaDatPhong.WaitAsync();
            try
            {
                await _datPhongService.TaoDatPhongAsync(khachMucTieu.MaKhachHang, danhSachPhongDat, maNhanVienDat, TienCoc, 1);
            }
            finally { _khoaDatPhong.Release(); }

            MessageBox.Show("Đặt phòng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearAllSelectedRooms(); SelectedRoom = null; TienCoc = 0;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi Đặt Phòng", ex);
            // Đào sâu lấy InnerException cuối cùng để xem chính xác lỗi do cột nào
            Exception realEx = ex;
            while (realEx.InnerException != null) realEx = realEx.InnerException;
            MessageBox.Show($"Lỗi DB: {realEx.Message}", "Lỗi Đặt Phòng", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Logger.LogError("Lỗi", ex);
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
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            MessageBox.Show("Lỗi: " + ex.Message);
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
            Logger.LogError("Lỗi", ex); MessageBox.Show("Lỗi: " + ex.Message);
        }
    }

}