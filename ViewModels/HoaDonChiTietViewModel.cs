using QuanLyKhachSan_PhamTanLoi.Commands;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public sealed class HoaDonChiTietViewModel : BaseViewModel
{
    #region CACHE TĨNH (THREAD-SAFE)

    private static class PhuongThucThanhToanCache
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static List<PhuongThucThanhToanDto>? _data;
        private static DateTime _cachedAt = DateTime.MinValue;
        private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

        public static bool IsValid => _data != null &&
                                      (TimeHelper.GetVietnamTime() - _cachedAt) <= _ttl;

        public static async Task<List<PhuongThucThanhToanDto>> GetOrFetchAsync(
            Func<Task<List<PhuongThucThanhToanDto>>> fetchFunc)
        {
            if (IsValid) return _data!;

            await _lock.WaitAsync();
            try
            {
                if (IsValid) return _data!; // double-check sau lock
                _data = await fetchFunc();
                _cachedAt = TimeHelper.GetVietnamTime();
                return _data;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    #endregion

    #region DỊCH VỤ (SERVICES)

    private readonly string _maHoaDon;
    private readonly IHoaDonService _hoaDon;
    private readonly IDichVuService _dichVuSvc;
    private readonly Func<Window?> _layChuSoHuu;
    private readonly Action<bool?> _dong;
    private readonly IHopThoaiService _hopThoai;
    private readonly IInHoaDonService _inHoaDon;
    private readonly IChonDichVuService _chonDichVu;
    private readonly Func<Task>? _taiLaiTrangHoaDonAsync;

    #endregion

    #region FIELDS

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
    private decimal _vatAmount;
    private decimal _tienCoc;
    private decimal _tongThanhToan;
    private decimal _conLai;
    private decimal _tongDaThu;
    private string _khuyenMai = "Không";

    private PhongItemVm? _selectedPhong;
    private string _soTienNhap = "";
    private PhuongThucThanhToanDto? _phuongThucThanhToanDuocChon;
    private string _loaiGiaoDichDuocChon = "Thanh toán cuối";
    private string _noiDung = "";

    // Danh sách tất cả command để RaiseCanExecute duyệt vòng lặp
    private readonly List<ICommand> _tatCaCommands = new();

    #endregion

    #region PROPERTIES

    public string MaHoaDon => _maHoaDon;

    public bool DangXuLy
    {
        get => _dangXuLy;
        private set
        {
            if (SetProperty(ref _dangXuLy, value)) RaiseAllCanExecuteChanged();
        }
    }

    public string MaKhachHang
    {
        get => _maKhachHang;
        private set
        {
            if (SetProperty(ref _maKhachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay));
        }
    }

    public string KhachHang
    {
        get => _khachHang;
        private set
        {
            if (SetProperty(ref _khachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay));
        }
    }

    public string KhachHangDisplay => $"{MaKhachHang} - {KhachHang}";

    public string MaNhanVienDatPhong
    {
        get => _maNhanVienDatPhong;
        set => SetProperty(ref _maNhanVienDatPhong, value);
    }

    public string TenNhanVienDatPhong
    {
        get => _tenNhanVienDatPhong;
        set => SetProperty(ref _tenNhanVienDatPhong, value);
    }

    public string MaNhanVienCheckIn
    {
        get => _maNhanVienCheckIn;
        private set
        {
            if (SetProperty(ref _maNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay));
        }
    }

    public string TenNhanVienCheckIn
    {
        get => _tenNhanVienCheckIn;
        private set
        {
            if (SetProperty(ref _tenNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay));
        }
    }

    public string NhanVienCheckInDisplay => $"{MaNhanVienCheckIn} - {TenNhanVienCheckIn}";
    public string MaNhanVienCheckOut => AppSession.MaNhanVien ?? "Hệ thống";
    public string TenNhanVienCheckOut => AppSession.TenNhanVien ?? "Hệ thống";
    public string NhanVienCheckOutDisplay => $"{MaNhanVienCheckOut} - {TenNhanVienCheckOut}";

    public DateTime? NgayLap
    {
        get => _ngayLap;
        private set => SetProperty(ref _ngayLap, value);
    }

    public string MaDatPhong
    {
        get => _maDatPhong;
        private set => SetProperty(ref _maDatPhong, value);
    }

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

    public bool CoTheChinhSua => TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan || ConLai != 0;

    public bool CoTheTraPhong => TrangThai == HoaDonTrangThaiTexts.DaThanhToan
                                 && TrangThaiDatPhong != DatPhongTrangThaiTexts.DaTraPhong
                                 && !string.IsNullOrWhiteSpace(MaDatPhong);

    public bool CoTheCapNhatTraSom => CoTheChinhSua
                                      && !string.IsNullOrWhiteSpace(MaDatPhong)
                                      && TrangThaiDatPhong != DatPhongTrangThaiTexts.DaTraPhong;

    public decimal TienPhong
    {
        get => _tienPhong;
        private set
        {
            if (SetProperty(ref _tienPhong, value)) OnPropertyChanged(nameof(TienPhongHienThi));
        }
    }

    public decimal TienDichVu
    {
        get => _tienDichVu;
        private set
        {
            if (SetProperty(ref _tienDichVu, value)) OnPropertyChanged(nameof(TienDichVuHienThi));
        }
    }

    public decimal VatPercent
    {
        get => _vatPercent;
        private set
        {
            if (SetProperty(ref _vatPercent, value)) OnPropertyChanged(nameof(VatPhanTramHienThi));
        }
    }

    public decimal VatAmount
    {
        get => _vatAmount;
        private set
        {
            if (SetProperty(ref _vatAmount, value)) OnPropertyChanged(nameof(TienVatHienThi));
        }
    }

    public decimal TienCoc
    {
        get => _tienCoc;
        private set
        {
            if (SetProperty(ref _tienCoc, value)) OnPropertyChanged(nameof(TienCocHienThi));
        }
    }

    public decimal TongThanhToan
    {
        get => _tongThanhToan;
        private set
        {
            if (SetProperty(ref _tongThanhToan, value)) OnPropertyChanged(nameof(TongThanhToanHienThi));
        }
    }

    public decimal TongDaThu
    {
        get => _tongDaThu;
        private set
        {
            if (SetProperty(ref _tongDaThu, value)) OnPropertyChanged(nameof(TongDaThuHienThi));
        }
    }

    public string KhuyenMai
    {
        get => _khuyenMai;
        private set => SetProperty(ref _khuyenMai, value);
    }

    public decimal ConLai
    {
        get => _conLai;
        private set
        {
            if (SetProperty(ref _conLai, value))
            {
                OnPropertyChanged(nameof(ConLaiHienThi));
                OnPropertyChanged(nameof(LabelDuNo));
                OnPropertyChanged(nameof(SoTienDuNoHienThi));
                OnPropertyChanged(nameof(CoTheChinhSua));
            }
        }
    }

    public string LabelDuNo => ConLai > 0 ? "KHÁCH NỢ:" : (ConLai < 0 ? "KHÁCH DƯ:" : "HẾT NỢ:");
    public string SoTienDuNoHienThi => $"{Math.Abs(ConLai):N0} ₫";
    public string ConLaiHienThi => ConLai >= 0 ? $"Còn lại: {ConLai:N0} ₫" : $"Dư: {Math.Abs(ConLai):N0} ₫";

    public string TienPhongHienThi => $"{TienPhong:N0} ₫";
    public string TienDichVuHienThi => $"{TienDichVu:N0} ₫";
    public string VatPhanTramHienThi => $"{VatPercent:N0}%";
    public string TienVatHienThi => $"{VatAmount:N0} ₫";
    public string TienCocHienThi => $"{TienCoc:N0} ₫";
    public string TongThanhToanHienThi => $"{TongThanhToan:N0} ₫";
    public string TongDaThuHienThi => $"{TongDaThu:N0} ₫";

    public ObservableCollection<PhongItemVm> Phongs { get; } = new();
    public ObservableCollection<DichVuItemVm> DichVus { get; } = new();
    public ObservableCollection<ThanhToanItemVm> LichSuThanhToan { get; } = new();
    public ObservableCollection<PhuongThucThanhToanDto> DanhSachPhuongThucThanhToan { get; } = new();

    public ObservableCollection<string> DanhSachLoaiGiaoDich { get; } =
        ["Thanh toán cuối", "Thanh toán trước", "Đặt cọc", "Tiền dịch vụ", "Hoàn tiền"];

    public PhongItemVm? SelectedPhong
    {
        get => _selectedPhong;
        set => SetProperty(ref _selectedPhong, value);
    }

    public PhuongThucThanhToanDto? PhuongThucThanhToanDuocChon
    {
        get => _phuongThucThanhToanDuocChon;
        set => SetProperty(ref _phuongThucThanhToanDuocChon, value);
    }

    public string LoaiGiaoDichDuocChon
    {
        get => _loaiGiaoDichDuocChon;
        set => SetProperty(ref _loaiGiaoDichDuocChon, value);
    }

    public string SoTienNhap
    {
        get => _soTienNhap;
        set => SetProperty(ref _soTienNhap, value);
    }

    public string NoiDung
    {
        get => _noiDung;
        set => SetProperty(ref _noiDung, value);
    }

    public bool CoLichSu => LichSuThanhToan.Count > 0;

    #endregion

    #region CONSTRUCTOR & COMMANDS

    public HoaDonChiTietViewModel(
        string maHoaDon,
        IHoaDonService hoaDonSvc,
        IDichVuService dichVuSvc,
        Func<Window?> layChuSoHuu,
        Action<bool?> dong,
        IHopThoaiService hopThoai,
        IInHoaDonService inHoaDon,
        IChonDichVuService chonDichVu,
        Func<Task>? taiLaiTrangHoaDonAsync = null)
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

        TaiLaiCommand = new AsyncRelayCommand(async _ => await TaiLaiAsync(), _ => !DangXuLy);
        TamInCommand = new AsyncRelayCommand(async _ => await TamInAsync(), _ => !DangXuLy && CoTheChinhSua);
        InHoaDonCommand = new AsyncRelayCommand(async _ => await InHoaDonAsync(), _ => !DangXuLy && !CoTheChinhSua);
        ThemDichVuCommand = new AsyncRelayCommand(async _ => await ThemDichVuAsync(), _ => !DangXuLy && CoTheChinhSua);
        ThanhToanCommand = new AsyncRelayCommand(async _ => await ThanhToanAsync(), _ => !DangXuLy && CoTheChinhSua);
        TraPhongCommand = new AsyncRelayCommand(async _ => await TraPhongAsync(), _ => !DangXuLy && CoTheTraPhong);
        CapNhatTraSomHomNayCommand = new AsyncRelayCommand(async _ => await CapNhatTraSomHomNayAsync(),
            _ => !DangXuLy && CoTheCapNhatTraSom);
        InHoaDonTienPhongCommand =
            new AsyncRelayCommand(async _ => await InHoaDonTienPhongAsync(), _ => !DangXuLy && !CoTheChinhSua);
        InHoaDonDichVuCommand =
            new AsyncRelayCommand(async _ => await InHoaDonDichVuAsync(), _ => !DangXuLy && !CoTheChinhSua);
        DongCommand = new RelayCommand(_ => _dong(false));

        _tatCaCommands.AddRange([
            TaiLaiCommand, TamInCommand, InHoaDonCommand, ThemDichVuCommand,
            ThanhToanCommand, TraPhongCommand, CapNhatTraSomHomNayCommand,
            InHoaDonTienPhongCommand, InHoaDonDichVuCommand, DongCommand
        ]);
    }

    public ICommand TaiLaiCommand { get; }
    public ICommand TamInCommand { get; }
    public ICommand InHoaDonCommand { get; }
    public ICommand ThemDichVuCommand { get; }
    public ICommand ThanhToanCommand { get; }
    public ICommand TraPhongCommand { get; }
    public ICommand CapNhatTraSomHomNayCommand { get; }
    public ICommand DongCommand { get; }
    public ICommand InHoaDonTienPhongCommand { get; }
    public ICommand InHoaDonDichVuCommand { get; }

    #endregion

    #region LOGIC XỬ LÝ

    public enum KieuInHoaDon
    {
        TongHop,
        ChiTienPhong,
        ChiDichVu
    }

    // Master method in — tự quản lý DangXuLy
    private async Task<bool> InHoaDonMasterAsync(bool laTamTinh, KieuInHoaDon kieuIn = KieuInHoaDon.TongHop)
    {
        if (DangXuLy) return false;
        DangXuLy = true;
        try
        {
            return await InHoaDonNoiBoAsync(laTamTinh, kieuIn);
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Lỗi in hóa đơn: {ex.Message}");
            return false;
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // Inner method in — KHÔNG set DangXuLy, dùng khi caller đã giữ lock
    private async Task<bool> InHoaDonNoiBoAsync(bool laTamTinh, KieuInHoaDon kieuIn = KieuInHoaDon.TongHop)
    {
        var hd = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);
        if (hd == null) return false;

        var khName = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
        bool daIn = laTamTinh
            ? _inHoaDon.XemTruocVaInTamTinh(hd, khName, NhanVienCheckOutDisplay, owner: _layChuSoHuu())
            : _inHoaDon.XemTruocVaInHoaDon(hd, khName, NhanVienCheckOutDisplay, kieuIn, owner: _layChuSoHuu());

        if (daIn) await TaiLaiDuLieuNoiBoAsync();
        return daIn;
    }

    private async Task InHoaDonAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
    private async Task TamInAsync() => await InHoaDonMasterAsync(true);
    private async Task InHoaDonTienPhongAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.ChiTienPhong);
    private async Task InHoaDonDichVuAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.ChiDichVu);

    public async Task TaiLaiAsync()
    {
        if (DangXuLy) return;
        DangXuLy = true;
        try
        {
            await TaiLaiDuLieuNoiBoAsync();
        }
        finally
        {
            DangXuLy = false;
        }
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
            MaNhanVienDatPhong = hd.MaDatPhongNavigation?.MaNhanVien ?? "";
            TenNhanVienDatPhong = hd.MaDatPhongNavigation?.MaNhanVienNavigation?.TenNhanVien ?? "";
            NgayLap = hd.NgayLap;
            TrangThai = hd.TrangThai ?? "";
            MaDatPhong = hd.MaDatPhong ?? "";
            TrangThaiDatPhong = hd.MaDatPhongNavigation?.TrangThai ?? "";
            KhuyenMai = hd.MaKhuyenMaiNavigation?.TenKhuyenMai ?? "Không";
            MaNhanVienCheckIn = hd.MaNhanVien ?? "";
            string tenNv = hd.MaNhanVienNavigation?.TenNhanVien ?? "";
            // Bọc lót UX: Nếu Entity Framework quên Include, ta mượn tên từ Session đắp vào
            if (string.IsNullOrWhiteSpace(tenNv) && MaNhanVienCheckIn == AppSession.MaNhanVien)
            {
                tenNv = AppSession.TenNhanVien ?? "Hệ thống";
            }

            TenNhanVienCheckIn = tenNv;


            decimal tienPhongDb = hd.TienPhong ?? 0;
            decimal tienDichVuDb = hd.TienDichVu ?? 0;
            decimal vatPercentDb = hd.Vat ?? 0;
            decimal tienCocDb = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            decimal tongLichSu = hd.ThanhToans?.Sum(t => t.SoTien) ?? 0;

            if (hd.TrangThai == "Chưa thanh toán")
            {
                tienPhongDb = hd.HoaDonChiTiets.Sum(p =>
                    TinhToanService.TinhTienPhongThucTe(p.DatPhongChiTiet.DonGia, p.DatPhongChiTiet.NgayNhan,
                        p.DatPhongChiTiet.NgayTra));

                tienDichVuDb = TinhToanService.TinhTongTienDichVu(hd.DichVuChiTiets);
            }

            var res = TinhToanService.TinhToanToanBo(
                tienPhongDb, tienDichVuDb, vatPercentDb,
                hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0,
                hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "",
                tienCocDb, tongLichSu
            );

            TienPhong = res.TienPhong;
            TienDichVu = res.TienDichVu;
            VatPercent = vatPercentDb;
            VatAmount = res.TienVat;
            TienCoc = tienCocDb;
            TongThanhToan = res.TongThanhToan;
            TongDaThu = tienCocDb + tongLichSu;
            ConLai = res.ConLai;

            // Cập nhật danh sách phòng
            var maPhongDangChon = SelectedPhong?.MaPhong;
            Phongs.Clear();
            foreach (var p in hd.HoaDonChiTiets)
                Phongs.Add(new PhongItemVm
                {
                    MaPhong = p.MaPhong,
                    NgayNhan = p.DatPhongChiTiet.NgayNhan,
                    NgayTra = p.DatPhongChiTiet.NgayTra,
                    SoDem = TinhToanService.TinhSoDem(p.DatPhongChiTiet.NgayNhan, p.DatPhongChiTiet.NgayTra),
                    DonGia = p.DatPhongChiTiet.DonGia
                });

            SelectedPhong = !string.IsNullOrWhiteSpace(maPhongDangChon)
                ? Phongs.FirstOrDefault(x => x.MaPhong == maPhongDangChon) ?? Phongs.FirstOrDefault()
                : Phongs.FirstOrDefault();

            // Cập nhật dịch vụ
            DichVus.Clear();
            foreach (var d in hd.DichVuChiTiets)
                DichVus.Add(new DichVuItemVm
                {
                    MaPhong = d.MaPhong,
                    TenDichVu = d.MaDichVuNavigation.TenDichVu,
                    SoLuong = d.SoLuong,
                    DonGia = d.DonGia
                });

            // Chạy song song: lịch sử thanh toán + phương thức thanh toán
            ApDungLichSuThanhToan(hd.ThanhToans);
            await TaiLaiPhuongThucThanhToanAsync();

            if (!CoTheChinhSua)
            {
                SoTienNhap = "";
                NoiDung = "";
                LoaiGiaoDichDuocChon = "Thanh toán cuối";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            _hopThoai.BaoLoi($"Lỗi tải chi tiết hóa đơn: {ex.Message}");
        }
    }

    // ✅ FIX #4: bỏ async Task không có await → đổi thành void sync
    private void ApDungLichSuThanhToan(IEnumerable<ThanhToan>? tts)
    {
        LichSuThanhToan.Clear();
        if (tts != null)
            foreach (var t in tts.OrderBy(x => x.NgayThanhToan))
                LichSuThanhToan.Add(new ThanhToanItemVm
                {
                    LoaiGiaoDich = t.LoaiGiaoDich ?? "",
                    SoTien = t.SoTien,
                    NgayThanhToan = t.NgayThanhToan,
                    TenNhanVienThuTien = t.NguoiThuNavigation?.TenNhanVien ?? "Hệ thống"
                });

        OnPropertyChanged(nameof(CoLichSu));

        if (CoTheChinhSua)
        {
            SoTienNhap = ConLai != 0 ? Math.Abs(ConLai).ToString("N0") : "0";
            LoaiGiaoDichDuocChon = ConLai < 0 ? "Hoàn tiền" : "Thanh toán cuối";
        }
    }

    // ✅ FIX #2: dùng static cache thread-safe
    private async Task TaiLaiPhuongThucThanhToanAsync()
    {
        var list = await PhuongThucThanhToanCache.GetOrFetchAsync(() => _hoaDon.LayDanhSachPhuongThucThanhToanAsync());
        GanDanhSachPhuongThucThanhToan(list);
    }

    private void GanDanhSachPhuongThucThanhToan(IEnumerable<PhuongThucThanhToanDto> danhSach)
    {
        var maDangChon = PhuongThucThanhToanDuocChon?.MaPTTT;
        DanhSachPhuongThucThanhToan.Clear();
        foreach (var p in danhSach) DanhSachPhuongThucThanhToan.Add(p);

        PhuongThucThanhToanDuocChon = !string.IsNullOrWhiteSpace(maDangChon)
            ? DanhSachPhuongThucThanhToan.FirstOrDefault(x => x.MaPTTT == maDangChon)
              ?? DanhSachPhuongThucThanhToan.FirstOrDefault()
            : PhuongThucThanhToanDuocChon ?? DanhSachPhuongThucThanhToan.FirstOrDefault();
    }

    private async Task ThemDichVuAsync()
    {
        if (TrangThai != HoaDonTrangThaiTexts.ChuaThanhToan)
        {
            _hopThoai.CanhBao(HoaDonMessages.HoaDonDaThanhToan);
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

            var chon = _chonDichVu.ChonDichVu(owner, dichVus, Phongs.Select(p => p.MaPhong).ToList());
            if (!chon.HasValue) return;

            var (maDichVu, soLuong, maPhong) = chon.Value;
            var phong = SelectedPhong?.MaPhong ?? Phongs.FirstOrDefault()?.MaPhong;

            if (string.IsNullOrWhiteSpace(MaDatPhong) || string.IsNullOrWhiteSpace(phong))
            {
                _hopThoai.CanhBao(HoaDonMessages.KhongXacDinhPhong);
                return;
            }

            await _dichVuSvc.UpsertDichVuAsync(_maHoaDon, MaDatPhong, phong, maDichVu, soLuong);
            await TaiLaiAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            _hopThoai.BaoLoi($"Lỗi thêm dịch vụ: {ex.Message}");
        }
    }

    private async Task ThanhToanAsync()
    {
        if (PhuongThucThanhToanDuocChon?.MaPTTT is not string maPttt || string.IsNullOrWhiteSpace(maPttt))
        {
            _hopThoai.CanhBao(HoaDonMessages.ChonPhuongThuc);
            return;
        }

        decimal soTienHienTai = 0;

        // ✅ FIX: Khi ConLai == 0, không cần tạo giao dịch 0đ
        // Chỉ cần đồng bộ trạng thái rồi tải lại
        if (ConLai == 0)
        {
            if (!_hopThoai.XacNhan("Xác nhận chốt hóa đơn (không phát sinh thêm tiền)?", "Xác nhận chốt")) return;
            DangXuLy = true;
            try
            {
                await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);
                _hopThoai.ThongBao(HoaDonMessages.GiaoDichThanhCong);
                await TaiLaiDuLieuNoiBoAsync();
                if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi", ex);
                _hopThoai.BaoLoi($"Lỗi: {ex.Message}");
            }
            finally
            {
                DangXuLy = false;
            }

            return; // ← thoát sớm, không tạo record 0đ
        }

        // ConLai != 0 → bắt buộc nhập số tiền hợp lệ
        if (!ThuParseSoTien(SoTienNhap, out soTienHienTai) || soTienHienTai <= 0)
        {
            _hopThoai.CanhBao(HoaDonMessages.SoTienKhongHopLe);
            return;
        }

        var maNhanVien = AppSession.MaNhanVien ?? "NV001";
        string cauHoi = LoaiGiaoDichDuocChon == "Hoàn tiền"
            ? $"Xác nhận HOÀN TRẢ {soTienHienTai:N0} đ cho khách?"
            : $"Xác nhận thanh toán {soTienHienTai:N0} đ?";

        if (!_hopThoai.XacNhan(cauHoi, "Xác nhận giao dịch")) return;

        DangXuLy = true;
        try
        {
            if (LoaiGiaoDichDuocChon == "Thanh toán cuối" && ConLai > 0 && soTienHienTai < ConLai)
            {
                _hopThoai.CanhBao($"Số tiền không đủ để chốt. Cần: {ConLai:N0} đ");
                return;
            }

            decimal soTienThucTe = LoaiGiaoDichDuocChon == "Hoàn tiền" ? -soTienHienTai : soTienHienTai;
            var thongTin = await _hoaDon.ThanhToanVaTraKetQuaAsync(
                _maHoaDon, soTienThucTe, maPttt, maNhanVien, LoaiGiaoDichDuocChon, NoiDung);
            SoTienNhap = "";

            if (thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat)
            {
                if (LoaiGiaoDichDuocChon is "Thanh toán cuối" or "Hoàn tiền" || ConLai == 0)
                    await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);

                _hopThoai.ThongBao(HoaDonMessages.GiaoDichThanhCong);

                DangXuLy = false;
                await InHoaDonNoiBoAsync(false, KieuInHoaDon.TongHop);

                if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
                await TaiLaiDuLieuNoiBoAsync();
                return;
            }

            _hopThoai.ThongBao(thongTin.ThongDiep);
            await TaiLaiDuLieuNoiBoAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            _hopThoai.BaoLoi($"Lỗi: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private async Task TraPhongAsync()
    {
        if (!CoTheTraPhong) return;
        if (!_hopThoai.XacNhan($"Xác nhận trả phòng cho hóa đơn {_maHoaDon}?", "Xác nhận trả phòng")) return;

        DangXuLy = true;
        try
        {
            var maNhanVien = AppSession.MaNhanVien;
            if (string.IsNullOrWhiteSpace(maNhanVien))
            {
                // ✅ FIX #3: dùng _hopThoai thay vì MessageBox.Show trực tiếp
                _hopThoai.BaoLoi(HoaDonMessages.PhienHetHan);
                return;
            }

            await _hoaDon.TraPhongAsync(_maHoaDon, maNhanVien);
            _hopThoai.ThongBao(HoaDonMessages.TraPhongThanhCong);
            await TaiLaiDuLieuNoiBoAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            _dong(true);
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            _hopThoai.BaoLoi($"Lỗi trả phòng: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    private async Task CapNhatTraSomHomNayAsync()
    {
        if (!CoTheCapNhatTraSom) return;
        if (!_hopThoai.XacNhan("Cập nhật ngày trả phòng = hôm nay và tính lại tiền phòng?", "Cập nhật trả sớm")) return;

        DangXuLy = true;
        try
        {
            var thongTin = await _hoaDon.CapNhatTienPhongKhiTraSomAsync(_maHoaDon, TimeHelper.GetVietnamTime());
            if (thongTin.KetQua == KetQuaThanhToan.TuChoi)
            {
                _hopThoai.CanhBao(thongTin.ThongDiep);
                return;
            }

            _hopThoai.ThongBao(thongTin.ConLai <= 0
                ? "Đã tính lại tổng tiền. Hóa đơn hiện tại đã đủ/đang dư tiền."
                : $"Đã tính lại tổng tiền. Còn lại: {thongTin.ConLai:N0} đ.");

            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            await TaiLaiDuLieuNoiBoAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            _hopThoai.BaoLoi($"Lỗi cập nhật trả sớm: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // ✅ FIX #5: duyệt vòng lặp thay vì cast thủ công từng command
    private void RaiseAllCanExecuteChanged()
    {
        foreach (var cmd in _tatCaCommands)
        {
            (cmd as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (cmd as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private static bool ThuParseSoTien(string? text, out decimal soTien)
    {
        soTien = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var digits = Regex.Replace(text, @"[^\d\-]", "");
        if (digits == "-" || string.IsNullOrWhiteSpace(digits)) return false;
        return decimal.TryParse(digits,
            NumberStyles.Integer | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out soTien);
    }

    #endregion
}

#region CONSTANTS

internal static class HoaDonMessages
{
    public const string PhienHetHan = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
    public const string ChonPhuongThuc = "Vui lòng chọn phương thức thanh toán.";
    public const string SoTienKhongHopLe = "Số tiền giao dịch không hợp lệ.";
    public const string HoaDonDaThanhToan = "Hoá đơn đã thanh toán, không thể thêm dịch vụ.";
    public const string KhongXacDinhPhong = "Không xác định được phòng để thêm dịch vụ.";
    public const string GiaoDichThanhCong = "Giao dịch thành công!";
    public const string TraPhongThanhCong = "Trả phòng thành công!";
}

#endregion

#region HELPER CLASSES

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
    public string MaPhong { get; set; } = "";
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

    public string SoTienText => SoTien < 0
        ? $"- {Math.Abs(SoTien):N0} ₫"
        : $"+ {SoTien:N0} ₫";
}

#endregion