using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using GreenWoodHotel.Models;

namespace GreenWoodHotel.ViewModels
{
    public class DatPhongViewModel : INotifyPropertyChanged
    {
        // ─── DB Connection ───────────────────────────────────────────────────
        // ⚠ Thay chuỗi kết nối phù hợp với môi trường của bạn
        private const string ConnStr =
            "Server=.;Database=HotelManagement;Integrated Security=True;TrustServerCertificate=True;";

        // ─── Collections ─────────────────────────────────────────────────────
        public ObservableCollection<PhongModel>  AllRooms     { get; } = new();
        public ObservableCollection<PhongModel>  FilteredRooms{ get; } = new();
        public ObservableCollection<DichVuModel> DichVuList   { get; } = new();

        // ─── Selected State ──────────────────────────────────────────────────
        public PhongModel?    SelectedRoom  { get; set; }
        public KhachHangModel? SelectedKhach { get; set; }

        // ─── Summary Calculated ──────────────────────────────────────────────
        private decimal _tienPhong, _tienDichVu, _vat, _tongTien;

        public string TienDichVuFormatted  => FormatVnd(_tienDichVu);
        public string VATFormatted         => FormatVnd(_vat);
        public string TongTienFormatted    => FormatVnd(_tongTien);

        // ─── Constructor ─────────────────────────────────────────────────────
        public DatPhongViewModel()
        {
            _ = LoadRoomsAsync();
            _ = LoadDichVuAsync();
        }

        // ─── LOAD ROOMS from DB ──────────────────────────────────────────────
        public async Task LoadRoomsAsync()
        {
            AllRooms.Clear();
            const string sql = @"
                SELECT p.MaPhong, p.MaLoaiPhong, p.MaTrangThaiPhong,
                       lp.TenLoaiPhong, lp.SoNguoiToiDa, lp.GiaPhong,
                       tt.TenTrangThai,
                       CAST(LEFT(p.MaPhong,1) AS INT) AS Tang
                FROM   PHONG p
                JOIN   LOAI_PHONG      lp ON lp.MaLoaiPhong      = p.MaLoaiPhong
                JOIN   PHONG_TRANG_THAI tt ON tt.MaTrangThaiPhong = p.MaTrangThaiPhong
                ORDER  BY p.MaPhong";

            await using var conn = new SqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var rd  = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                AllRooms.Add(new PhongModel
                {
                    MaPhong          = rd.GetString("MaPhong"),
                    MaLoaiPhong      = rd.GetString("MaLoaiPhong"),
                    TenLoaiPhong     = rd.GetString("TenLoaiPhong"),
                    MaTrangThaiPhong = rd.GetString("MaTrangThaiPhong"),
                    TenTrangThai     = rd.GetString("TenTrangThai"),
                    SoNguoiToiDa     = rd.GetInt32("SoNguoiToiDa"),
                    GiaPhong         = rd.GetDecimal("GiaPhong"),
                    Tang             = rd.GetInt32("Tang")
                });
            }
            FilterRooms("all");
        }

        // ─── LOAD DICH VU ────────────────────────────────────────────────────
        public async Task LoadDichVuAsync()
        {
            DichVuList.Clear();
            const string sql = @"
                SELECT MaDichVu, TenDichVu, Gia, DonViTinh
                FROM   DICH_VU
                WHERE  IsActive = 1
                ORDER  BY TenDichVu";

            await using var conn = new SqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var rd  = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                DichVuList.Add(new DichVuModel
                {
                    MaDichVu  = rd.GetString("MaDichVu"),
                    TenDichVu = rd.GetString("TenDichVu"),
                    Gia       = rd.GetDecimal("Gia"),
                    DonViTinh = rd.IsDBNull("DonViTinh") ? "" : rd.GetString("DonViTinh")
                });
            }
        }

        // ─── FILTER ROOMS ────────────────────────────────────────────────────
        public void FilterRooms(string filter)
        {
            FilteredRooms.Clear();
            var list = filter switch
            {
                "empty" => AllRooms.Where(r => r.MaTrangThaiPhong == "PTT01"),
                "2p"    => AllRooms.Where(r => r.SoNguoiToiDa <= 2),
                "4p"    => AllRooms.Where(r => r.SoNguoiToiDa >= 4),
                _       => AllRooms.AsEnumerable()
            };
            foreach (var r in list) FilteredRooms.Add(r);
        }

        // ─── SELECT ROOM ─────────────────────────────────────────────────────
        public void SelectRoom(PhongModel room)
        {
            // Deselect previous
            foreach (var r in AllRooms) r.IsSelected = false;
            room.IsSelected = true;
            SelectedRoom = room;
        }

        // ─── SEARCH GUEST ────────────────────────────────────────────────────
        public async Task<List<KhachHangModel>> SearchGuestAsync(string keyword)
        {
            const string sql = @"
                SELECT TOP 10 MaKhachHang, TenKhachHang, DienThoai, CCCD, Email, DiaChi,
                              MaLoaiKhach, TongTichLuy
                FROM   KHACH_HANG
                WHERE  TenKhachHang LIKE @kw
                    OR DienThoai    LIKE @kw
                    OR CCCD         LIKE @kw";

            var result = new List<KhachHangModel>();
            await using var conn = new SqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            await using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                result.Add(new KhachHangModel
                {
                    MaKhachHang  = rd.GetString("MaKhachHang"),
                    TenKhachHang = rd.GetString("TenKhachHang"),
                    DienThoai    = rd.IsDBNull("DienThoai") ? "" : rd.GetString("DienThoai"),
                    CCCD         = rd.IsDBNull("CCCD")      ? "" : rd.GetString("CCCD"),
                    Email        = rd.IsDBNull("Email")     ? "" : rd.GetString("Email"),
                    DiaChi       = rd.IsDBNull("DiaChi")    ? "" : rd.GetString("DiaChi"),
                    MaLoaiKhach  = rd.GetString("MaLoaiKhach"),
                    TongTichLuy  = rd.GetDecimal("TongTichLuy")
                });
            }
            return result;
        }

        // ─── CALCULATE SUMMARY ───────────────────────────────────────────────
        public void CalculateSummary(int soDem)
        {
            if (SelectedRoom == null) return;

            _tienPhong  = SelectedRoom.GiaPhong * soDem;
            _tienDichVu = DichVuList
                .Where(d => d.IsSelected)
                .Sum(d => d.Gia);
            _vat        = (_tienPhong + _tienDichVu) * 0.10m;
            _tongTien   = _tienPhong + _tienDichVu + _vat;

            OnPropertyChanged(nameof(TienDichVuFormatted));
            OnPropertyChanged(nameof(VATFormatted));
            OnPropertyChanged(nameof(TongTienFormatted));
        }

        // ─── CONFIRM BOOKING ─────────────────────────────────────────────────
        public async Task<string> ConfirmBookingAsync(DatPhongRequest req)
        {
            await using var conn = new SqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 1. Upsert KHACH_HANG (tìm theo CCCD hoặc tạo mới)
                string maKhach = await UpsertKhachHangAsync(conn, tx, req);

                // 2. Tạo DAT_PHONG
                string maDatPhong = GenerateId("DP");
                const string sqlDP = @"
                    INSERT INTO DAT_PHONG (MaDatPhong, MaKhachHang, NgayDat, TrangThai)
                    VALUES (@mdp, @mkh, GETDATE(), N'Chờ nhận phòng')";
                await using (var cmd = new SqlCommand(sqlDP, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@mdp", maDatPhong);
                    cmd.Parameters.AddWithValue("@mkh", maKhach);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Tạo DAT_PHONG_CHI_TIET
                const string sqlDPCT = @"
                    INSERT INTO DAT_PHONG_CHI_TIET
                           (MaDatPhong, MaPhong, NgayNhan, NgayTra, DonGia, MaNhanVien)
                    VALUES (@mdp, @mp, @nn, @nt, @dg, @mnv)";
                await using (var cmd = new SqlCommand(sqlDPCT, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@mdp", maDatPhong);
                    cmd.Parameters.AddWithValue("@mp",  req.MaPhong);
                    cmd.Parameters.AddWithValue("@nn",  req.NgayNhan);
                    cmd.Parameters.AddWithValue("@nt",  req.NgayTra);
                    cmd.Parameters.AddWithValue("@dg",  SelectedRoom!.GiaPhong);
                    cmd.Parameters.AddWithValue("@mnv", (object?)req.MaNhanVien ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Cập nhật trạng thái phòng → PTT05 (Đã đặt trước)
                const string sqlPTT = @"
                    UPDATE PHONG SET MaTrangThaiPhong = 'PTT05'
                    WHERE  MaPhong = @mp";
                await using (var cmd = new SqlCommand(sqlPTT, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@mp", req.MaPhong);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. Tạo HOA_DON
                string maHoaDon   = GenerateId("HD");
                int    soDem      = (req.NgayTra - req.NgayNhan).Days;
                decimal tienPhong = SelectedRoom.GiaPhong * soDem;
                decimal tienDV    = DichVuList.Where(d => d.IsSelected).Sum(d => d.Gia);
                decimal vat       = 10m;
                decimal tongTT    = (tienPhong + tienDV) * 1.10m;

                const string sqlHD = @"
                    INSERT INTO HOA_DON
                           (MaHoaDon, MaDatPhong, MaNhanVien, TienPhong,
                            TienDichVu, VAT, TongThanhToan, TrangThai)
                    VALUES (@mhd, @mdp, @mnv, @tp, @tdv, @vat, @tt, N'Chưa thanh toán')";
                await using (var cmd = new SqlCommand(sqlHD, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@mhd", maHoaDon);
                    cmd.Parameters.AddWithValue("@mdp", maDatPhong);
                    cmd.Parameters.AddWithValue("@mnv", (object?)req.MaNhanVien ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tp",  tienPhong);
                    cmd.Parameters.AddWithValue("@tdv", tienDV);
                    cmd.Parameters.AddWithValue("@vat", vat);
                    cmd.Parameters.AddWithValue("@tt",  tongTT);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 6. HOA_DON_CHI_TIET
                const string sqlHDCT = @"
                    INSERT INTO HOA_DON_CHI_TIET (MaHoaDon, MaDatPhong, MaPhong, SoDem)
                    VALUES (@mhd, @mdp, @mp, @sd)";
                await using (var cmd = new SqlCommand(sqlHDCT, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@mhd", maHoaDon);
                    cmd.Parameters.AddWithValue("@mdp", maDatPhong);
                    cmd.Parameters.AddWithValue("@mp",  req.MaPhong);
                    cmd.Parameters.AddWithValue("@sd",  soDem);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 7. DICH_VU_CHI_TIET (các dịch vụ được chọn)
                foreach (var dv in DichVuList.Where(d => d.IsSelected))
                {
                    const string sqlDVCT = @"
                        INSERT INTO DICH_VU_CHI_TIET
                               (MaHoaDon, MaDatPhong, MaPhong, MaDichVu,
                                SoLuong, DonGia, NgaySuDung)
                        VALUES (@mhd, @mdp, @mp, @mdv, 1, @dg, GETDATE())";
                    await using var cmd = new SqlCommand(sqlDVCT, conn, tx);
                    cmd.Parameters.AddWithValue("@mhd", maHoaDon);
                    cmd.Parameters.AddWithValue("@mdp", maDatPhong);
                    cmd.Parameters.AddWithValue("@mp",  req.MaPhong);
                    cmd.Parameters.AddWithValue("@mdv", dv.MaDichVu);
                    cmd.Parameters.AddWithValue("@dg",  dv.Gia);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // Refresh room list
                await LoadRoomsAsync();

                return maDatPhong;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────

        private async Task<string> UpsertKhachHangAsync(
            SqlConnection conn, SqlTransaction tx, DatPhongRequest req)
        {
            if (!string.IsNullOrEmpty(req.MaKhachHang)) return req.MaKhachHang;

            // Try find by CCCD
            if (!string.IsNullOrEmpty(req.CCCD))
            {
                const string findSql = "SELECT MaKhachHang FROM KHACH_HANG WHERE CCCD = @cccd";
                await using var find = new SqlCommand(findSql, conn, tx);
                find.Parameters.AddWithValue("@cccd", req.CCCD);
                var existing = await find.ExecuteScalarAsync();
                if (existing != null) return existing.ToString()!;
            }

            // Insert new guest
            string maKhach = GenerateId("KH");
            const string insertSql = @"
                INSERT INTO KHACH_HANG (MaKhachHang, TenKhachHang, DienThoai, CCCD)
                VALUES (@mk, @ten, @dt, @cccd)";
            await using var ins = new SqlCommand(insertSql, conn, tx);
            ins.Parameters.AddWithValue("@mk",   maKhach);
            ins.Parameters.AddWithValue("@ten",  req.TenKhachHang);
            ins.Parameters.AddWithValue("@dt",   (object?)req.DienThoai ?? DBNull.Value);
            ins.Parameters.AddWithValue("@cccd", (object?)req.CCCD ?? DBNull.Value);
            await ins.ExecuteNonQueryAsync();
            return maKhach;
        }

        public List<string> GetSelectedDichVuIds() =>
            DichVuList.Where(d => d.IsSelected).Select(d => d.MaDichVu).ToList();

        public void ResetServices()
        {
            foreach (var dv in DichVuList) dv.IsSelected = false;
            _tienPhong = _tienDichVu = _vat = _tongTien = 0;
        }

        private static string GenerateId(string prefix) =>
            prefix + DateTime.Now.ToString("yyMMddHHmmss");

        private static string FormatVnd(decimal amount) =>
            amount.ToString("N0", new CultureInfo("vi-VN")) + " ₫";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
