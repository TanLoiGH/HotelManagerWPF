using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public sealed class HoaDonChiTietViewModel : BaseViewModel
{
    private static readonly CultureInfo VanHoaVietNam = new("vi-VN");
    private static readonly CultureInfo VanHoaMy = new("en-US");

    private readonly string _maHoaDon;
    private readonly HoaDonService _hoaDon;
    private readonly DichVuService _dichVuSvc;
    private readonly Func<Window?> _getOwner;
    private readonly Action<bool?> _close;
    private readonly IHopThoaiService _hopThoai;
    private readonly IInHoaDonService _inHoaDon;
    private readonly IChonDichVuService _chonDichVu;
    private readonly Func<Task>? _taiLaiTrangHoaDonAsync;

    private bool _isBusy;
    private string _khachHang = "";
    private string _nhanVien = "";
    private DateTime? _ngayLap;
    private string _trangThai = "";
    private string _trangThaiDatPhong = "";
    private string _maDatPhong = "";
    private decimal _tienPhong;
    private decimal _tienDichVu;
    private decimal _tongThanhToan;
    private decimal _vatPercent;
    private string _khuyenMai = "Không";

    private PhongItemVm? _selectedPhong;

    private string _soTienNhap = "";
    private PhuongThucThanhToanDto? _selectedPttt;
    private string _selectedLoaiGiaoDich = "Thanh toán cuối";
    private string _noiDung = "";
    private decimal _conLai;

    public HoaDonChiTietViewModel(
        string maHoaDon,
        HoaDonService hoaDonSvc,
        DichVuService dichVuSvc,
        Func<Window?> getOwner,
        Action<bool?> close,
        IHopThoaiService hopThoai,
        IInHoaDonService inHoaDon,
        IChonDichVuService chonDichVu,
        Func<Task>? taiLaiTrangHoaDonAsync = null)
    {
        _maHoaDon = maHoaDon;
        _hoaDon = hoaDonSvc;
        _dichVuSvc = dichVuSvc;
        _getOwner = getOwner;
        _close = close;
        _hopThoai = hopThoai;
        _inHoaDon = inHoaDon;
        _chonDichVu = chonDichVu;
        _taiLaiTrangHoaDonAsync = taiLaiTrangHoaDonAsync;

        ReloadCommand = new AsyncRelayCommand(async _ => await ReloadAsync(), _ => !IsBusy);
        TamInCommand = new AsyncRelayCommand(async _ => await TamInAsync(), _ => !IsBusy && IsEditable);
        InHoaDonCommand = new AsyncRelayCommand(async _ => await InHoaDonAsync(), _ => !IsBusy && !IsEditable);
        ThemDichVuCommand = new AsyncRelayCommand(async _ => await ThemDichVuAsync(), _ => !IsBusy && IsEditable);
        ThanhToanCommand = new AsyncRelayCommand(async _ => await ThanhToanAsync(), _ => !IsBusy && IsEditable);
        TraPhongCommand = new AsyncRelayCommand(async _ => await TraPhongAsync(), _ => !IsBusy && CanTraPhong);
        CapNhatTraSomHomNayCommand = new AsyncRelayCommand(async _ => await CapNhatTraSomHomNayAsync(), _ => !IsBusy && CanCapNhatTraSom);
        CloseCommand = new RelayCommand(_ => _close(false));
    }

    public ICommand ReloadCommand { get; }
    public ICommand TamInCommand { get; }
    public ICommand InHoaDonCommand { get; }
    public ICommand ThemDichVuCommand { get; }
    public ICommand ThanhToanCommand { get; }
    public ICommand TraPhongCommand { get; }
    public ICommand CapNhatTraSomHomNayCommand { get; }
    public ICommand CloseCommand { get; }

    public string MaHoaDon => _maHoaDon;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            RaiseAllCanExecuteChanged();
        }
    }

    public string KhachHang { get => _khachHang; private set => SetProperty(ref _khachHang, value); }
    public string NhanVien { get => _nhanVien; private set => SetProperty(ref _nhanVien, value); }
    public DateTime? NgayLap { get => _ngayLap; private set => SetProperty(ref _ngayLap, value); }
    public string TrangThai
    {
        get => _trangThai;
        private set
        {
            if (!SetProperty(ref _trangThai, value)) return;
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(CanCapNhatTraSom));
            RaiseAllCanExecuteChanged();
        }
    }

    public bool IsEditable => TrangThai == "Chưa thanh toán";

    public string MaDatPhong { get => _maDatPhong; private set => SetProperty(ref _maDatPhong, value); }
    public string TrangThaiDatPhong
    {
        get => _trangThaiDatPhong;
        private set
        {
            if (!SetProperty(ref _trangThaiDatPhong, value)) return;
            OnPropertyChanged(nameof(CanTraPhong));
            OnPropertyChanged(nameof(CanCapNhatTraSom));
            RaiseAllCanExecuteChanged();
        }
    }

    public bool CanTraPhong => TrangThai == "Đã thanh toán" && TrangThaiDatPhong != "Đã trả phòng" && !string.IsNullOrWhiteSpace(MaDatPhong);
    public bool CanCapNhatTraSom => IsEditable && !string.IsNullOrWhiteSpace(MaDatPhong) && TrangThaiDatPhong != "Đã trả phòng";

    public decimal TienPhong { get => _tienPhong; private set { if (SetProperty(ref _tienPhong, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal TienDichVu { get => _tienDichVu; private set { if (SetProperty(ref _tienDichVu, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public decimal TongThanhToan { get => _tongThanhToan; private set => SetProperty(ref _tongThanhToan, value); }
    public decimal VatPercent { get => _vatPercent; private set { if (SetProperty(ref _vatPercent, value)) OnPropertyChanged(nameof(VatAmount)); } }
    public string KhuyenMai { get => _khuyenMai; private set => SetProperty(ref _khuyenMai, value); }

    public decimal VatAmount => (TienPhong + TienDichVu) * (VatPercent / 100m);

    public string TienPhongText => $"{TienPhong:N0} ₫";
    public string TienDichVuText => $"{TienDichVu:N0} ₫";
    public string VatPercentText => $"{VatPercent:N0}%";
    public string VatAmountText => $"{VatAmount:N0} ₫";
    public string TongThanhToanText => $"{TongThanhToan:N0} ₫";

    public ObservableCollection<PhongItemVm> Phongs { get; } = new();
    public ObservableCollection<DichVuItemVm> DichVus { get; } = new();

    public PhongItemVm? SelectedPhong
    {
        get => _selectedPhong;
        set => SetProperty(ref _selectedPhong, value);
    }

    public ObservableCollection<PhuongThucThanhToanDto> PhuongThucThanhToans { get; } = new();
    public ObservableCollection<string> LoaiGiaoDichOptions { get; } =
    [
        "Thanh toán cuối",
        "Đặt cọc",
        "Hoàn tiền"
    ];

    public PhuongThucThanhToanDto? SelectedPTTT
    {
        get => _selectedPttt;
        set => SetProperty(ref _selectedPttt, value);
    }

    public string SelectedLoaiGiaoDich
    {
        get => _selectedLoaiGiaoDich;
        set => SetProperty(ref _selectedLoaiGiaoDich, value);
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

    public ObservableCollection<ThanhToanItemVm> LichSuThanhToan { get; } = new();

    public bool HasLichSu => LichSuThanhToan.Count > 0;

    public decimal ConLai
    {
        get => _conLai;
        private set
        {
            if (!SetProperty(ref _conLai, value)) return;
            OnPropertyChanged(nameof(ConLaiText));
            OnPropertyChanged(nameof(ConLaiHienThiText));
        }
    }

    public string ConLaiText => $"Còn lại: {ConLai:N0} ₫";
    public string ConLaiHienThiText => ConLai >= 0 ? $"Còn lại: {ConLai:N0} ₫" : $"Dư: {Math.Abs(ConLai):N0} ₫";

    public async Task ReloadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            await TaiLaiDuLieuCoreAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TaiLaiDuLieuCoreAsync()
    {
        try
        {
            await _hoaDon.EnsureHoaDonChiTietAsync(_maHoaDon);
            var hd = await _hoaDon.LayHoaDonChiTietAsync(_maHoaDon);

            if (hd == null) return;

            KhachHang = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
            NhanVien = hd.MaNhanVienNavigation?.TenNhanVien ?? "";
            NgayLap = hd.NgayLap;
            TrangThai = hd.TrangThai ?? "";
            MaDatPhong = hd.MaDatPhong ?? "";
            TrangThaiDatPhong = hd.MaDatPhongNavigation?.TrangThai ?? "";

            TienPhong = hd.TienPhong ?? 0;
            TienDichVu = hd.TienDichVu ?? 0;
            TongThanhToan = hd.TongThanhToan ?? 0;
            VatPercent = hd.Vat ?? 0;
            KhuyenMai = hd.MaKhuyenMaiNavigation?.TenKhuyenMai ?? "Không";

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

            OnPropertyChanged(nameof(TienPhongText));
            OnPropertyChanged(nameof(TienDichVuText));
            OnPropertyChanged(nameof(VatPercentText));
            OnPropertyChanged(nameof(TongThanhToanText));

            await Task.WhenAll(
                    ReloadPaymentAsync(),
                    ReloadPtttAsync()
                );

            if (!IsEditable)
            {
                SoTienNhap = "";
                NoiDung = "";
                SelectedLoaiGiaoDich = "Thanh toán cuối";
            }
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi tai chi tiet hoa don: {ex.Message}");
        }
    }

    private async Task ReloadPtttAsync()
    {
        using var db = new Data.QuanLyKhachSanContext();
        var svc = new HoaDonService(db, new KhachHangService(db));
        var list = await svc.GetPTTTAsync();
        PhuongThucThanhToans.Clear();
        foreach (var p in list)
            PhuongThucThanhToans.Add(p);

        SelectedPTTT ??= PhuongThucThanhToans.FirstOrDefault();
    }

    private async Task ReloadPaymentAsync()
    {
        using var db = new Data.QuanLyKhachSanContext();
        var svc = new HoaDonService(db, new KhachHangService(db));
        var tts = await svc.LayLichSuThanhToanAsync(_maHoaDon);
        var items = tts.Select(t => new ThanhToanItemVm
        {
            LoaiGiaoDich = t.LoaiGiaoDich ?? "",
            SoTien = t.SoTien,
            NgayThanhToan = t.NgayThanhToan
        }).ToList();

        LichSuThanhToan.Clear();
        foreach (var t in items)
            LichSuThanhToan.Add(t);

        OnPropertyChanged(nameof(HasLichSu));

        decimal tongDaThu = items.Sum(t => t.SoTien);
        ConLai = TongThanhToan - tongDaThu;

        if (IsEditable && string.IsNullOrWhiteSpace(SoTienNhap))
            SoTienNhap = ConLai > 0 ? $"{ConLai:N0}" : "";
    }

    private async Task ThemDichVuAsync()
    {
        if (TrangThai != "Chưa thanh toán")
        {
            _hopThoai.CanhBao("Hoa don da thanh toan, khong the them dich vu.");
            return;
        }

        var owner = _getOwner();

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
            await ReloadAsync();
            if (_taiLaiTrangHoaDonAsync != null)
                await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi them dich vu: {ex.Message}");
        }
    }

    private async Task ThanhToanAsync()
    {
        if (SelectedPTTT?.MaPTTT is not string maPttt || string.IsNullOrWhiteSpace(maPttt))
        {
            _hopThoai.CanhBao("Vui long chon phuong thuc thanh toan.");
            return;
        }

        // Nếu đã dư/đủ tiền, cho phép chốt hóa đơn mà không cần nhập thêm tiền.
        decimal soTien = 0;
        if (ConLai > 0)
        {
            if (!ThuParseSoTien(SoTienNhap, out soTien) || soTien <= 0)
            {
                _hopThoai.CanhBao("So tien khong hop le.");
                return;
            }
        }

        var maNhanVien = App.CurrentUser?.MaNhanVien ?? "NV001";

        if (!_hopThoai.XacNhan($"Xac nhan thanh toan so tien {soTien:N0} d cho hoa don {_maHoaDon}?", "Xac nhan thanh toan"))
            return;

        IsBusy = true;
        try
        {
            // Logic mới:
            // - "Thanh toán cuối": bắt buộc in hóa đơn trước, in xong mới ghi nhận thanh toán.
            // - Các loại giao dịch khác (đặt cọc/hoàn tiền): vẫn ghi nhận như bình thường.
            if (SelectedLoaiGiaoDich == "Thanh toán cuối")
            {
                if (ConLai > 0 && soTien < ConLai)
                {
                    _hopThoai.CanhBao("So tien thanh toan chua du de chot hoa don.");
                    return;
                }

                // Nếu đã đủ tiền (ConLai <= 0) thì chỉ cần in + đồng bộ trạng thái.
                if (ConLai <= 0)
                {
                    var hdPrint0 = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);
                    if (hdPrint0 == null) return;

                    var khName0 = hdPrint0.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
                    var staffName0 = hdPrint0.MaNhanVienNavigation?.TenNhanVien ?? "N/A";

                    bool printed0 = _inHoaDon.XemTruocVaInHoaDon(hdPrint0, khName0, staffName0, owner: _getOwner());
                    if (!printed0) return;

                    await _hoaDon.DongBoTrangThaiThanhToanAsync(_maHoaDon);

                    _hopThoai.ThongBao("Thanh toan hoan tat! Co the tra phong khi khach roi di.");
                    if (_taiLaiTrangHoaDonAsync != null)
                        await _taiLaiTrangHoaDonAsync();
                    await TaiLaiDuLieuCoreAsync();
                    return;
                }

                var hdPrint = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);
                if (hdPrint == null) return;

                var khName = hdPrint.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
                var staffName = hdPrint.MaNhanVienNavigation?.TenNhanVien ?? "N/A";

                bool printed = _inHoaDon.XemTruocVaInHoaDon(hdPrint, khName, staffName, owner: _getOwner());
                if (!printed)
                    return;
            }

            var thongTin = await _hoaDon.ThanhToanVaTraKetQuaAsync(
                _maHoaDon, soTien, maPttt, maNhanVien, SelectedLoaiGiaoDich, NoiDung);

            SoTienNhap = "";

            if (thongTin.KetQua is KetQuaThanhToan.HoanTat or KetQuaThanhToan.DaHoanTat)
            {
                _hopThoai.ThongBao("Thanh toan hoan tat! Co the tra phong khi khach roi di.");
                if (_taiLaiTrangHoaDonAsync != null)
                    await _taiLaiTrangHoaDonAsync();
                await TaiLaiDuLieuCoreAsync();
                _close(true);
                return;
            }

            if (thongTin.KetQua == KetQuaThanhToan.GhiNhanChuaDu)
                _hopThoai.ThongBao($"Da ghi nhan thanh toan {soTien:N0} d. Khach chua thanh toan du.");
            else
                _hopThoai.CanhBao(thongTin.ThongDiep);

            await TaiLaiDuLieuCoreAsync();
            if (_taiLaiTrangHoaDonAsync != null)
                await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi thanh toan: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TraPhongAsync()
    {
        if (!CanTraPhong)
            return;

        if (!_hopThoai.XacNhan($"Xac nhan tra phong cho hoa don {_maHoaDon}?", "Xac nhan tra phong"))
            return;

        IsBusy = true;
        try
        {
            var maNhanVien = App.CurrentUser?.MaNhanVien ?? "NV001";
            await _hoaDon.TraPhongAsync(_maHoaDon, maNhanVien);
            _hopThoai.ThongBao("Tra phong thanh cong! Phong da chuyen sang trang thai can don dep.");
            await TaiLaiDuLieuCoreAsync();

            if (_taiLaiTrangHoaDonAsync != null)
                await _taiLaiTrangHoaDonAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi tra phong: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CapNhatTraSomHomNayAsync()
    {
        if (!CanCapNhatTraSom) return;

        if (!_hopThoai.XacNhan("Cap nhat ngay tra phong = hom nay va tinh lai tien phong?", "Cap nhat tra som"))
            return;

        IsBusy = true;
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
                _hopThoai.ThongBao($"Da tinh lai tong tien. Con lai: {thongTin.ConLai:N0} d.");

            if (_taiLaiTrangHoaDonAsync != null)
                await _taiLaiTrangHoaDonAsync();

            await TaiLaiDuLieuCoreAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi cap nhat tra som: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task InHoaDonAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var hd = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);

            if (hd == null) return;

            var khName = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
            var staffName = hd.MaNhanVienNavigation?.TenNhanVien ?? "N/A";

            bool daIn = _inHoaDon.XemTruocVaInHoaDon(hd, khName, staffName, owner: _getOwner());
            if (daIn)
                await TaiLaiDuLieuCoreAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi in hoa don: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TamInAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var hd = await _hoaDon.LayHoaDonDeInAsync(_maHoaDon);

            if (hd == null) return;

            var khName = hd.MaDatPhongNavigation?.MaKhachHangNavigation?.TenKhachHang ?? "";
            var staffName = hd.MaNhanVienNavigation?.TenNhanVien ?? "N/A";

            bool daIn = _inHoaDon.XemTruocVaInTamTinh(hd, khName, staffName, owner: _getOwner());
            if (daIn)
                await TaiLaiDuLieuCoreAsync();
        }
        catch (Exception ex)
        {
            _hopThoai.BaoLoi($"Loi tam in: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseAllCanExecuteChanged()
    {
        (ReloadCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TamInCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (InHoaDonCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThemDichVuCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ThanhToanCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (TraPhongCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CapNhatTraSomHomNayCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CloseCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        if (decimal.TryParse(raw,
                NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                VanHoaVietNam,
                out soTien))
            return true;

        if (decimal.TryParse(raw,
                NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                VanHoaMy,
                out soTien))
            return true;

        var digits = Regex.Replace(raw, @"[^\d\-]", "");
        if (digits == "-" || digits.Length == 0) return false;

        return decimal.TryParse(digits, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out soTien);
    }
}

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
    public string SoTienText => $"{SoTien:N0} ₫";
}
