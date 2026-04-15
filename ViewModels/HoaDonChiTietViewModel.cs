using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public sealed class HoaDonChiTietViewModel : BaseViewModel
{
    #region TÀI NGUYÊN VÀ DỊCH VỤ (SERVICES)
    private static readonly CultureInfo VanHoaVietNam = new("vi-VN");
    private static readonly CultureInfo VanHoaMy = new("en-US");
    private static readonly TimeSpan ThoiGianHetHanPttt = TimeSpan.FromMinutes(5);
    private static DateTime _thoiDiemTaiPttt = DateTime.MinValue;
    private static List<PhuongThucThanhToanDto>? _cachePhuongThucThanhToan;

    private readonly string _maHoaDon;
    private readonly HoaDonService _hoaDon;
    private readonly DichVuService _dichVuSvc;
    private readonly Func<Window?> _layChuSoHuu;
    private readonly Action<bool?> _dong;
    private readonly IHopThoaiService _hopThoai;
    private readonly IInHoaDonService _inHoaDon;
    private readonly IChonDichVuService _chonDichVu;
    private readonly Func<Task>? _taiLaiTrangHoaDonAsync;
    #endregion

    #region KHAI BÁO BIẾN (FIELDS)
    private bool _dangXuLy;

    private string _maKhachHang = "";
    private string _khachHang = "";

    private string _maNhanVienDatPhong = "";
    private string _tenNhanVienDatPhong = "";
    private string _maNhanVienCheckIn = "";
    private string _tenNhanVienCheckIn = "";

    private DateTime? _ngayLap;
    private string _trangThai = "";
    private string _trangThaiDatPhong = "";
    private string _maDatPhong = "";

    private decimal _tienPhong;
    private decimal _tienDichVu;
    private decimal _vatPercent;
    private decimal _tienCoc;
    private decimal _tongThanhToan;
    private decimal _conLai;
    private string _khuyenMai = "Không";

    private PhongItemVm? _selectedPhong;
    private string _soTienNhap = "";
    private PhuongThucThanhToanDto? _phuongThucThanhToanDuocChon;
    private string _loaiGiaoDichDuocChon = "Thanh toán cuối";
    private string _noiDung = "";
    #endregion

    #region THUỘC TÍNH (PROPERTIES)
    public string MaHoaDon => _maHoaDon;

    public bool DangXuLy
    {
        get => _dangXuLy;
        private set { if (!SetProperty(ref _dangXuLy, value)) return; RaiseAllCanExecuteChanged(); }
    }

    public string MaKhachHang { get => _maKhachHang; private set { if (SetProperty(ref _maKhachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHang { get => _khachHang; private set { if (SetProperty(ref _khachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHangDisplay => $"{MaKhachHang} - {KhachHang}";

    public string MaNhanVienDatPhong { get => _maNhanVienDatPhong; set => SetProperty(ref _maNhanVienDatPhong, value); }
    public string TenNhanVienDatPhong { get => _tenNhanVienDatPhong; set => SetProperty(ref _tenNhanVienDatPhong, value); }

    public string MaNhanVienCheckIn { get => _maNhanVienCheckIn; private set { if (SetProperty(ref _maNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string TenNhanVienCheckIn { get => _tenNhanVienCheckIn; private set { if (SetProperty(ref _tenNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string NhanVienCheckInDisplay => $"{MaNhanVienCheckIn} - {TenNhanVienCheckIn}";

    public string MaNhanVienCheckOut => AppSession.MaNhanVien ?? "He thong";
    public string TenNhanVienCheckOut => AppSession.TenNhanVien ?? "He thong";
    public string NhanVienCheckOutDisplay => $"{MaNhanVienCheckOut} - {TenNhanVienCheckOut}";

    public DateTime? NgayLap { get => _ngayLap; private set => SetProperty(ref _ngayLap, value); }
    public string MaDatPhong { get => _maDatPhong; private set => SetProperty(ref _maDatPhong, value); }

    public string TrangThai
    {
        get => _trangThai;
        private set
        {
            if (!SetProperty(ref _trangThai, value)) return;
            OnPropertyChanged(nameof(CoTheChinhSua));
            OnPropertyChanged(nameof(CoTheTraPhong));
            OnPropertyChanged(nameof(CoTheCapNhatTraSom));
            RaiseAllCanExecuteChanged();
        }
    }

    public string TrangThaiDatPhong
    {
        get => _trangThaiDatPhong;
        private set
        {
            if (!SetProperty(ref _trangThaiDatPhong, value)) return;
            OnPropertyChanged(nameof(CoTheTraPhong));
            OnPropertyChanged(nameof(CoTheCapNhatTraSom));
            RaiseAllCanExecuteChanged();
        }
    }

    public bool CoTheChinhSua => TrangThai == "Chưa thanh toán";
    public bool CoTheTraPhong => TrangThai == "Đã thanh toán" && TrangThaiDatPhong != "Đã trả phòng" && !string.IsNullOrWhiteSpace(MaDatPhong);
    public bool CoTheCapNhatTraSom => CoTheChinhSua && !string.IsNullOrWhiteSpace(MaDatPhong) && TrangThaiDatPhong != "Đã trả phòng";

    // --- TIỀN BẠC (ĐÃ SỬA LOGIC VAT) ---
    public decimal TienPhong { get => _tienPhong; private set { if (SetProperty(ref _tienPhong, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal TienDichVu { get => _tienDichVu; private set { if (SetProperty(ref _tienDichVu, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal VatPercent { get => _vatPercent; private set { if (SetProperty(ref _vatPercent, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal VatAmount => (TienPhong + TienDichVu) * (VatPercent / 100m); // Chỉ tính VAT cho tiền phòng


    public decimal TienCoc { get => _tienCoc; private set { if (SetProperty(ref _tienCoc, value)) OnPropertyChanged(nameof(TienCocHienThi)); } }
    public decimal TongThanhToan { get => _tongThanhToan; private set => SetProperty(ref _tongThanhToan, value); }
    public string KhuyenMai { get => _khuyenMai; private set => SetProperty(ref _khuyenMai, value); }

    public string TienPhongHienThi => $"{TienPhong:N0} ₫";
    public string TienDichVuHienThi => $"{TienDichVu:N0} ₫";
    public string VatPhanTramHienThi => $"{VatPercent:N0}%";
    public string TienVatHienThi => $"{VatAmount:N0} ₫";
    public string TienCocHienThi => $"- {TienCoc:N0} ₫";
    public string TongThanhToanHienThi => $"{TongThanhToan:N0} ₫";

    public decimal ConLai
    {
        get => _conLai;
        private set { if (!SetProperty(ref _conLai, value)) return; OnPropertyChanged(nameof(ConLaiHienThi)); }
    }
    public string ConLaiHienThi => ConLai >= 0 ? $"Còn lại: {ConLai:N0} ₫" : $"Dư: {Math.Abs(ConLai):N0} ₫";

    public ObservableCollection<PhongItemVm> Phongs { get; } = new();
    public ObservableCollection<DichVuItemVm> DichVus { get; } = new();
    public ObservableCollection<ThanhToanItemVm> LichSuThanhToan { get; } = new();
    public ObservableCollection<PhuongThucThanhToanDto> DanhSachPhuongThucThanhToan { get; } = new();
    public ObservableCollection<string> DanhSachLoaiGiaoDich { get; } = ["Thanh toán cuối", "Thanh toán trước", "Đặt cọc", "Hoàn tiền"];

    public PhongItemVm? SelectedPhong { get => _selectedPhong; set => SetProperty(ref _selectedPhong, value); }
    public PhuongThucThanhToanDto? PhuongThucThanhToanDuocChon { get => _phuongThucThanhToanDuocChon; set => SetProperty(ref _phuongThucThanhToanDuocChon, value); }
    public string LoaiGiaoDichDuocChon { get => _loaiGiaoDichDuocChon; set => SetProperty(ref _loaiGiaoDichDuocChon, value); }
    public string SoTienNhap { get => _soTienNhap; set => SetProperty(ref _soTienNhap, value); }
    public string NoiDung { get => _noiDung; set => SetProperty(ref _noiDung, value); }
    public bool CoLichSu => LichSuThanhToan.Count > 0;
    #endregion

    #region CONSTRUCTOR & COMMANDS
    public HoaDonChiTietViewModel(
        string maHoaDon, HoaDonService hoaDonSvc, DichVuService dichVuSvc, Func<Window?> layChuSoHuu,
        Action<bool?> dong, IHopThoaiService hopThoai, IInHoaDonService inHoaDon,
        IChonDichVuService chonDichVu, Func<Task>? taiLaiTrangHoaDonAsync = null)
    {
        _maHoaDon = maHoaDon;
        _hoaDon = hoaDonSvc;
        _dichVuSvc = dichVuSvc;
        _layChuSoHuu = layChuSoHuu;
        _dong = dong;
        _hopThoai = hopThoai;
        _inHoaDon = inHoaDon;
        _chonDichVu = chonDichVu;
        _taiLaiTrangHoaDonAsync = taiLaiTrangHoaDonAsync;

        #region  CONSTRUCTOR
        TaiLaiCommand = new AsyncRelayCommand(async _ => await TaiLaiAsync(), _ => !DangXuLy);
        TamInCommand = new AsyncRelayCommand(async _ => await TamInAsync(), _ => !DangXuLy && CoTheChinhSua);
        InHoaDonCommand = new AsyncRelayCommand(async _ => await InHoaDonAsync(), _ => !DangXuLy && !CoTheChinhSua);
        ThemDichVuCommand = new AsyncRelayCommand(async _ => await ThemDichVuAsync(), _ => !DangXuLy && CoTheChinhSua);
        ThanhToanCommand = new AsyncRelayCommand(async _ => await ThanhToanAsync(), _ => !DangXuLy && CoTheChinhSua);
        TraPhongCommand = new AsyncRelayCommand(async _ => await TraPhongAsync(), _ => !DangXuLy && CoTheTraPhong);
        CapNhatTraSomHomNayCommand = new AsyncRelayCommand(async _ => await CapNhatTraSomHomNayAsync(), _ => !DangXuLy && CoTheCapNhatTraSom);
       
        InHoaDonTienPhongCommand = new AsyncRelayCommand(async _ => await InHoaDonTienPhongAsync(),_ => !DangXuLy && !CoTheChinhSua);  // Khởi tạo lệnh in tiền phòng
        InHoaDonDichVuCommand = new AsyncRelayCommand(async _ => await InHoaDonDichVuAsync(), _ => !DangXuLy && !CoTheChinhSua);  // Khởi tạo lệnh in dịch vụ
        DongCommand = new RelayCommand(_ => _dong(false));
        #endregion
    }

        #region COMMANDS
        public ICommand TaiLaiCommand { get; }
        public ICommand TamInCommand { get; }
        public ICommand InHoaDonCommand { get; }
        public ICommand ThemDichVuCommand { get; }
        public ICommand ThanhToanCommand { get; }
        public ICommand TraPhongCommand { get; }
        public ICommand CapNhatTraSomHomNayCommand { get; }
        public ICommand DongCommand { get; }
        public ICommand InHoaDonTienPhongCommand { get; } // Lệnh in chỉ tiền phòng
        public ICommand InHoaDonDichVuCommand { get; }    // Lệnh in chỉ dịch vụ
        #endregion
    #endregion

    #region LOGIC XỬ LÝ
    public enum KieuInHoaDon { TongHop, ChiTienPhong, ChiDichVu }

    // HÀM IN GỘP CHUNG (MASTER PRINT)
    private async Task<bool> InHoaDonMasterAsync(bool laTamTinh, KieuInHoaDon kieuIn = KieuInHoaDon.TongHop)
    {
        if (DangXuLy) return false;
        DangXuLy = true;
        try
        {
            var hd = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);
            if (hd == null) return false;

            var khName = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
            var staffName = NhanVienCheckOutDisplay;

            bool daIn;
            if (laTamTinh)
            {
                daIn = _inHoaDon.XemTruocVaInTamTinh(hd, khName, staffName, owner: _layChuSoHuu());
            }
            else
            {
                daIn = _inHoaDon.XemTruocVaInHoaDon(hd, khName, staffName, kieuIn, owner: _layChuSoHuu());
            }

            if (daIn) await TaiLaiDuLieuNoiBoAsync();
            return daIn;
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi in hoa don: {ex.Message}");
            return false;
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // Các lệnh gọi in từ nút bấm (UI)
    private async Task InHoaDonAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
    private async Task TamInAsync() => await InHoaDonMasterAsync(true);
    private async Task InHoaDonTienPhongAsync()=> await InHoaDonMasterAsync(false, KieuInHoaDon.ChiTienPhong);
    private async Task InHoaDonDichVuAsync()=> await InHoaDonMasterAsync(false, KieuInHoaDon.ChiDichVu);

    public async Task TaiLaiAsync()
    {
        if (DangXuLy) return;
        DangXuLy = true;
        try { await TaiLaiDuLieuNoiBoAsync(); }
        finally { DangXuLy = false; }
    }

    private async Task TaiLaiDuLieuNoiBoAsync()
    {
        try
        {
            await _hoaDon.DamBaoHoaDonChiTietAsync(_maHoaDon);
            var hd = await _hoaDon.LayHoaDonChiTietAsync(_maHoaDon);
            if (hd == null) return;

            MaKhachHang = hd.MaDatPhongNavigation?.MaKhachHang ?? "";
            KhachHang = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";

            MaNhanVienCheckIn = hd.MaNhanVien ?? "";
            TenNhanVienCheckIn = hd.MaNhanVienNavigation?.TenNhanVien ?? "";

            MaNhanVienDatPhong = hd.MaDatPhongNavigation?.MaNhanVien ?? "";
            TenNhanVienDatPhong = hd.MaDatPhongNavigation?.MaNhanVienNavigation?.TenNhanVien ?? "";

            NgayLap = hd.NgayLap;
            TrangThai = hd.TrangThai ?? "";
            MaDatPhong = hd.MaDatPhong ?? "";
            TrangThaiDatPhong = hd.MaDatPhongNavigation?.TrangThai ?? "";

            TienPhong = hd.TienPhong ?? 0;
            TienDichVu = hd.TienDichVu ?? 0;
            VatPercent = hd.Vat ?? 0;
            TienCoc = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            KhuyenMai = hd.MaKhuyenMaiNavigation?.TenKhuyenMai ?? "Không";

            TongThanhToan = (TienPhong + TienDichVu + VatAmount) - TienCoc;

            var maPhongDangChon = SelectedPhong?.MaPhong;
            Phongs.Clear();
            foreach (var p in hd.HoaDonChiTiets.Select(p => new PhongItemVm
            {
                MaPhong = p.MaPhong,
                NgayNhan = p.DatPhongChiTiet.NgayNhan,
                NgayTra = p.DatPhongChiTiet.NgayTra,
                SoDem = p.SoDem,
                DonGia = p.DatPhongChiTiet.DonGia
            }))
                Phongs.Add(p);

            SelectedPhong = !string.IsNullOrWhiteSpace(maPhongDangChon)
                ? Phongs.FirstOrDefault(x => x.MaPhong == maPhongDangChon) ?? Phongs.FirstOrDefault()
                : Phongs.FirstOrDefault();

            DichVus.Clear();
            foreach (var d in hd.DichVuChiTiets.Select(d => new DichVuItemVm
            {
                TenDichVu = d.MaDichVuNavigation.TenDichVu,
                SoLuong = d.SoLuong,
                DonGia = d.DonGia
            }))
                DichVus.Add(d);

            OnPropertyChanged(nameof(TienPhongHienThi));
            OnPropertyChanged(nameof(TienDichVuHienThi));
            OnPropertyChanged(nameof(VatPhanTramHienThi));
            OnPropertyChanged(nameof(TongThanhToanHienThi));

            await Task.WhenAll(TaiLaiLichSuThanhToanAsync(), TaiLaiPhuongThucThanhToanAsync());

            if (!CoTheChinhSua)
            {
                SoTienNhap = "";
                NoiDung = "";
                LoaiGiaoDichDuocChon = "Thanh toán cuối";
            }
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi tai chi tiet hoa don: {ex.Message}");
        }
    }

    private async Task TaiLaiPhuongThucThanhToanAsync()
    {
        if (_cachePhuongThucThanhToan != null && (DateTime.Now - _thoiDiemTaiPttt) <= ThoiGianHetHanPttt)
        {
            GanDanhSachPhuongThucThanhToan(_cachePhuongThucThanhToan);
            return;
        }

        using var db = new Data.QuanLyKhachSanContext();
        var svc = new HoaDonService(db, new KhachHangService(db));
        var list = await svc.LayDanhSachPhuongThucThanhToanAsync();
        _cachePhuongThucThanhToan = list;
        _thoiDiemTaiPttt = DateTime.Now;

        GanDanhSachPhuongThucThanhToan(list);
    }

    private void GanDanhSachPhuongThucThanhToan(IEnumerable<PhuongThucThanhToanDto> danhSach)
    {
        var maDangChon = PhuongThucThanhToanDuocChon?.MaPTTT;
        DanhSachPhuongThucThanhToan.Clear();
        foreach (var p in danhSach) DanhSachPhuongThucThanhToan.Add(p);

        if (!string.IsNullOrWhiteSpace(maDangChon))
            PhuongThucThanhToanDuocChon = DanhSachPhuongThucThanhToan.FirstOrDefault(x => x.MaPTTT == maDangChon) ?? DanhSachPhuongThucThanhToan.FirstOrDefault();
        else
            PhuongThucThanhToanDuocChon ??= DanhSachPhuongThucThanhToan.FirstOrDefault();
    }

    private async Task TaiLaiLichSuThanhToanAsync()
    {
        using var db = new Data.QuanLyKhachSanContext();
        var svc = new HoaDonService(db, new KhachHangService(db));
        var tts = await svc.LayLichSuThanhToanAsync(_maHoaDon);
        var items = tts.Select(t => new ThanhToanItemVm
        {
            LoaiGiaoDich = t.LoaiGiaoDich ?? "",
            SoTien = t.SoTien,
            NgayThanhToan = t.NgayThanhToan,
            MaNhanVienThuTien = t.NguoiThu ?? "",
            TenNhanVienThuTien = t.NguoiThuNavigation?.TenNhanVien ?? "Hệ thống"
        }).ToList();

        LichSuThanhToan.Clear();
        foreach (var t in items) LichSuThanhToan.Add(t);

        OnPropertyChanged(nameof(CoLichSu));
        decimal tongDaThu = items.Sum(t => t.SoTien);
        ConLai = TongThanhToan - tongDaThu;

        if (CoTheChinhSua && string.IsNullOrWhiteSpace(SoTienNhap))
            SoTienNhap = ConLai > 0 ? $"{ConLai:N0}" : "";
    }

    private async Task ThemDichVuAsync()
    {
        if (TrangThai != "Chưa thanh toán")
        {
            _hopThoai.CanhBao("Hoá đơn đã thanh toán, không thể thêm dịch vụ.");
            return;
        }

        var owner = _layChuSoHuu();
        try
        {
            var dvs = await _dichVuSvc.GetAllDichVuAsync();
            var dichVus = dvs.Select(d => new DichVuViewModel
            {
                MaDichVu = d.MaDichVu,
                TenDichVu = d.TenDichVu,
                Gia = d.Gia ?? 0,
                IsActive = d.IsActive ?? false
            }).ToList();

            var chon = _chonDichVu.ChonDichVu(owner, dichVus);
            if (!chon.HasValue) return;
            var (maDichVu, soLuong) = chon.Value;

            var phong = SelectedPhong?.MaPhong ?? Phongs.FirstOrDefault()?.MaPhong;
            if (string.IsNullOrWhiteSpace(MaDatPhong) || string.IsNullOrWhiteSpace(phong))
            {
                _hopThoai.CanhBao("Khong xac dinh duoc phong de them dich vu.");
                return;
            }

            await _dichVuSvc.UpsertDichVuAsync(_maHoaDon, MaDatPhong, phong, maDichVu, soLuong);
            await TaiLaiAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi them dich vu: {ex.Message}");
        }
    }

    private async Task ThanhToanAsync()
    {
        // 1. Validate đầu vào
        if (PhuongThucThanhToanDuocChon?.MaPTTT is not string maPttt || string.IsNullOrWhiteSpace(maPttt))
        {
            _hopThoai.CanhBao("Vui lòng chọn phương thức thanh toán.");
            return;
        }

        decimal soTien = 0;

        // Chỉ yêu cầu nhập và validate số tiền nếu khách CÒN NỢ
        if (ConLai > 0)
        {
            if (!ThuParseSoTien(SoTienNhap, out soTien) || soTien <= 0)
            {
                _hopThoai.CanhBao("Số tiền không hợp lệ.");
                return;
            }
        }
        else
        {
            // Nếu đã đủ tiền hoặc dư tiền cọc, mặc định số tiền giao dịch phát sinh là 0
            soTien = 0;
        }

        var maNhanVien = AppSession.MaNhanVien ?? "NV001";

        // Thay đổi câu hỏi xác nhận cho phù hợp với ngữ cảnh
        string cauHoiXacNhan = soTien > 0
            ? $"Xác nhận thanh toán số tiền {soTien:N0} đ cho hóa đơn {_maHoaDon}?"
            : $"Hóa đơn {_maHoaDon} đã đủ tiền. Xác nhận chốt hóa đơn và in?";

        if (!_hopThoai.XacNhan(cauHoiXacNhan, "Xác nhận thanh toán")) return;

        DangXuLy = true;
        try
        {
            // 2. Kiểm tra logic Thanh toán cuối
            if (LoaiGiaoDichDuocChon == "Thanh toán cuối" && ConLai > 0 && soTien < ConLai)
            {
                _hopThoai.CanhBao($"Số tiền thanh toán chưa đủ để chốt hóa đơn. {soTien:N0}đ");
                return;
            }

            // 3. XỬ LÝ DATA: Lưu thông tin thanh toán vào Database
            var thongTin = await _hoaDon.ThanhToanVaTraKetQuaAsync(_maHoaDon, soTien, maPttt, maNhanVien, LoaiGiaoDichDuocChon, NoiDung);
            SoTienNhap = "";

            // 4. XỬ LÝ UI: Hiển thị kết quả & In hóa đơn
            if (thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat)
            {
                if (LoaiGiaoDichDuocChon == "Thanh toán cuối" || ConLai <= 0)
                {
                    await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);
                }

                _hopThoai.ThongBao("Chốt hóa đơn hoàn tất! Hệ thống sẽ hiển thị bản in.");

                // Gọi Print: Tạm tắt cờ DangXuLy để hàm InHoaDonMasterAsync có thể chạy
                DangXuLy = false;
                await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
                DangXuLy = true; // Bật lại để block finally phía dưới xử lý an toàn

                if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
                await TaiLaiDuLieuNoiBoAsync();
                return;
            }

            // Các trường hợp trả thiếu tiền hoặc lỗi logic khác
            if (thongTin.KetQua == KetQuaThanhToan.GhiNhanChuaDu)
                _hopThoai.ThongBao($"Đã ghi nhận thanh toán {soTien:N0} đ. Khách chưa thanh toán đủ.");
            else
                _hopThoai.CanhBao(thongTin.ThongDiep);

            await TaiLaiDuLieuNoiBoAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Lỗi thanh toán: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private async Task TraPhongAsync()
    {
        if (!CoTheTraPhong) return;
        if (!_hopThoai.XacNhan($"Xac nhan tra phong cho hoa don {_maHoaDon}?", "Xac nhan tra phong")) return;

        DangXuLy = true;
        try
        {
            var maNhanVien = AppSession.MaNhanVien ?? "NV001";
            await _hoaDon.TraPhongAsync(_maHoaDon, maNhanVien);
            _hopThoai.ThongBao("Tra phong thanh cong! Phong da chuyen sang trang thai can don dep.");
            await TaiLaiDuLieuNoiBoAsync();

            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            _dong(true);
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi tra phong: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private async Task CapNhatTraSomHomNayAsync()
    {
        if (!CoTheCapNhatTraSom) return;
        if (!_hopThoai.XacNhan("Cap nhat ngay tra phong = hom nay va tinh lai tien phong?", "Cap nhat tra som")) return;

        DangXuLy = true;
        try
        {
            var thongTin = await _hoaDon.CapNhatTienPhongKhiTraSomAsync(_maHoaDon, TimeHelper.GetVietnamTime());
            if (thongTin.KetQua == KetQuaThanhToan.TuChoi)
            {
                _hopThoai.CanhBao(thongTin.ThongDiep);
                return;
            }

            if (thongTin.ConLai <= 0)
                _hopThoai.ThongBao("Da tinh lai tong tien. Hoa don hien tai da du/dang du tien.");
            else
                _hopThoai.ThongBao($"Da tinh lai tong tien. Con lai: {thongTin.ConLai:N0} đ.");

            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            await TaiLaiDuLieuNoiBoAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi cap nhat tra som: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // / Hàm tiện ích để gọi RaiseCanExecuteChanged cho tất cả các lệnh liên quan khi trạng thái có thể thay đổi
    private void RaiseAllCanExecuteChanged()
    {
        (TaiLaiCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TamInCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThemDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThanhToanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TraPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CapNhatTraSomHomNayCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonTienPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (DongCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

   private static bool ThuParseSoTien(string? text, out decimal soTien)
{
    soTien = 0;
    if (string.IsNullOrWhiteSpace(text)) return false;

    // BƯỚC 1: Dùng Regex gọt sạch MỌI THỨ (chữ đ, dấu phẩy, khoảng trắng, dấu chấm...)
    // Chỉ giữ lại duy nhất các con số từ 0-9 và dấu trừ (-)
    var digits = Regex.Replace(text, @"[^\d\-]", "");

    // BƯỚC 2: Kiểm tra an toàn
    if (digits == "-" || string.IsNullOrWhiteSpace(digits)) return false;

    // BƯỚC 3: Ép kiểu chuỗi số nguyên thuần túy
    return decimal.TryParse(digits, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out soTien);
}
    #endregion
}

#region LỚP PHỤ TRỢ (HELPER CLASSES)
public sealed class PhongItemVm
{
    public string MaPhong { get; set; } = "";
    public DateTime NgayNhan { get; set; }
    public DateTime NgayTra { get; set; }
    public int SoDem { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoDem * DonGia;
    public string DonGiaText => $"{DonGia:N0} ₫";
    public string ThanhTienText => $"{ThanhTien:N0} ₫";
}

public sealed class DichVuItemVm
{
    public string TenDichVu { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => DonGia * SoLuong;
    public string DonGiaText => $"{DonGia:N0} ₫";
    public string ThanhTienText => $"{ThanhTien:N0} ₫";
}

public sealed class ThanhToanItemVm
{
    public string LoaiGiaoDich { get; set; } = "";
    public decimal SoTien { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string MaNhanVienThuTien { get; set; } = ""; 
    public string TenNhanVienThuTien { get; set; } = "";
    public string SoTienText => $"{SoTien:N0} ₫";
}
#endregion