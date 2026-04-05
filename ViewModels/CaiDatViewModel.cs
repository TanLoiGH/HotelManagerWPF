using System.Windows;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

/// <summary>
/// ViewModel cho trang Cài đặt: thông tin tài khoản + đổi mật khẩu.
/// </summary>
public class CaiDatViewModel : BaseViewModel
{
    // Cài đặt hệ thống (lưu vào appsettings.json)
    private string _hotelName = "";
    private string _hotelAddress = "";
    private string _hotelPhone = "";
    private string _hotelEmail = "";
    private string _defaultCheckIn = "14:00";
    private string _defaultCheckOut = "12:00";
    private int _vatPercent = 8;

    // Thông tin tài khoản/nhân viên
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

    public CaiDatViewModel()
    {
        SaveSystemCommand = new RelayCommand(_ => ExecuteSaveSystem());
        ReloadSystemCommand = new RelayCommand(_ => LoadSystemSettings());

        UpdateInfoCommand = new RelayCommand(_ => ExecuteUpdateInfo(), _ => CanUpdate);
        ChangePasswordCommand = new RelayCommand(_ => ExecuteChangePassword(), _ => CanChangePassword);
        CancelCommand = new RelayCommand(_ => ExecuteCancel());

        LoadSystemSettings();
        LoadCurrentUserInfo();
    }

    public string HotelName
    {
        get => _hotelName;
        set => SetProperty(ref _hotelName, value);
    }

    public string HotelAddress
    {
        get => _hotelAddress;
        set => SetProperty(ref _hotelAddress, value);
    }

    public string HotelPhone
    {
        get => _hotelPhone;
        set => SetProperty(ref _hotelPhone, value);
    }

    public string HotelEmail
    {
        get => _hotelEmail;
        set => SetProperty(ref _hotelEmail, value);
    }

    public string DefaultCheckIn
    {
        get => _defaultCheckIn;
        set => SetProperty(ref _defaultCheckIn, value);
    }

    public string DefaultCheckOut
    {
        get => _defaultCheckOut;
        set => SetProperty(ref _defaultCheckOut, value);
    }

    public int VatPercent
    {
        get => _vatPercent;
        set => SetProperty(ref _vatPercent, value);
    }

    public string MaNhanVien
    {
        get => _maNhanVien;
        private set => SetProperty(ref _maNhanVien, value);
    }

    public string TenDangNhap
    {
        get => _tenDangNhap;
        private set => SetProperty(ref _tenDangNhap, value);
    }

    public string Quyen
    {
        get => _quyen;
        private set => SetProperty(ref _quyen, value);
    }

    public string ChucVu
    {
        get => _chucVu;
        private set => SetProperty(ref _chucVu, value);
    }

    public string HoTen
    {
        get => _hoTen;
        set
        {
            if (SetProperty(ref _hoTen, value))
                UpdateInfoCommand.RaiseCanExecuteChanged();
        }
    }

    public string SoDienThoai
    {
        get => _soDienThoai;
        set
        {
            if (SetProperty(ref _soDienThoai, value))
                UpdateInfoCommand.RaiseCanExecuteChanged();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
                UpdateInfoCommand.RaiseCanExecuteChanged();
        }
    }

    public string DiaChi
    {
        get => _diaChi;
        set
        {
            if (SetProperty(ref _diaChi, value))
                UpdateInfoCommand.RaiseCanExecuteChanged();
        }
    }

    public string MatKhauCu
    {
        get => _matKhauCu;
        set
        {
            if (SetProperty(ref _matKhauCu, value))
                ChangePasswordCommand.RaiseCanExecuteChanged();
        }
    }

    public string MatKhauMoi
    {
        get => _matKhauMoi;
        set
        {
            if (SetProperty(ref _matKhauMoi, value))
                ChangePasswordCommand.RaiseCanExecuteChanged();
        }
    }

    public string XacNhanMatKhau
    {
        get => _xacNhanMatKhau;
        set
        {
            if (SetProperty(ref _xacNhanMatKhau, value))
                ChangePasswordCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanUpdate => !string.IsNullOrWhiteSpace(HoTen)
                             && !string.IsNullOrWhiteSpace(SoDienThoai);

    public bool CanChangePassword => !string.IsNullOrWhiteSpace(MatKhauCu)
                                     && !string.IsNullOrWhiteSpace(MatKhauMoi)
                                     && !string.IsNullOrWhiteSpace(XacNhanMatKhau)
                                     && MatKhauMoi.Length >= 6;

    public RelayCommand UpdateInfoCommand { get; }
    public RelayCommand ChangePasswordCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SaveSystemCommand { get; }
    public RelayCommand ReloadSystemCommand { get; }

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
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải cài đặt hệ thống: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteSaveSystem()
    {
        if (string.IsNullOrWhiteSpace(HotelName))
        {
            MessageBox.Show("Vui lòng nhập Tên khách sạn.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (VatPercent < 0 || VatPercent > 30)
        {
            MessageBox.Show("VAT(%) không hợp lệ (0–30).", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SystemSettingsService.Save(new SystemSettings
            {
                HotelName = HotelName.Trim(),
                HotelAddress = (HotelAddress ?? "").Trim(),
                HotelPhone = (HotelPhone ?? "").Trim(),
                HotelEmail = (HotelEmail ?? "").Trim(),
                DefaultCheckIn = (DefaultCheckIn ?? "14:00").Trim(),
                DefaultCheckOut = (DefaultCheckOut ?? "12:00").Trim(),
                VatPercent = VatPercent,
            });

            MessageBox.Show("Đã lưu cài đặt hệ thống.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi lưu cài đặt hệ thống: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCurrentUserInfo()
    {
        string? maNv = App.CurrentUser?.MaNhanVien ?? AppSession.MaNhanVien;
        if (string.IsNullOrWhiteSpace(maNv))
        {
            MessageBox.Show("Bạn chưa đăng nhập hoặc session đã hết.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();

            var nv = db.NhanViens.FirstOrDefault(x => x.MaNhanVien == maNv);
            if (nv == null)
            {
                MessageBox.Show("Không tìm thấy thông tin nhân viên.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Account (TaiKhoan) theo user đang đăng nhập. Nếu TenDangNhap chưa có, fallback theo account active.
            string? tenDn = AppSession.TenDangNhap;
            var tkQuery = db.TaiKhoans.Where(t => t.MaNhanVien == maNv && t.IsActive == true);
            if (!string.IsNullOrWhiteSpace(tenDn))
                tkQuery = tkQuery.Where(t => t.TenDangNhap == tenDn);

            var tk = tkQuery
                .Select(t => new
                {
                    t.TenDangNhap,
                    t.MaQuyen,
                    TenQuyen = t.MaQuyenNavigation != null ? t.MaQuyenNavigation.TenQuyen : null
                })
                .FirstOrDefault();

            MaNhanVien = nv.MaNhanVien;
            HoTen = nv.TenNhanVien ?? "";
            SoDienThoai = nv.DienThoai ?? "";
            Email = nv.Email ?? "";
            DiaChi = nv.DiaChi ?? "";
            ChucVu = nv.ChucVu ?? "";

            TenDangNhap = tk?.TenDangNhap ?? (AppSession.TenDangNhap ?? "");
            Quyen = tk?.TenQuyen ?? tk?.MaQuyen ?? (AppSession.MaQuyen ?? "");

            UpdateInfoCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi tải thông tin: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteUpdateInfo()
    {
        if (!CanUpdate)
        {
            MessageBox.Show("Vui lòng nhập đầy đủ Họ tên và Số điện thoại.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();
            var nv = db.NhanViens.FirstOrDefault(x => x.MaNhanVien == MaNhanVien);
            if (nv == null)
            {
                MessageBox.Show("Không tìm thấy nhân viên.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate trùng SĐT/email (ngoại trừ chính mình)
            if (db.NhanViens.Any(x => x.MaNhanVien != MaNhanVien && x.DienThoai == SoDienThoai))
            {
                MessageBox.Show("Số điện thoại đã được sử dụng bởi nhân viên khác.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(Email) && db.NhanViens.Any(x => x.MaNhanVien != MaNhanVien && x.Email == Email))
            {
                MessageBox.Show("Email đã được sử dụng bởi nhân viên khác.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            nv.TenNhanVien = HoTen;
            nv.DienThoai = SoDienThoai;
            nv.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;
            nv.DiaChi = string.IsNullOrWhiteSpace(DiaChi) ? null : DiaChi;

            db.SaveChanges();

            // Sync session/global user
            AppSession.TenNhanVien = HoTen;
            if (App.CurrentUser != null && App.CurrentUser.MaNhanVien == MaNhanVien)
                App.CurrentUser = App.CurrentUser with { TenNhanVien = HoTen };

            MessageBox.Show("Cập nhật thông tin thành công.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi cập nhật thông tin: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteChangePassword()
    {
        if (!CanChangePassword)
        {
            MessageBox.Show("Vui lòng nhập đủ mật khẩu cũ/mới/xác nhận.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MatKhauMoi.Length < 6)
        {
            MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MatKhauMoi != XacNhanMatKhau)
        {
            MessageBox.Show("Mật khẩu mới và xác nhận không khớp.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new QuanLyKhachSanContext();

            // Ưu tiên đúng tài khoản đang đăng nhập (TenDangNhap), fallback active account.
            string? tenDn = string.IsNullOrWhiteSpace(TenDangNhap) ? AppSession.TenDangNhap : TenDangNhap;
            var tkQuery = db.TaiKhoans.Where(t => t.MaNhanVien == MaNhanVien && t.IsActive == true);
            if (!string.IsNullOrWhiteSpace(tenDn))
                tkQuery = tkQuery.Where(t => t.TenDangNhap == tenDn);

            var account = tkQuery.FirstOrDefault() ?? db.TaiKhoans.FirstOrDefault(t => t.MaNhanVien == MaNhanVien);
            if (account == null)
            {
                MessageBox.Show("Không tìm thấy tài khoản.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var stored = account.MatKhau ?? "";
            bool ok;
            if (!string.IsNullOrEmpty(stored) && stored.StartsWith("HASH2:"))
            {
                ok = PasswordHasher.Verify(MatKhauCu, stored);
            }
            else
            {
                // Hỗ trợ legacy plaintext một lần để nâng cấp sang HASH2
                ok = !string.IsNullOrEmpty(stored) && stored == MatKhauCu;
            }

            if (!ok)
            {
                MessageBox.Show("Mật khẩu cũ không đúng.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            account.MatKhau = PasswordHasher.Hash(MatKhauMoi);
            db.SaveChanges();

            MatKhauCu = "";
            MatKhauMoi = "";
            XacNhanMatKhau = "";

            MessageBox.Show("Đổi mật khẩu thành công.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteCancel()
    {
        MatKhauCu = "";
        MatKhauMoi = "";
        XacNhanMatKhau = "";
        LoadSystemSettings();
        LoadCurrentUserInfo();
    }
}
