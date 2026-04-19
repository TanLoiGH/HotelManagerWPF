using System;
using System.Threading.Tasks;
using System.Windows;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class CaiDatViewModel : BaseViewModel
{
    private readonly EmployeeService _employeeSvc;
    private readonly IAuthService _authSvc;
    private readonly IHoaDonService _hoaDonSvc;

    // Cài đặt hệ thống
    private string _hotelName = "";
    private string _hotelAddress = "";
    private string _hotelPhone = "";
    private string _hotelEmail = "";
    private string _defaultCheckIn = "14:00";
    private string _defaultCheckOut = "12:00";
    private int _vatPercent = 10;

    // Thông tin tài khoản
    private string _maNhanVien = "";
    private string _tenDangNhap = "";
    private string _quyen = "";
    private string _chucVu = "";
    private string _hoTen = "";
    private string _soDienThoai = "";
    private string _email = "";
    private string _diaChi = "";

    // Đổi mật khẩu
    private string _matKhauCu = "";
    private string _matKhauMoi = "";
    private string _xacNhanMatKhau = "";

    // Commands
    public RelayCommand UpdateInfoCommand { get; }
    public RelayCommand ChangePasswordCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveSystemCommand { get; }
    public RelayCommand ReloadSystemCommand { get; }

    // CHỈ DÙNG 1 CONSTRUCTOR NÀY
    public CaiDatViewModel(EmployeeService employeeSvc, IAuthService authSvc, IHoaDonService hoaDonSvc)
    {
        _employeeSvc = employeeSvc;
        _authSvc = authSvc;
        _hoaDonSvc = hoaDonSvc;

        // Khởi tạo các Command
        SaveSystemCommand = new RelayCommand(_ => ExecuteSaveSystem());
        ReloadSystemCommand = new RelayCommand(_ => LoadSystemSettings());
        UpdateInfoCommand = new RelayCommand(_ => ExecuteUpdateInfoAsync(), _ => CanUpdate);
        ChangePasswordCommand = new RelayCommand(_ => ExecuteChangePasswordAsync(), _ => CanChangePassword);
        CancelCommand = new RelayCommand(_ => ExecuteCancel());

        // Load dữ liệu ngay khi khởi tạo
        LoadSystemSettings();
        _ = LoadCurrentUserInfoAsync();
    }

    #region Properties (Binding dữ liệu)

    public string HotelName { get => _hotelName; set => SetProperty(ref _hotelName, value); }
    public string HotelAddress { get => _hotelAddress; set => SetProperty(ref _hotelAddress, value); }
    public string HotelPhone { get => _hotelPhone; set => SetProperty(ref _hotelPhone, value); }
    public string HotelEmail { get => _hotelEmail; set => SetProperty(ref _hotelEmail, value); }
    public string DefaultCheckIn { get => _defaultCheckIn; set => SetProperty(ref _defaultCheckIn, value); }
    public string DefaultCheckOut { get => _defaultCheckOut; set => SetProperty(ref _defaultCheckOut, value); }
    public int VatPercent { get => _vatPercent; set => SetProperty(ref _vatPercent, value); }

    public string MaNhanVien { get => _maNhanVien; private set => SetProperty(ref _maNhanVien, value); }
    public string TenDangNhap { get => _tenDangNhap; private set => SetProperty(ref _tenDangNhap, value); }
    public string Quyen { get => _quyen; private set => SetProperty(ref _quyen, value); }
    public string ChucVu { get => _chucVu; private set => SetProperty(ref _chucVu, value); }

    public string HoTen
    {
        get => _hoTen;
        set { if (SetProperty(ref _hoTen, value)) UpdateInfoCommand.RaiseCanExecuteChanged(); }
    }
    public string SoDienThoai
    {
        get => _soDienThoai;
        set { if (SetProperty(ref _soDienThoai, value)) UpdateInfoCommand.RaiseCanExecuteChanged(); }
    }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string DiaChi { get => _diaChi; set => SetProperty(ref _diaChi, value); }

    public string MatKhauCu
    {
        get => _matKhauCu;
        set { if (SetProperty(ref _matKhauCu, value)) ChangePasswordCommand.RaiseCanExecuteChanged(); }
    }
    public string MatKhauMoi
    {
        get => _matKhauMoi;
        set { if (SetProperty(ref _matKhauMoi, value)) ChangePasswordCommand.RaiseCanExecuteChanged(); }
    }
    public string XacNhanMatKhau
    {
        get => _xacNhanMatKhau;
        set { if (SetProperty(ref _xacNhanMatKhau, value)) ChangePasswordCommand.RaiseCanExecuteChanged(); }
    }

    public bool CanUpdate => !string.IsNullOrWhiteSpace(HoTen) && !string.IsNullOrWhiteSpace(SoDienThoai);
    public bool CanChangePassword => !string.IsNullOrWhiteSpace(MatKhauCu) && !string.IsNullOrWhiteSpace(MatKhauMoi) && (MatKhauMoi == XacNhanMatKhau) && MatKhauMoi.Length >= 6;

    #endregion

    #region Methods (Logic xử lý)

    private void LoadSystemSettings()
    {
        try
        {
            var s = SystemSettingsService.Load();
            HotelName = s.HotelName;
            HotelAddress = s.HotelAddress;
            HotelPhone = s.HotelPhone;
            HotelEmail = s.HotelEmail;
            DefaultCheckIn = s.DefaultCheckIn;
            DefaultCheckOut = s.DefaultCheckOut;
            VatPercent = s.VatPercent;
        }
        catch (Exception ex) { Logger.LogError("Lỗi load settings", ex); }
    }

    private async Task LoadCurrentUserInfoAsync()
    {
        string? maNv = AppSession.MaNhanVien;
        if (string.IsNullOrWhiteSpace(maNv)) return;

        try
        {
            var nv = await _employeeSvc.LayNhanVienAsync(maNv);
            if (nv == null) return;

            var tk = await _employeeSvc.LayThongTinTaiKhoanAsync(maNv, AppSession.TenDangNhap);

            MaNhanVien = nv.MaNhanVien;
            HoTen = nv.TenNhanVien ?? "";
            SoDienThoai = nv.DienThoai ?? "";
            Email = nv.Email ?? "";
            DiaChi = nv.DiaChi ?? "";
            ChucVu = nv.ChucVu ?? "";
            TenDangNhap = tk?.TenDangNhap ?? AppSession.TenDangNhap ?? "";
            Quyen = tk?.TenQuyen ?? AppSession.MaQuyen ?? "";
        }
        catch (Exception ex) { Logger.LogError("Lỗi load info", ex); }
    }

    private async void ExecuteSaveSystem()
    {
        try
        {
            SystemSettingsService.Save(new SystemSettings
            {
                HotelName = HotelName,
                HotelAddress = HotelAddress,
                HotelPhone = HotelPhone,
                HotelEmail = HotelEmail,
                DefaultCheckIn = DefaultCheckIn,
                DefaultCheckOut = DefaultCheckOut,
                VatPercent = VatPercent
            });

            // Cập nhật VAT cho hóa đơn chưa thanh toán
            await _hoaDonSvc.CapNhatVatChoHoaDonDangMoAsync(VatPercent);

            MessageBox.Show("Đã lưu cài đặt hệ thống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
    }

    private async void ExecuteUpdateInfoAsync()
    {
        try
        {
            await _employeeSvc.CapNhatThongTinCaNhanAsync(MaNhanVien, HoTen, SoDienThoai, Email, DiaChi);
            AppSession.TenNhanVien = HoTen;
            MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo");
        }
        catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
    }

    private async void ExecuteChangePasswordAsync()
    {
        try
        {
            await _authSvc.DoiMatKhauAsync(MaNhanVien, TenDangNhap, MatKhauCu, MatKhauMoi);
            MatKhauCu = MatKhauMoi = XacNhanMatKhau = "";
            MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
        }
        catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
    }

    private void ExecuteCancel()
    {
        LoadSystemSettings();
        _ = LoadCurrentUserInfoAsync();
    }

    #endregion
}