using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.Services.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
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

    // Khách hàng
    private string _maKhachHang = "";
    private string _khachHang = "";

    // Nhân viên
    private string _maNhanVienDatPhong = "";
    private string _tenNhanVienDatPhong = "";
    private string _maNhanVienCheckIn = "";
    private string _tenNhanVienCheckIn = "";

    // Thông tin hóa đơn & phòng
    private DateTime? _ngayLap;
    private string _trangThai = "";
    private string _trangThaiDatPhong = "";
    private string _maDatPhong = "";

    // Tiền bạc
    private decimal _tienPhong;
    private decimal _tienDichVu;
    private decimal _vatPercent;
    private decimal _tienCoc;
    private decimal _tongThanhToan;
    private decimal _conLai;
    private string _khuyenMai = "Không";

    // Thanh toán & Tương tác UI
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

    // --- KHÁCH HÀNG ---
    public string MaKhachHang { get => _maKhachHang; private set { if (SetProperty(ref _maKhachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHang { get => _khachHang; private set { if (SetProperty(ref _khachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHangDisplay => $"{MaKhachHang} - {KhachHang}";

    // --- NHÂN VIÊN ---
    public string MaNhanVienDatPhong { get => _maNhanVienDatPhong; set => SetProperty(ref _maNhanVienDatPhong, value); }
    public string TenNhanVienDatPhong { get => _tenNhanVienDatPhong; set => SetProperty(ref _tenNhanVienDatPhong, value); }

    public string MaNhanVienCheckIn { get => _maNhanVienCheckIn; private set { if (SetProperty(ref _maNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string TenNhanVienCheckIn { get => _tenNhanVienCheckIn; private set { if (SetProperty(ref _tenNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string NhanVienCheckInDisplay => $"{MaNhanVienCheckIn} - {TenNhanVienCheckIn}";

    public string MaNhanVienCheckOut => AppSession.MaNhanVien ?? "He thong";
    public string TenNhanVienCheckOut => AppSession.TenNhanVien ?? "He thong";
    public string NhanVienCheckOutDisplay => $"{MaNhanVienCheckOut} - {TenNhanVienCheckOut}";

    // --- THÔNG TIN HÓA ĐƠN ---
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

    // --- TIỀN BẠC ---
    public decimal TienPhong { get => _tienPhong; private set { if (SetProperty(ref _tienPhong, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal TienDichVu { get => _tienDichVu; private set { if (SetProperty(ref _tienDichVu, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal VatPercent { get => _vatPercent; private set { if (SetProperty(ref _vatPercent, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal VatAmount => TienPhong * (VatPercent / 100m);

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

    // --- GIAO DIỆN & DANH SÁCH ---
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

        TaiLaiCommand = new AsyncRelayCommand(async _ => await TaiLaiAsync(), _ => !DangXuLy);
        TamInCommand = new AsyncRelayCommand(async _ => await TamInAsync(), _ => !DangXuLy && CoTheChinhSua);
        InHoaDonCommand = new AsyncRelayCommand(async _ => await InHoaDonAsync(), _ => !DangXuLy && !CoTheChinhSua);
        ThemDichVuCommand = new AsyncRelayCommand(async _ => await ThemDichVuAsync(), _ => !DangXuLy && CoTheChinhSua);
        ThanhToanCommand = new AsyncRelayCommand(async _ => await ThanhToanAsync(), _ => !DangXuLy && CoTheChinhSua);
        TraPhongCommand = new AsyncRelayCommand(async _ => await TraPhongAsync(), _ => !DangXuLy && CoTheTraPhong);
        CapNhatTraSomHomNayCommand = new AsyncRelayCommand(async _ => await CapNhatTraSomHomNayAsync(), _ => !DangXuLy && CoTheCapNhatTraSom);
        DongCommand = new RelayCommand(_ => _dong(false));
    }

    public ICommand TaiLaiCommand { get; }
    public ICommand TamInCommand { get; }
    public ICommand InHoaDonCommand { get; }
    public ICommand ThemDichVuCommand { get; }
    public ICommand ThanhToanCommand { get; }
    public ICommand TraPhongCommand { get; }
    public ICommand CapNhatTraSomHomNayCommand { get; }
    public ICommand DongCommand { get; }
    #endregion

    #region LOGIC XỬ LÝ
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

            // Tính lại Tổng thanh toán hiển thị cho khách (Đã trừ cọc)
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

// TẢI LẠI PHƯƠNG THỨC THANH TOÁN
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

    // GÁN DANH SÁCH PHƯƠNG THỨC THANH TOÁN VÀ GIỮ LẠI LỰA CHỌN CŨ (NẾU CÒN)
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

    // TẢI LẠI LỊCH SỬ THANH TOÁN
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

    //THÊM DỊCH VỤ
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

    // THANH TOÁN
    private async Task ThanhToanAsync()
    {
        if (PhuongThucThanhToanDuocChon?.MaPTTT is not string maPttt || string.IsNullOrWhiteSpace(maPttt))
        {
            _hopThoai.CanhBao("Vui long chon phuong thuc thanh toan.");
            return;
        }

        decimal soTien = 0;
        if (ConLai > 0)
        {
            if (!ThuParseSoTien(SoTienNhap, out soTien) || soTien <= 0)
            {
                _hopThoai.CanhBao("So tien khong hop le.");
                return;
            }
        }

        var maNhanVien = AppSession.MaNhanVien ?? "NV001";
        if (!_hopThoai.XacNhan($"Xac nhan thanh toan so tien {soTien:N0} d cho hoa don {_maHoaDon}?", "Xac nhan thanh toan")) return;

        DangXuLy = true;
        try
        {
            if (LoaiGiaoDichDuocChon == "Thanh toán cuối")
            {
                if (ConLai > 0 && soTien < ConLai)
                {
                    _hopThoai.CanhBao($"So tien thanh toan chua du de chot hoa don.  {soTien:N0}đ");
                    return;
                }

                if (ConLai <= 0)
                {
                    // Gọi hàm Master là đủ, nó tự load data và tự in
                    bool printed0 = await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
                    if (!printed0) return;

                    await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);
                    _hopThoai.ThongBao("Thanh toan hoan tat! Co the tra phong khi khach roi di.");
                    if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
                    await TaiLaiDuLieuNoiBoAsync();
                    return;
                }

                var hdPrint = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);
                if (hdPrint == null) return;

                var khName = hdPrint.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
                var staffName = NhanVienCheckOutDisplay;
                bool printed = _inHoaDon.XemTruocVaInHoaDon(hdPrint, khName, staffName, owner: _layChuSoHuu());
                if (!printed) return;
            }

            var thongTin = await _hoaDon.ThanhToanVaTraKetQuaAsync(_maHoaDon, soTien, maPttt, maNhanVien, LoaiGiaoDichDuocChon, NoiDung);
            SoTienNhap = "";

            if (thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat)
            {
                _hopThoai.ThongBao("Thanh toan hoan tat! Co the tra phong khi khach roi di.");
                if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
                await TaiLaiDuLieuNoiBoAsync();
                return;
            }

            if (thongTin.KetQua == KetQuaThanhToan.GhiNhanChuaDu)
                _hopThoai.ThongBao($"Da ghi nhan thanh toan {soTien:N0} đ. Khach chua thanh toan du.");
            else
                _hopThoai.CanhBao(thongTin.ThongDiep);

            await TaiLaiDuLieuNoiBoAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi thanh toan: {ex.Message}");
        }
        finally
        {
            DangXuLy = false;
        }
    }

    // TRẢ PHÒNG
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

    // CẬP NHẬT TRẢ SỚM HÔM NAY
    private async Task CapNhatTraSomHomNayAsync()
    {
        if (!CoTheCapNhatTraSom) return;
        if (!_hopThoai.XacNhan("Cap nhat ngay tra phong = hom nay va tinh lai tien phong?", "Cap nhat tra som")) return;

        DangXuLy = true;
        try
        {
            var thongTin = await _hoaDon.CapNhatTienPhongKhiTraSomAsync(_maHoaDon, DateTime.Now);
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


    // Khai báo Enum ở đầu namespace hoặc bên trong class
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
                // NẾU VẪN BÁO LỖI ĐỎ Ở DÒNG DƯỚI: Tạm thời xóa chữ ', kieuIn' đi để Build chạy được.
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

    // Các Command gọi vào hàm Master
    private async Task InHoaDonAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
    private async Task TamInAsync() => await InHoaDonMasterAsync(true);

    private void RaiseAllCanExecuteChanged()
    {
        (TaiLaiCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TamInCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThemDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThanhToanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TraPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CapNhatTraSomHomNayCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (DongCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static bool ThuParseSoTien(string? text, out decimal soTien)
    {
        soTien = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var raw = text.Trim();
        raw = raw.Replace("₫", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("d", "", StringComparison.OrdinalIgnoreCase)
                 .Trim();

        raw = Regex.Replace(raw, @"[^\d\.,\-]", "");

        if (decimal.TryParse(raw, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, VanHoaVietNam, out soTien))
            return true;

        if (decimal.TryParse(raw, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, VanHoaMy, out soTien))
            return true;

        var digits = Regex.Replace(raw, @"[^\d\-]", "");
        if (digits == "-" || digits.Length == 0) return false;

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