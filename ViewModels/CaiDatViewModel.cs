using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels
{
    /// <summary>
    /// ViewModel cho chức năng Cài đặt (thông tin cá nhân và đổi mật khẩu)
    /// </summary>
    public class CaiDatViewModel : INotifyPropertyChanged
    {
        private readonly QuanLyKhachSanContext _context; // DbContext để truy vấn dữ liệu

        // Thông tin cá nhân
        private string _maNhanVien;
        private string _hoTen;
        private string _soDienThoai;
        private string _email;
        private string _diaChi;

        // Đổi mật khẩu
        private string _matKhauCu;
        private string _matKhauMoi;
        private string _xacNhanMatKhau;

        public CaiDatViewModel()
        {
            _context = new QuanLyKhachSanContext(); // Khởi tạo context

            // Load thông tin user hiện tại từ session
            LoadCurrentUserInfo();

            // Khởi tạo các command (RelayCommand là custom ICommand)
            UpdateInfoCommand = new RelayCommand(ExecuteUpdateInfo, CanExecuteUpdateInfo);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        #region Properties - Thông tin cá nhân

        // Mã nhân viên (chỉ đọc)
        public string MaNhanVien
        {
            get => _maNhanVien;
            set
            {
                _maNhanVien = value;
                OnPropertyChanged(nameof(MaNhanVien));
            }
        }

        // Họ tên nhân viên (có thể sửa)
        public string HoTen
        {
            get => _hoTen;
            set
            {
                _hoTen = value;
                OnPropertyChanged(nameof(HoTen));
                OnPropertyChanged(nameof(CanUpdate)); // Cập nhật trạng thái command
            }
        }

        // Số điện thoại (có thể sửa)
        public string SoDienThoai
        {
            get => _soDienThoai;
            set
            {
                _soDienThoai = value;
                OnPropertyChanged(nameof(SoDienThoai));
                OnPropertyChanged(nameof(CanUpdate));
            }
        }

        // Email (có thể sửa)
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(CanUpdate));
            }
        }

        // Địa chỉ (có thể sửa)
        public string DiaChi
        {
            get => _diaChi;
            set
            {
                _diaChi = value;
                OnPropertyChanged(nameof(DiaChi));
                OnPropertyChanged(nameof(CanUpdate));
            }
        }

        #endregion

        #region Properties - Đổi mật khẩu

        public string MatKhauCu
        {
            get => _matKhauCu;
            set
            {
                _matKhauCu = value;
                OnPropertyChanged(nameof(MatKhauCu));
                OnPropertyChanged(nameof(CanChangePassword));
            }
        }

        public string MatKhauMoi
        {
            get => _matKhauMoi;
            set
            {
                _matKhauMoi = value;
                OnPropertyChanged(nameof(MatKhauMoi));
                OnPropertyChanged(nameof(CanChangePassword));
            }
        }

        public string XacNhanMatKhau
        {
            get => _xacNhanMatKhau;
            set
            {
                _xacNhanMatKhau = value;
                OnPropertyChanged(nameof(XacNhanMatKhau));
                OnPropertyChanged(nameof(CanChangePassword));
            }
        }

        #endregion

        #region Validation Properties

        // Cho phép cập nhật thông tin khi họ tên và số điện thoại không rỗng
        public bool CanUpdate => !string.IsNullOrWhiteSpace(HoTen)
                                 && !string.IsNullOrWhiteSpace(SoDienThoai);

        // Cho phép đổi mật khẩu khi các trường đều có giá trị và mật khẩu mới ≥ 6 ký tự
        public bool CanChangePassword => !string.IsNullOrWhiteSpace(MatKhauCu)
                                         && !string.IsNullOrWhiteSpace(MatKhauMoi)
                                         && !string.IsNullOrWhiteSpace(XacNhanMatKhau)
                                         && MatKhauMoi.Length >= 6;

        #endregion

        #region Commands

        public ICommand UpdateInfoCommand { get; }      // Lệnh cập nhật thông tin cá nhân
        public ICommand ChangePasswordCommand { get; }  // Lệnh đổi mật khẩu
        public ICommand CancelCommand { get; }          // Lệnh hủy (reset form)

        #endregion

        #region Methods

        /// <summary>
        /// Tải thông tin nhân viên hiện tại từ database dựa vào UserSession
        /// </summary>
        private void LoadCurrentUserInfo()
        {
            if (UserSession.CurrentUser == null)
            {
                MessageBox.Show("Không tìm thấy thông tin người dùng đang đăng nhập!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Lấy thông tin mới nhất từ DB (không tracking để tránh conflict)
            var user = _context.NhanViens
                .AsNoTracking()
                .FirstOrDefault(nv => nv.MaNhanVien == UserSession.CurrentUser.MaNhanVien);

            if (user != null)
            {
                MaNhanVien = user.MaNhanVien;
                HoTen = user.TenNhanVien;      // Lưu ý: property trong model là TenNhanVien
                SoDienThoai = user.DienThoai;   // Property trong model là DienThoai
                Email = user.Email;
                DiaChi = user.DiaChi;
            }
        }

        /// <summary>
        /// Kiểm tra xem có thể thực hiện cập nhật thông tin không
        /// </summary>
        private bool CanExecuteUpdateInfo(object parameter) => CanUpdate;

        /// <summary>
        /// Xử lý cập nhật thông tin cá nhân
        /// </summary>
        private void ExecuteUpdateInfo(object parameter)
        {
            try
            {
                // Lấy lại đối tượng nhân viên từ DB (có tracking để sửa)
                var user = _context.NhanViens
                    .FirstOrDefault(nv => nv.MaNhanVien == MaNhanVien);

                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy nhân viên!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validate dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(HoTen))
                {
                    MessageBox.Show("Vui lòng nhập họ tên!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(SoDienThoai))
                {
                    MessageBox.Show("Vui lòng nhập số điện thoại!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kiểm tra số điện thoại trùng (ngoại trừ chính mình)
                var existingPhone = _context.NhanViens
                    .Any(nv => nv.DienThoai == SoDienThoai && nv.MaNhanVien != MaNhanVien);

                if (existingPhone)
                {
                    MessageBox.Show("Số điện thoại đã được sử dụng bởi nhân viên khác!",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kiểm tra email trùng nếu có nhập
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    var existingEmail = _context.NhanViens
                        .Any(nv => nv.Email == Email && nv.MaNhanVien != MaNhanVien);

                    if (existingEmail)
                    {
                        MessageBox.Show("Email đã được sử dụng bởi nhân viên khác!",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Gán giá trị mới cho đối tượng
                user.TenNhanVien = HoTen;
                user.DienThoai = SoDienThoai;
                user.Email = Email;
                user.DiaChi = DiaChi;

                // Lưu thay đổi vào database
                _context.SaveChanges();

                // Cập nhật lại thông tin trong session để các form khác nhận biết
                UserSession.CurrentUser = user;

                MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật thông tin: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Kiểm tra có thể đổi mật khẩu không
        /// </summary>
        private bool CanExecuteChangePassword(object parameter) => CanChangePassword;

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        private void ExecuteChangePassword(object parameter)
        {
            try
            {
                // Kiểm tra độ dài mật khẩu mới
                if (MatKhauMoi.Length < 6)
                {
                    MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kiểm tra xác nhận mật khẩu
                if (MatKhauMoi != XacNhanMatKhau)
                {
                    MessageBox.Show("Mật khẩu mới và xác nhận không khớp!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Lấy thông tin nhân viên (không tracking để tránh lỗi khi lấy tài khoản sau)
                var user = _context.TaiKhoans
                    .FirstOrDefault(nv => nv.MaNhanVien == UserSession.CurrentUser.MaNhanVien);

                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy nhân viên!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Lấy tài khoản tương ứng (có tracking để sửa)
                var account = _context.TaiKhoans
                    .FirstOrDefault(tk => tk.MaNhanVien == MaNhanVien);

                if (account == null)
                {
                    MessageBox.Show("Không tìm thấy tài khoản!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Kiểm tra mật khẩu cũ
                if (account.MatKhau != MatKhauCu)
                {
                    MessageBox.Show("Mật khẩu cũ không đúng!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Cập nhật mật khẩu mới
                account.MatKhau = MatKhauMoi;
                _context.SaveChanges();

                // Xóa trắng các trường mật khẩu trên form
                MatKhauCu = string.Empty;
                MatKhauMoi = string.Empty;
                XacNhanMatKhau = string.Empty;

                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hủy thao tác: tải lại thông tin cá nhân và xóa các trường mật khẩu
        /// </summary>
        private void ExecuteCancel(object parameter)
        {
            LoadCurrentUserInfo(); // Lấy lại thông tin từ DB

            // Xóa trắng các trường mật khẩu
            MatKhauCu = string.Empty;
            MatKhauMoi = string.Empty;
            XacNhanMatKhau = string.Empty;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}