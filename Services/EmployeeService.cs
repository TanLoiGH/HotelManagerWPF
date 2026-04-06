using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public record ThongTinTaiKhoanNhanVien(
    string TenDangNhap,
    string MaQuyen,
    string TenQuyen);

public class EmployeeService
{
    private readonly QuanLyKhachSanContext _db;

    public EmployeeService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<List<NhanVien>> GetEmployeesAsync()
    {
        return await _db.NhanViens
            .AsNoTracking()
            .Include(nv => nv.MaTrangThaiNavigation)
            .Include(nv => nv.TaiKhoans)
                .ThenInclude(tk => tk.MaQuyenNavigation)
            .ToListAsync();
    }

    public async Task<NhanVien?> GetEmployeeWithAccountAsync(string maNhanVien)
    {
        return await _db.NhanViens
            .Include(n => n.TaiKhoans)
            .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
    }

    public async Task<List<TrangThaiNhanVien>> GetEmployeeStatusesAsync()
    {
        return await _db.TrangThaiNhanViens
            .AsNoTracking()
            .OrderBy(t => t.MaTrangThai)
            .ToListAsync();
    }

    public async Task<List<PhanQuyen>> GetRolesAsync()
    {
        return await _db.PhanQuyens
            .AsNoTracking()
            .OrderBy(q => q.MaQuyen)
            .ToListAsync();
    }

    public async Task<NhanVien?> LayNhanVienVaTaiKhoanAsync(string maNhanVien)
    {
        return await _db.NhanViens
            .AsNoTracking()
            .Include(n => n.TaiKhoans)
            .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
    }

    public async Task<NhanVien?> LayNhanVienAsync(string maNhanVien)
    {
        return await _db.NhanViens
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
    }

    public async Task<ThongTinTaiKhoanNhanVien?> LayThongTinTaiKhoanAsync(string maNhanVien, string? tenDangNhap)
    {
        var q = _db.TaiKhoans
            .AsNoTracking()
            .Include(t => t.MaQuyenNavigation)
            .Where(t => t.MaNhanVien == maNhanVien && t.IsActive == true);

        if (!string.IsNullOrWhiteSpace(tenDangNhap))
            q = q.Where(t => t.TenDangNhap == tenDangNhap);

        return await q
            .Select(t => new ThongTinTaiKhoanNhanVien(
                t.TenDangNhap,
                t.MaQuyen,
                t.MaQuyenNavigation != null ? t.MaQuyenNavigation.TenQuyen : t.MaQuyen))
            .FirstOrDefaultAsync();
    }

    public async Task CapNhatThongTinCaNhanAsync(
        string maNhanVien,
        string hoTen,
        string soDienThoai,
        string? email,
        string? diaChi)
    {
        var nv = await _db.NhanViens.FirstOrDefaultAsync(x => x.MaNhanVien == maNhanVien);
        if (nv == null)
            throw new InvalidOperationException("Không tìm thấy nhân viên.");

        bool trungSdt = await _db.NhanViens
            .AnyAsync(x => x.MaNhanVien != maNhanVien && x.DienThoai == soDienThoai);
        if (trungSdt)
            throw new InvalidOperationException("Số điện thoại đã được sử dụng bởi nhân viên khác.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            bool trungEmail = await _db.NhanViens
                .AnyAsync(x => x.MaNhanVien != maNhanVien && x.Email == email);
            if (trungEmail)
                throw new InvalidOperationException("Email đã được sử dụng bởi nhân viên khác.");
        }

        nv.TenNhanVien = hoTen;
        nv.DienThoai = soDienThoai;
        nv.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        nv.DiaChi = string.IsNullOrWhiteSpace(diaChi) ? null : diaChi;

        await _db.SaveChangesAsync();
    }

    public async Task VoHieuHoaNhanVienAsync(string maNhanVien, string maTrangThaiNghiViec = "TT02")
    {
        var nv = await _db.NhanViens
            .Include(n => n.TaiKhoans)
            .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

        if (nv == null) return;

        nv.MaTrangThai = maTrangThaiNghiViec;
        foreach (var tk in nv.TaiKhoans)
            tk.IsActive = false;

        await _db.SaveChangesAsync();
    }

    public async Task LuuNhanVienVaTaiKhoanAsync(
        bool isNew,
        string? maNhanVien,
        string tenNhanVien,
        string? chucVu,
        string? dienThoai,
        string? email,
        string? cccd,
        string maTrangThai,
        string tenDangNhap,
        string matKhau,
        string? maQuyen,
        bool isTkActive)
    {
        bool userTypedAccount = !string.IsNullOrWhiteSpace(tenDangNhap) || !string.IsNullOrWhiteSpace(matKhau);

        void EnsureAccountInputsValid(bool requirePassword)
        {
            if (!userTypedAccount && !requirePassword) return;

            if (string.IsNullOrWhiteSpace(tenDangNhap))
                throw new InvalidOperationException("Vui lòng nhập tên đăng nhập.");

            if (string.IsNullOrWhiteSpace(maQuyen))
                throw new InvalidOperationException("Vui lòng chọn quyền.");

            if (requirePassword && string.IsNullOrWhiteSpace(matKhau))
                throw new InvalidOperationException("Vui lòng nhập mật khẩu.");
        }

        if (isNew)
        {
            if (userTypedAccount)
            {
                EnsureAccountInputsValid(requirePassword: true);

                bool trung = await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap);
                if (trung)
                    throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            }

            var lastMaNV = await _db.NhanViens
                .OrderByDescending(n => n.MaNhanVien)
                .Select(n => n.MaNhanVien)
                .FirstOrDefaultAsync();

            var nv = new NhanVien
            {
                MaNhanVien = MaHelper.Next("NV", lastMaNV),
                TenNhanVien = tenNhanVien,
                ChucVu = chucVu,
                DienThoai = dienThoai,
                Email = email,
                Cccd = cccd,
                MaTrangThai = maTrangThai,
                NgayVaoLam = DateOnly.FromDateTime(DateTime.Today),
            };

            _db.NhanViens.Add(nv);

            if (userTypedAccount && !string.IsNullOrWhiteSpace(maQuyen))
            {
                _db.TaiKhoans.Add(new TaiKhoan
                {
                    MaNhanVien = nv.MaNhanVien,
                    MaQuyen = maQuyen,
                    TenDangNhap = tenDangNhap,
                    MatKhau = PasswordHasher.Hash(matKhau),
                    IsActive = isTkActive,
                });
            }

            await _db.SaveChangesAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(maNhanVien))
            throw new InvalidOperationException("Không xác định được mã nhân viên.");

        var existing = await _db.NhanViens
            .Include(n => n.TaiKhoans)
            .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

        if (existing == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên.");

        existing.TenNhanVien = tenNhanVien;
        existing.ChucVu = chucVu;
        existing.DienThoai = dienThoai;
        existing.Email = email;
        existing.Cccd = cccd;
        existing.MaTrangThai = maTrangThai;

        var tkActive = existing.TaiKhoans.FirstOrDefault(t => t.IsActive == true);

        bool wantsAccountUpdate =
            userTypedAccount ||
            (tkActive != null && !string.IsNullOrWhiteSpace(maQuyen) && tkActive.MaQuyen != maQuyen) ||
            (tkActive != null && (tkActive.IsActive ?? false) != isTkActive);

        if (wantsAccountUpdate)
        {
            EnsureAccountInputsValid(requirePassword: tkActive == null);

            bool trung = await _db.TaiKhoans.AnyAsync(t =>
                t.TenDangNhap == tenDangNhap && t.MaNhanVien != existing.MaNhanVien);
            if (trung)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

            if (tkActive == null)
            {
                _db.TaiKhoans.Add(new TaiKhoan
                {
                    MaNhanVien = existing.MaNhanVien,
                    MaQuyen = maQuyen!,
                    TenDangNhap = tenDangNhap,
                    MatKhau = PasswordHasher.Hash(matKhau),
                    IsActive = isTkActive,
                });
            }
            else
            {
                if (!string.Equals(tkActive.MaQuyen, maQuyen, StringComparison.Ordinal))
                {
                    string keepPassword = string.IsNullOrWhiteSpace(matKhau)
                        ? (tkActive.MatKhau ?? "")
                        : PasswordHasher.Hash(matKhau);

                    _db.TaiKhoans.RemoveRange(existing.TaiKhoans);
                    _db.TaiKhoans.Add(new TaiKhoan
                    {
                        MaNhanVien = existing.MaNhanVien,
                        MaQuyen = maQuyen!,
                        TenDangNhap = tenDangNhap,
                        MatKhau = keepPassword,
                        IsActive = isTkActive,
                    });
                }
                else
                {
                    tkActive.TenDangNhap = tenDangNhap;
                    if (!string.IsNullOrWhiteSpace(matKhau))
                        tkActive.MatKhau = PasswordHasher.Hash(matKhau);
                    tkActive.IsActive = isTkActive;

                    foreach (var other in existing.TaiKhoans.Where(t => t != tkActive))
                        other.IsActive = false;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveEmployeeAsync(NhanVien nv, string? tenDangNhap, string? matKhau, string? maQuyen, bool isTkActive)
    {
        bool isNew = string.IsNullOrEmpty(nv.MaNhanVien);

        if (isNew)
        {
            var lastMa = await _db.NhanViens.OrderByDescending(n => n.MaNhanVien).Select(n => n.MaNhanVien).FirstOrDefaultAsync();
            nv.MaNhanVien = MaHelper.Next("NV", lastMa);
            nv.NgayVaoLam = DateOnly.FromDateTime(DateTime.Today);
            _db.NhanViens.Add(nv);

            if (!string.IsNullOrEmpty(tenDangNhap) && !string.IsNullOrEmpty(maQuyen))
            {
                if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap))
                    throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

                _db.TaiKhoans.Add(new TaiKhoan
                {
                    MaNhanVien = nv.MaNhanVien,
                    MaQuyen = maQuyen,
                    TenDangNhap = tenDangNhap,
                    MatKhau = PasswordHasher.Hash(matKhau ?? ""),
                    IsActive = isTkActive,
                });
            }
        }
        else
        {
            var existing = await _db.NhanViens.Include(n => n.TaiKhoans).FirstOrDefaultAsync(n => n.MaNhanVien == nv.MaNhanVien);
            if (existing == null) throw new KeyNotFoundException("Không tìm thấy nhân viên.");

            existing.TenNhanVien = nv.TenNhanVien;
            existing.ChucVu = nv.ChucVu;
            existing.DienThoai = nv.DienThoai;
            existing.Email = nv.Email;
            existing.Cccd = nv.Cccd;
            existing.MaTrangThai = nv.MaTrangThai;

            if (!string.IsNullOrEmpty(tenDangNhap))
            {
                if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap && t.MaNhanVien != nv.MaNhanVien))
                    throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

                var tk = existing.TaiKhoans.FirstOrDefault(t => t.IsActive == true) ?? existing.TaiKhoans.FirstOrDefault();

                if (tk == null)
                {
                    if (string.IsNullOrEmpty(maQuyen)) throw new InvalidOperationException("Vui lòng chọn quyền.");
                    _db.TaiKhoans.Add(new TaiKhoan
                    {
                        MaNhanVien = nv.MaNhanVien,
                        MaQuyen = maQuyen,
                        TenDangNhap = tenDangNhap,
                        MatKhau = PasswordHasher.Hash(matKhau ?? ""),
                        IsActive = isTkActive,
                    });
                }
                else
                {
                    if (tk.MaQuyen != maQuyen && !string.IsNullOrEmpty(maQuyen))
                    {
                        // Remove old and add new if MaQuyen (PK) changed
                        string keepPass = string.IsNullOrEmpty(matKhau) ? (tk.MatKhau ?? "") : PasswordHasher.Hash(matKhau);
                        _db.TaiKhoans.Remove(tk);
                        _db.TaiKhoans.Add(new TaiKhoan
                        {
                            MaNhanVien = nv.MaNhanVien,
                            MaQuyen = maQuyen,
                            TenDangNhap = tenDangNhap,
                            MatKhau = keepPass,
                            IsActive = isTkActive,
                        });
                    }
                    else
                    {
                        tk.TenDangNhap = tenDangNhap;
                        if (!string.IsNullOrEmpty(matKhau)) tk.MatKhau = PasswordHasher.Hash(matKhau);
                        tk.IsActive = isTkActive;
                    }
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteEmployeeAsync(string maNhanVien)
    {
        var nv = await _db.NhanViens.Include(n => n.TaiKhoans).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
        if (nv == null) return;

        _db.TaiKhoans.RemoveRange(nv.TaiKhoans);
        _db.NhanViens.Remove(nv);
        await _db.SaveChangesAsync();
    }
}
