using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Helpers;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

// Logic Thanh toán & Trả phòng (Checkout)
public partial class SoDoPhongViewModel
{
    #region Thuộc tính hiển thị Thanh toán (Billing Properties)

    private decimal _billTienPhong;
    private decimal _billTienDichVu;
    private decimal _billGiamGia;
    private decimal _billVat;
    private decimal _billTongThanhToan;
    private decimal _billConLai;
    private decimal _billDaThu;

    public decimal BillTienPhong
    {
        get => _billTienPhong;
        set => SetProperty(ref _billTienPhong, value);
    }

    public decimal BillTienDichVu
    {
        get => _billTienDichVu;
        set => SetProperty(ref _billTienDichVu, value);
    }

    public decimal BillGiamGia
    {
        get => _billGiamGia;
        set => SetProperty(ref _billGiamGia, value);
    }

    public decimal BillVat
    {
        get => _billVat;
        set => SetProperty(ref _billVat, value);
    }

    public decimal BillTongThanhToan
    {
        get => _billTongThanhToan;
        set => SetProperty(ref _billTongThanhToan, value);
    }

    public decimal BillConLai
    {
        get => _billConLai;
        set => SetProperty(ref _billConLai, value);
    }

    public decimal BillDaThu
    {
        get => _billDaThu;
        set => SetProperty(ref _billDaThu, value);
    }

    private ObservableCollection<DichVuChiTiet> _billDichVus = new();
    public ObservableCollection<DichVuChiTiet> BillDichVus => _billDichVus;

    #endregion

    #region Commands Thanh toán

    public ICommand ThanhToanCommand { get; }
    public ICommand InTamTinhCommand { get; }

    #endregion

    #region Logic Xử lý chính (Main Logic)

    private async Task LoadThongTinThanhToanAsync(string maPhong)
    {
        try
        {
            IsLoading = true;

            var booking = await _datPhongService.LayDatPhongDangOTheoPhongAsync(maPhong);
            if (booking == null) return;

            var dichVus = await _datPhongService.LayDichVuTheoDatPhongAsync(booking.MaDatPhong);
            BillDichVus.Clear();
            foreach (var dv in dichVus) BillDichVus.Add(dv);

            // GIAO VIỆC TÍNH TOÁN CHO SERVICE (Chuẩn MVVM / Clean Architecture)
            decimal tienPhong =
                TinhToanService.TinhTienPhongThucTe(booking.DonGia, booking.NgayNhan, booking.NgayTra);
            decimal tienDichVu = TinhToanService.TinhTongTienDichVu(dichVus);

            var settings = CaiDatService.Load();

            var ketQua = TinhToanService.TinhToanToanBo(
                tienPhong: tienPhong,
                tienDichVu: tienDichVu,
                vatPercent: settings.VatPercent,
                giaTriKm: 0m,
                loaiKm: "",
                tienCoc: booking.MaDatPhongNavigation?.TienCoc ?? 0m,
                tongDaThuLichSu: 0m
            );

            // Gán kết quả tính sẵn lên UI
            BillTienPhong = ketQua.TienPhong;
            BillTienDichVu = ketQua.TienDichVu;
            BillGiamGia = ketQua.GiamGia;
            BillVat = ketQua.TienVat;
            BillTongThanhToan = ketQua.TongThanhToan;
            BillConLai = ketQua.ConLai;
            BillDaThu = booking.MaDatPhongNavigation?.TienCoc ?? 0m;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi nạp hóa đơn", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ThucHienThanhToanAsync()
    {
        if (SelectedRoom == null || BillConLai < 0) return;

        var xacNhan = MessageBox.Show(
            $"Xác nhận thanh toán và trả phòng {SelectedRoom.MaPhong}?\nTổng thu thêm: {BillConLai:N0} VNĐ",
            "Xác nhận thanh toán", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (xacNhan != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;

            await _datPhongService.ThanhToanVaTraPhongAsync(
                SelectedRoom.MaDatPhong,
                SelectedRoom.MaPhong,
                AppSession.MaNhanVien ?? "NV001",
                BillConLai
            );

            MessageBox.Show("Thanh toán thành công! Phòng đã được chuyển sang trạng thái chờ dọn dẹp.", "Thông báo");

            SelectedRoom = null;
            await TaiDuLieuAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi thanh toán: {ex.Message}", "Lỗi");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}