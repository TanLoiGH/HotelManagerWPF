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
    #region TÀI NGUYÊN VÀ DỊCH VỤ (SERVICES)
    private static readonly TimeSpan ThoiGianHetHanPttt = TimeSpan.FromMinutes(5);
    private static DateTime _thoiDiemTaiPttt = DateTime.MinValue;
    private static List<PhuongThucThanhToanDto>? _cachePhuongThucThanhToan;

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

    // TIỀN BẠC
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
    #endregion

    #region THUỘC TÍNH (PROPERTIES)
    public string MaHoaDon => _maHoaDon;
    public bool DangXuLy { get => _dangXuLy; private set { if (SetProperty(ref _dangXuLy, value)) RaiseAllCanExecuteChanged(); } }

    public string MaKhachHang { get => _maKhachHang; private set { if (SetProperty(ref _maKhachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHang { get => _khachHang; private set { if (SetProperty(ref _khachHang, value)) OnPropertyChanged(nameof(KhachHangDisplay)); } }
    public string KhachHangDisplay => $"{MaKhachHang} - {KhachHang}";

    public string MaNhanVienDatPhong { get => _maNhanVienDatPhong; set => SetProperty(ref _maNhanVienDatPhong, value); }
    public string TenNhanVienDatPhong { get => _tenNhanVienDatPhong; set => SetProperty(ref _tenNhanVienDatPhong, value); }

    public string MaNhanVienCheckIn { get => _maNhanVienCheckIn; private set { if (SetProperty(ref _maNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string TenNhanVienCheckIn { get => _tenNhanVienCheckIn; private set { if (SetProperty(ref _tenNhanVienCheckIn, value)) OnPropertyChanged(nameof(NhanVienCheckInDisplay)); } }
    public string NhanVienCheckInDisplay => $"{MaNhanVienCheckIn} - {TenNhanVienCheckIn}";

    public string MaNhanVienCheckOut => AppSession.MaNhanVien ?? "Hệ thống";
    public string TenNhanVienCheckOut => AppSession.TenNhanVien ?? "Hệ thống";
    public string NhanVienCheckOutDisplay => $"{MaNhanVienCheckOut} - {TenNhanVienCheckOut}";

    public DateTime? NgayLap { get => _ngayLap; private set => SetProperty(ref _ngayLap, value); }
    public string MaDatPhong { get => _maDatPhong; private set => SetProperty(ref _maDatPhong, value); }

    public string TrangThai
    {
        get => _trangThai;
        private set { if (!SetProperty(ref _trangThai, value)) return; OnPropertyChanged(nameof(CoTheChinhSua)); OnPropertyChanged(nameof(CoTheTraPhong)); OnPropertyChanged(nameof(CoTheCapNhatTraSom)); RaiseAllCanExecuteChanged(); }
    }

    public string TrangThaiDatPhong
    {
        get => _trangThaiDatPhong;
        private set { if (!SetProperty(ref _trangThaiDatPhong, value)) return; OnPropertyChanged(nameof(CoTheTraPhong)); OnPropertyChanged(nameof(CoTheCapNhatTraSom)); RaiseAllCanExecuteChanged(); }
    }

    public bool CoTheChinhSua => TrangThai == HoaDonTrangThaiTexts.ChuaThanhToan || ConLai != 0;
    public bool CoTheTraPhong => TrangThai == HoaDonTrangThaiTexts.DaThanhToan && TrangThaiDatPhong != DatPhongTrangThaiTexts.DaTraPhong && !string.IsNullOrWhiteSpace(MaDatPhong);
    public bool CoTheCapNhatTraSom => CoTheChinhSua && !string.IsNullOrWhiteSpace(MaDatPhong) && TrangThaiDatPhong != DatPhongTrangThaiTexts.DaTraPhong;

    // --- TIỀN BẠC (KHÔNG TỰ TÍNH, CHỜ TRẠM ĐIỀU KHIỂN GÁN VÀO) ---
    public decimal TienPhong { get => _tienPhong; private set { if (SetProperty(ref _tienPhong, value)) OnPropertyChanged(nameof(TienPhongHienThi)); } }
    public decimal TienDichVu { get => _tienDichVu; private set { if (SetProperty(ref _tienDichVu, value)) OnPropertyChanged(nameof(TienDichVuHienThi)); } }
    public decimal VatPercent { get => _vatPercent; private set { if (SetProperty(ref _vatPercent, value)) OnPropertyChanged(nameof(VatPhanTramHienThi)); } }
    public decimal VatAmount { get => _vatAmount; private set { if (SetProperty(ref _vatAmount, value)) OnPropertyChanged(nameof(TienVatHienThi)); } }
    public decimal TienCoc { get => _tienCoc; private set { if (SetProperty(ref _tienCoc, value)) OnPropertyChanged(nameof(TienCocHienThi)); } }
    public decimal TongThanhToan { get => _tongThanhToan; private set { if (SetProperty(ref _tongThanhToan, value)) OnPropertyChanged(nameof(TongThanhToanHienThi)); } }
    public decimal TongDaThu { get => _tongDaThu; private set { if (SetProperty(ref _tongDaThu, value)) OnPropertyChanged(nameof(TongDaThuHienThi)); } }
    public string KhuyenMai { get => _khuyenMai; private set => SetProperty(ref _khuyenMai, value); }

    // 🔥 FIX LỖI "KHÁCH DƯ 0đ": Bổ sung trigger thông báo cho UI cập nhật
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
    public ObservableCollection<string> DanhSachLoaiGiaoDich { get; } = ["Thanh toán cuối", "Thanh toán trước", "Đặt cọc", "Tiền dịch vụ", "Hoàn tiền"];

    public PhongItemVm? SelectedPhong { get => _selectedPhong; set => SetProperty(ref _selectedPhong, value); }
    public PhuongThucThanhToanDto? PhuongThucThanhToanDuocChon { get => _phuongThucThanhToanDuocChon; set => SetProperty(ref _phuongThucThanhToanDuocChon, value); }
    public string LoaiGiaoDichDuocChon { get => _loaiGiaoDichDuocChon; set => SetProperty(ref _loaiGiaoDichDuocChon, value); }
    public string SoTienNhap { get => _soTienNhap; set => SetProperty(ref _soTienNhap, value); }
    public string NoiDung { get => _noiDung; set => SetProperty(ref _noiDung, value); }
    public bool CoLichSu => LichSuThanhToan.Count > 0;
    #endregion

    #region CONSTRUCTOR & COMMANDS
    public HoaDonChiTietViewModel(
        string maHoaDon, IHoaDonService hoaDonSvc, IDichVuService dichVuSvc, Func<Window?> layChuSoHuu,
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
        InHoaDonTienPhongCommand = new AsyncRelayCommand(async _ => await InHoaDonTienPhongAsync(), _ => !DangXuLy && !CoTheChinhSua);
        InHoaDonDichVuCommand = new AsyncRelayCommand(async _ => await InHoaDonDichVuAsync(), _ => !DangXuLy && !CoTheChinhSua);
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
    public ICommand InHoaDonTienPhongCommand { get; }
    public ICommand InHoaDonDichVuCommand { get; }
    #endregion

    #region LOGIC XỬ LÝ
    public enum KieuInHoaDon { TongHop, ChiTienPhong, ChiDichVu }

    private async Task<bool> InHoaDonMasterAsync(bool laTamTinh, KieuInHoaDon kieuIn = KieuInHoaDon.TongHop)
    {
        if (DangXuLy) return false; DangXuLy = true;
        try
        {
            var hd = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon); if (hd == null) return false;
            var khName = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
            bool daIn = laTamTinh ? _inHoaDon.XemTruocVaInTamTinh(hd, khName, NhanVienCheckOutDisplay, owner: _layChuSoHuu()) : _inHoaDon.XemTruocVaInHoaDon(hd, khName, NhanVienCheckOutDisplay, kieuIn, owner: _layChuSoHuu());
            if (daIn) await TaiLaiDuLieuNoiBoAsync(); return daIn;
        }
        catch (Exception ex) { _hopThoai.BaoLoi($"Loi in hoa don: {ex.Message}"); return false; }
        finally { DangXuLy = false; }
    }

    private async Task InHoaDonAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop);
    private async Task TamInAsync() => await InHoaDonMasterAsync(true);
    private async Task InHoaDonTienPhongAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.ChiTienPhong);
    private async Task InHoaDonDichVuAsync() => await InHoaDonMasterAsync(false, KieuInHoaDon.ChiDichVu);

    public async Task TaiLaiAsync()
    {
        if (DangXuLy) return; DangXuLy = true;
        try { await TaiLaiDuLieuNoiBoAsync(); } finally { DangXuLy = false; }
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
            KhuyenMai = hd.MaKhuyenMaiNavigation?.TenKhuyenMai ?? "Không";

            // 🔥 BƯỚC QUAN TRỌNG: GỌI BỘ NÃO TÍNH TOÁN
            decimal tienPhongDb = hd.TienPhong ?? 0;
            decimal tienDichVuDb = hd.TienDichVu ?? 0;
            decimal vatPercentDb = hd.Vat ?? 0;
            decimal tienCocDb = hd.MaDatPhongNavigation?.TienCoc ?? 0;
            decimal tongLichSu = hd.ThanhToans?.Sum(t => t.SoTien) ?? 0;

            var res = TinhToanHoaDonService.TinhToanToanBo(
                tienPhongDb, tienDichVuDb, vatPercentDb,
                hd.MaKhuyenMaiNavigation?.GiaTriKm ?? 0, hd.MaKhuyenMaiNavigation?.LoaiKhuyenMai ?? "",
                tienCocDb, tongLichSu
            );

            // Gán dữ liệu hiển thị (Bao gồm VAT và Tổng)
            TienPhong = res.TienPhong;
            TienDichVu = res.TienDichVu;
            VatPercent = vatPercentDb;
            VatAmount = res.TienVat;        // Không tự tính nữa, lấy từ Service
            TienCoc = tienCocDb;
            TongThanhToan = res.TongThanhToan;
            TongDaThu = tienCocDb + tongLichSu;
            ConLai = res.ConLai;            // Khi set biến này, LabelDuNo sẽ tự động chớp chớp nháy nháy theo!

            var maPhongDangChon = SelectedPhong?.MaPhong;
            Phongs.Clear();
            foreach (var p in hd.HoaDonChiTiets.Select(p => new PhongItemVm
            {
                MaPhong = p.MaPhong,
                NgayNhan = p.DatPhongChiTiet.NgayNhan,
                NgayTra = p.DatPhongChiTiet.NgayTra,
                // 🔥 FIX LỖI SỐ ĐÊM SAI TRONG CSDL CŨ BẰNG CÁCH CHO TÍNH TRỰC TIẾP
                SoDem = TinhToanHoaDonService.TinhSoDem(p.DatPhongChiTiet.NgayNhan, p.DatPhongChiTiet.NgayTra),
                DonGia = p.DatPhongChiTiet.DonGia
            })) Phongs.Add(p);

            SelectedPhong = !string.IsNullOrWhiteSpace(maPhongDangChon) ? Phongs.FirstOrDefault(x => x.MaPhong == maPhongDangChon) ?? Phongs.FirstOrDefault() : Phongs.FirstOrDefault();

            DichVus.Clear();
            foreach (var d in hd.DichVuChiTiets.Select(d => new DichVuItemVm { TenDichVu = d.MaDichVuNavigation.TenDichVu, SoLuong = d.SoLuong, DonGia = d.DonGia })) DichVus.Add(d);

            await Task.WhenAll(TaiLaiLichSuThanhToanNoiBoAsync(hd.ThanhToans), TaiLaiPhuongThucThanhToanAsync());

            if (!CoTheChinhSua) { SoTienNhap = ""; NoiDung = ""; LoaiGiaoDichDuocChon = "Thanh toán cuối"; }
        }
        catch (Exception ex) { Logger.LogError("Lỗi", ex); _hopThoai.BaoLoi($"Loi tai chi tiet hoa don: {ex.Message}"); }
    }

    private async Task TaiLaiLichSuThanhToanNoiBoAsync(IEnumerable<ThanhToan>? tts)
    {
        LichSuThanhToan.Clear();
        if (tts != null)
        {
            foreach (var t in tts.OrderBy(x => x.NgayThanhToan))
                LichSuThanhToan.Add(new ThanhToanItemVm { LoaiGiaoDich = t.LoaiGiaoDich ?? "", SoTien = t.SoTien, NgayThanhToan = t.NgayThanhToan, TenNhanVienThuTien = t.NguoiThuNavigation?.TenNhanVien ?? "Hệ thống" });
        }
        OnPropertyChanged(nameof(CoLichSu));

        // Đổ sẵn tiền gợi ý
        if (CoTheChinhSua)
        {
            SoTienNhap = ConLai != 0 ? Math.Abs(ConLai).ToString("N0") : "0";
            LoaiGiaoDichDuocChon = ConLai < 0 ? "Hoàn tiền" : "Thanh toán cuối";
        }
    }

    private async Task TaiLaiPhuongThucThanhToanAsync()
    {
        if (_cachePhuongThucThanhToan != null && (DateTime.Now - _thoiDiemTaiPttt) <= ThoiGianHetHanPttt) { GanDanhSachPhuongThucThanhToan(_cachePhuongThucThanhToan); return; }
        var list = await _hoaDon.LayDanhSachPhuongThucThanhToanAsync();
        _cachePhuongThucThanhToan = list; _thoiDiemTaiPttt = DateTime.Now; GanDanhSachPhuongThucThanhToan(list);
    }

    private void GanDanhSachPhuongThucThanhToan(IEnumerable<PhuongThucThanhToanDto> danhSach)
    {
        var maDangChon = PhuongThucThanhToanDuocChon?.MaPTTT;
        DanhSachPhuongThucThanhToan.Clear(); foreach (var p in danhSach) DanhSachPhuongThucThanhToan.Add(p);
        if (!string.IsNullOrWhiteSpace(maDangChon)) PhuongThucThanhToanDuocChon = DanhSachPhuongThucThanhToan.FirstOrDefault(x => x.MaPTTT == maDangChon) ?? DanhSachPhuongThucThanhToan.FirstOrDefault();
        else PhuongThucThanhToanDuocChon ??= DanhSachPhuongThucThanhToan.FirstOrDefault();
    }

    private async Task ThemDichVuAsync()
    {
        if (TrangThai != HoaDonTrangThaiTexts.ChuaThanhToan) { _hopThoai.CanhBao("Hoá đơn đã thanh toán, không thể thêm dịch vụ."); return; }
        var owner = _layChuSoHuu();
        try
        {
            var dvs = await _dichVuSvc.GetAllDichVuAsync();
            var dichVus = dvs.Select(d => new DichVuViewModel { MaDichVu = d.MaDichVu, TenDichVu = d.TenDichVu, Gia = d.Gia ?? 0, IsActive = d.IsActive ?? false }).ToList();
            var chon = _chonDichVu.ChonDichVu(owner, dichVus, Phongs.Select(p => p.MaPhong).ToList());
            if (!chon.HasValue) return;
            var (maDichVu, soLuong, maPhong) = chon.Value;
            var phong = SelectedPhong?.MaPhong ?? Phongs.FirstOrDefault()?.MaPhong;
            if (string.IsNullOrWhiteSpace(MaDatPhong) || string.IsNullOrWhiteSpace(phong)) { _hopThoai.CanhBao("Khong xac dinh duoc phong de them dich vu."); return; }
            await _dichVuSvc.UpsertDichVuAsync(_maHoaDon, MaDatPhong, phong, maDichVu, soLuong);
            await TaiLaiAsync(); if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex) { Logger.LogError("Lỗi", ex); _hopThoai.BaoLoi($"Loi them dich vu: {ex.Message}"); }
    }

    private async Task ThanhToanAsync()
    {
        if (PhuongThucThanhToanDuocChon?.MaPTTT is not string maPttt || string.IsNullOrWhiteSpace(maPttt)) { _hopThoai.CanhBao("Vui lòng chọn phương thức thanh toán."); return; }
        decimal soTienHienTai = 0;
        if (ConLai != 0 && (!ThuParseSoTien(SoTienNhap, out soTienHienTai) || soTienHienTai <= 0)) { _hopThoai.CanhBao("Số tiền giao dịch không hợp lệ."); return; }

        var maNhanVien = AppSession.MaNhanVien ?? "NV001";
        string cauHoi = LoaiGiaoDichDuocChon == "Hoàn tiền" ? $"Xác nhận HOÀN TRẢ {soTienHienTai:N0} đ cho khách?" : (soTienHienTai > 0 ? $"Xác nhận thanh toán {soTienHienTai:N0} đ?" : "Xác nhận chốt hóa đơn?");
        if (!_hopThoai.XacNhan(cauHoi, "Xác nhận giao dịch")) return;

        DangXuLy = true;
        try
        {
            if (LoaiGiaoDichDuocChon == "Thanh toán cuối" && ConLai > 0 && soTienHienTai < ConLai) { _hopThoai.CanhBao($"Số tiền không đủ để chốt. Cần: {ConLai:N0}đ"); return; }
            decimal soTienThucTe = LoaiGiaoDichDuocChon == "Hoàn tiền" ? -soTienHienTai : soTienHienTai;
            var thongTin = await _hoaDon.ThanhToanVaTraKetQuaAsync(_maHoaDon, soTienThucTe, maPttt, maNhanVien, LoaiGiaoDichDuocChon, NoiDung);
            SoTienNhap = "";

            if (thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat)
            {
                if (LoaiGiaoDichDuocChon is "Thanh toán cuối" or "Hoàn tiền" || ConLai == 0) await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);
                _hopThoai.ThongBao("Giao dịch thành công!");
                DangXuLy = false; await InHoaDonMasterAsync(false, KieuInHoaDon.TongHop); DangXuLy = true;
                if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
                await TaiLaiDuLieuNoiBoAsync(); return;
            }
            _hopThoai.ThongBao(thongTin.ThongDiep); await TaiLaiDuLieuNoiBoAsync(); if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex) { Logger.LogError("Lỗi", ex); _hopThoai.BaoLoi($"Lỗi: {ex.Message}"); }
        finally { DangXuLy = false; }
    }

    private async Task TraPhongAsync()
    {
        if (!CoTheTraPhong) return;
        if (!_hopThoai.XacNhan($"Xac nhan tra phong cho hoa don {_maHoaDon}?", "Xac nhan tra phong")) return;
        DangXuLy = true;
        try
        {
            var maNhanVien = AppSession.MaNhanVien;
            if (string.IsNullOrWhiteSpace(maNhanVien)) { MessageBox.Show("Phiên đăng nhập hết hạn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            await _hoaDon.TraPhongAsync(_maHoaDon, maNhanVien);
            _hopThoai.ThongBao("Tra phong thanh cong!");
            await TaiLaiDuLieuNoiBoAsync();
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            _dong(true);
        }
        catch (Exception ex) { Logger.LogError("Lỗi", ex); _hopThoai.BaoLoi($"Loi tra phong: {ex.Message}"); }
        finally { DangXuLy = false; }
    }

    private async Task CapNhatTraSomHomNayAsync()
    {
        if (!CoTheCapNhatTraSom) return;
        if (!_hopThoai.XacNhan("Cap nhat ngay tra phong = hom nay va tinh lai tien phong?", "Cap nhat tra som")) return;
        DangXuLy = true;
        try
        {
            var thongTin = await _hoaDon.CapNhatTienPhongKhiTraSomAsync(_maHoaDon, TimeHelper.GetVietnamTime());
            if (thongTin.KetQua == KetQuaThanhToan.TuChoi) { _hopThoai.CanhBao(thongTin.ThongDiep); return; }
            if (thongTin.ConLai <= 0) _hopThoai.ThongBao("Da tinh lai tong tien. Hoa don hien tai da du/dang du tien.");
            else _hopThoai.ThongBao($"Da tinh lai tong tien. Con lai: {thongTin.ConLai:N0} đ.");
            if (_taiLaiTrangHoaDonAsync != null) await _taiLaiTrangHoaDonAsync();
            await TaiLaiDuLieuNoiBoAsync();
        }
        catch (Exception ex) { Logger.LogError("Lỗi", ex); _hopThoai.BaoLoi($"Loi cap nhat tra som: {ex.Message}"); }
        finally { DangXuLy = false; }
    }

    private void RaiseAllCanExecuteChanged()
    {
        (TaiLaiCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (TamInCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (ThemDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThanhToanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (TraPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CapNhatTraSomHomNayCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (InHoaDonTienPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (DongCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private static bool ThuParseSoTien(string? text, out decimal soTien)
    {
        soTien = 0; if (string.IsNullOrWhiteSpace(text)) return false;
        var digits = Regex.Replace(text, @"[^\d\-]", "");
        if (digits == "-" || string.IsNullOrWhiteSpace(digits)) return false;
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
    public string SoTienText => SoTien < 0 ? $"- {Math.Abs(SoTien):N0} ₫" : $"+ {SoTien:N0} ₫";
}
#endregion