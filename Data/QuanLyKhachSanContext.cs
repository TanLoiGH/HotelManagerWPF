using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuanLyKhachSan_PhamTanLoi.Dtos;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.Data;

public partial class QuanLyKhachSanContext : DbContext
{
    public QuanLyKhachSanContext()
    {
    }

    public QuanLyKhachSanContext(DbContextOptions<QuanLyKhachSanContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiPhi> ChiPhis { get; set; } = null!;

    public virtual DbSet<DatPhong> DatPhongs { get; set; } = null!;

    public virtual DbSet<DatPhongChiTiet> DatPhongChiTiets { get; set; } = null!;

    public virtual DbSet<DichVu> DichVus { get; set; } = null!;

    public virtual DbSet<DichVuChiTiet> DichVuChiTiets { get; set; } = null!;

    public virtual DbSet<HoaDon> HoaDons { get; set; } = null!;

    public virtual DbSet<HoaDonChiTiet> HoaDonChiTiets { get; set; } = null!;

    public virtual DbSet<KhachHang> KhachHangs { get; set; } = null!;

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; } = null!;

    public virtual DbSet<LoaiChiPhi> LoaiChiPhis { get; set; } = null!;

    public virtual DbSet<LoaiKhach> LoaiKhaches { get; set; } = null!;

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; } = null!;

    public virtual DbSet<NhaCungCap> NhaCungCaps { get; set; } = null!;

    public virtual DbSet<NhanVien> NhanViens { get; set; } = null!;

    public virtual DbSet<PhanQuyen> PhanQuyens { get; set; } = null!;

    public virtual DbSet<Phong> Phongs { get; set; } = null!;

    public virtual DbSet<PhongTrangThai> PhongTrangThais { get; set; } = null!;

    public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; } = null!;

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; } = null!;

    public virtual DbSet<ThanhToan> ThanhToans { get; set; } = null!;

    public virtual DbSet<TienNghi> TienNghis { get; set; } = null!;

    public virtual DbSet<TienNghiDanhMuc> TienNghiDanhMucs { get; set; } = null!;

    public virtual DbSet<TienNghiPhong> TienNghiPhongs { get; set; } = null!;

    public virtual DbSet<TienNghiTrangThai> TienNghiTrangThais { get; set; } = null!;

    public virtual DbSet<TrangThaiNhanVien> TrangThaiNhanViens { get; set; } = null!;

    public DbSet<VwDoanhThuThang> VwDoanhThuThangs { get; set; } = null!;
    public DbSet<VwCoCauChiPhi> VwCoCauChiPhis { get; set; } = null!;
    public DbSet<VwTopDichVu> VwTopDichVus { get; set; } = null!;




    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("Default")
                     ?? "Server=LOI_WINDOW\\SQLEXPRESS;Database=QuanLyKhachSan;Trusted_Connection=True;TrustServerCertificate=True;";

            optionsBuilder.UseSqlServer(cs);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VwDoanhThuThang>().HasNoKey().ToView("VW_DOANH_THU_THANG");
        modelBuilder.Entity<VwCoCauChiPhi>().HasNoKey().ToView("VW_CO_CAU_CHI_PHI");
        modelBuilder.Entity<VwTopDichVu>().HasNoKey().ToView("VW_TOP_DICH_VU");




        modelBuilder.Entity<ChiPhi>(entity =>
        {
            entity.HasKey(e => e.MaChiPhi).HasName("PK_CP");

            entity.ToTable("CHI_PHI");

            entity.Property(e => e.MaChiPhi)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.MaLoaiCp)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaLoaiCP");
            entity.Property(e => e.MaNcc)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNCC");
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayChiPhi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenChiPhi).HasMaxLength(200);

            entity.HasOne(d => d.MaLoaiCpNavigation).WithMany(p => p.ChiPhis)
                .HasForeignKey(d => d.MaLoaiCp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CP_LOAI");

            entity.HasOne(d => d.MaNccNavigation).WithMany(p => p.ChiPhis)
                .HasForeignKey(d => d.MaNcc)
                .HasConstraintName("FK_CP_NCC");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.ChiPhis)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_CP_NV");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.ChiPhis)
                .HasForeignKey(d => d.MaPhong)
                .HasConstraintName("FK_CP_PHONG");
        });

        modelBuilder.Entity<DatPhong>(entity =>
        {
            entity.HasKey(e => e.MaDatPhong);

            entity.ToTable("DAT_PHONG");

            entity.Property(e => e.MaDatPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaKhachHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayDat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ nhận phòng");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.DatPhongs)
                .HasForeignKey(d => d.MaKhachHang)
                .HasConstraintName("FK_DP_KH");
        });

        modelBuilder.Entity<DatPhongChiTiet>(entity =>
        {
            entity.HasKey(e => new { e.MaDatPhong, e.MaPhong }).HasName("PK_DPCT");

            entity.ToTable("DAT_PHONG_CHI_TIET");

            entity.Property(e => e.MaDatPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayNhan).HasColumnType("datetime");
            entity.Property(e => e.NgayTra).HasColumnType("datetime");

            entity.HasOne(d => d.MaDatPhongNavigation).WithMany(p => p.DatPhongChiTiets)
                .HasForeignKey(d => d.MaDatPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DPCT_DP");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.DatPhongChiTiets)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_DPCT_NV");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.DatPhongChiTiets)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DPCT_PHONG");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDichVu).HasName("PK_DV");

            entity.ToTable("DICH_VU");

            entity.Property(e => e.MaDichVu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DonViTinh).HasMaxLength(30);
            entity.Property(e => e.Gia)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TenDichVu).HasMaxLength(100);
        });

        modelBuilder.Entity<DichVuChiTiet>(entity =>
        {
            entity.HasKey(e => new { e.MaHoaDon, e.MaDatPhong, e.MaPhong, e.MaDichVu }).HasName("PK_DVCT");

            entity.ToTable("DICH_VU_CHI_TIET");

            entity.HasIndex(e => e.MaDatPhong, "IX_DVCT_DatPhong");

            entity.HasIndex(e => e.MaHoaDon, "IX_DVCT_HoaDon");

            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaDatPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaDichVu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DonGia).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.NgaySuDung)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoLuong).HasDefaultValue(1);

            entity.HasOne(d => d.MaDichVuNavigation).WithMany(p => p.DichVuChiTiets)
                .HasForeignKey(d => d.MaDichVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DVCT_DV");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.DichVuChiTiets)
                .HasForeignKey(d => d.MaHoaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DVCT_HD");

            entity.HasOne(d => d.DatPhongChiTiet).WithMany(p => p.DichVuChiTiets)
                .HasForeignKey(d => new { d.MaDatPhong, d.MaPhong })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DVCT_DPCT");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHoaDon).HasName("PK_HD");

            entity.ToTable("HOA_DON");

            entity.HasIndex(e => e.MaDatPhong, "UIX_HD_DatPhong_Active")
                .IsUnique()
                .HasFilter("([TrangThai]<>N'Đã hủy')");

            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaDatPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TienDichVu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TienPhong)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongThanhToan)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("Chưa thanh toán");
            entity.Property(e => e.Vat)
                .HasDefaultValue(10m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("VAT");

            entity.HasOne(d => d.MaDatPhongNavigation).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.MaDatPhong)
                .HasConstraintName("FK_HD_DP");

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaKhuyenMai)
                .HasConstraintName("FK_HD_KM");

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaNhanVien)
                .HasConstraintName("FK_HD_NV");
        });

        modelBuilder.Entity<HoaDonChiTiet>(entity =>
        {
            entity.HasKey(e => new { e.MaHoaDon, e.MaDatPhong, e.MaPhong }).HasName("PK_HDCT");

            entity.ToTable("HOA_DON_CHI_TIET");

            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaDatPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.HoaDonChiTiets)
                .HasForeignKey(d => d.MaHoaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDCT_HD");

            entity.HasOne(d => d.DatPhongChiTiet).WithMany(p => p.HoaDonChiTiets)
                .HasForeignKey(d => new { d.MaDatPhong, d.MaPhong })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDCT_DPCT");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKhachHang).HasName("PK_KH");

            entity.ToTable("KHACH_HANG");

            entity.HasIndex(e => e.Cccd, "UQ_KH_CCCD").IsUnique();

            entity.Property(e => e.MaKhachHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiKhach)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("LK001");
            entity.Property(e => e.TenKhachHang).HasMaxLength(100);
            entity.Property(e => e.TongTichLuy)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaLoaiKhachNavigation).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.MaLoaiKhach)
                .HasConstraintName("FK_KH_LOAI");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaKhuyenMai).HasName("PK_KM");

            entity.ToTable("KHUYEN_MAI");

            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.GiaTriKm)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GiaTriKM");
            entity.Property(e => e.GiaTriToiThieu)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LoaiKhuyenMai).HasMaxLength(50);
            entity.Property(e => e.MaLoaiKhach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NgayBatDau).HasColumnType("datetime");
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.TenKhuyenMai).HasMaxLength(100);

            entity.HasOne(d => d.MaLoaiKhachNavigation).WithMany(p => p.KhuyenMais)
                .HasForeignKey(d => d.MaLoaiKhach)
                .HasConstraintName("FK_KM_LOAIKHACH");
        });

        modelBuilder.Entity<LoaiChiPhi>(entity =>
        {
            entity.HasKey(e => e.MaLoaiCp).HasName("PK_LOAICP");

            entity.ToTable("LOAI_CHI_PHI");

            entity.Property(e => e.MaLoaiCp)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaLoaiCP");
            entity.Property(e => e.TenLoaiCp)
                .HasMaxLength(50)
                .HasColumnName("TenLoaiCP");
        });

        modelBuilder.Entity<LoaiKhach>(entity =>
        {
            entity.HasKey(e => e.MaLoaiKhach);

            entity.ToTable("LOAI_KHACH");

            entity.Property(e => e.MaLoaiKhach)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NguongTichLuy)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenLoaiKhach).HasMaxLength(50);
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLoaiPhong);

            entity.ToTable("LOAI_PHONG");

            entity.Property(e => e.MaLoaiPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.GiaPhong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TenLoaiPhong).HasMaxLength(50);
        });

        modelBuilder.Entity<NhaCungCap>(entity =>
        {
            entity.HasKey(e => e.MaNcc).HasName("PK_NCC");

            entity.ToTable("NHA_CUNG_CAP");

            entity.Property(e => e.MaNcc)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNCC");
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.NguoiLienHe).HasMaxLength(100);
            entity.Property(e => e.TenNcc)
                .HasMaxLength(100)
                .HasColumnName("TenNCC");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNhanVien);

            entity.ToTable("NHAN_VIEN");

            entity.HasIndex(e => e.Cccd, "UQ_NV_CCCD").IsUnique();

            entity.HasIndex(e => e.Email, "UQ_NV_EMAIL").IsUnique();

            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.DienThoai)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("TT01");
            entity.Property(e => e.NgayVaoLam).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TenNhanVien).HasMaxLength(100);

            entity.HasOne(d => d.MaTrangThaiNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaTrangThai)
                .HasConstraintName("FK_NV_TRANGTHAI");
        });

        modelBuilder.Entity<PhanQuyen>(entity =>
        {
            entity.HasKey(e => e.MaQuyen);

            entity.ToTable("PHAN_QUYEN");

            entity.Property(e => e.MaQuyen)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenQuyen).HasMaxLength(50);
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPhong);

            entity.ToTable("PHONG");

            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaLoaiPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaTrangThaiPhong)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("PTT01");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLoaiPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PHONG_LOAI");

            entity.HasOne(d => d.MaTrangThaiPhongNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaTrangThaiPhong)
                .HasConstraintName("FK_PHONG_TT");
        });

        modelBuilder.Entity<PhongTrangThai>(entity =>
        {
            entity.HasKey(e => e.MaTrangThaiPhong).HasName("PK_TT_PHONG");

            entity.ToTable("PHONG_TRANG_THAI");

            entity.Property(e => e.MaTrangThaiPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<PhuongThucThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaPttt).HasName("PK_PTTT");

            entity.ToTable("PHUONG_THUC_THANH_TOAN");

            entity.Property(e => e.MaPttt)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaPTTT");
            entity.Property(e => e.TenPhuongThuc).HasMaxLength(50);
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => new { e.MaNhanVien, e.MaQuyen });

            entity.ToTable("TAI_KHOAN");

            entity.HasIndex(e => e.TenDangNhap, "UQ_TK_LOGIN").IsUnique();

            entity.Property(e => e.MaNhanVien)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaQuyen)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MatKhau)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNhanVienNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaNhanVien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TK_NV");

            entity.HasOne(d => d.MaQuyenNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaQuyen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TK_QUYEN");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaThanhToan).HasName("PK_TT");

            entity.ToTable("THANH_TOAN");

            entity.Property(e => e.MaThanhToan)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LoaiGiaoDich).HasMaxLength(50);
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaPttt)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaPTTT");
            entity.Property(e => e.NgayThanhToan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NguoiThu)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NoiDung).HasMaxLength(255);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.MaHoaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TT_HD");

            entity.HasOne(d => d.MaPtttNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.MaPttt)
                .HasConstraintName("FK_TT_PTTT");

            entity.HasOne(d => d.NguoiThuNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.NguoiThu)
                .HasConstraintName("FK_TT_NV");
        });

        modelBuilder.Entity<TienNghi>(entity =>
        {
            entity.HasKey(e => e.MaTienNghi);

            entity.ToTable("TIEN_NGHI");

            entity.Property(e => e.MaTienNghi)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DonViTinh).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaNcc)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNCC");
            entity.Property(e => e.TenTienNghi).HasMaxLength(100);
            entity.Property(e => e.TongSoLuong).HasDefaultValue(0);

            entity.HasOne(d => d.MaNccNavigation).WithMany(p => p.TienNghis)
                .HasForeignKey(d => d.MaNcc)
                .HasConstraintName("FK_TIENNGHI_NCC");

            entity.HasOne(d => d.MaDanhMucNavigation).WithMany(p => p.TienNghis)
                .HasForeignKey(d => d.MaDanhMuc)
                .HasConstraintName("FK_TIEN_NGHI_TIEN_NGHI_DANH_MUC");
        });


        modelBuilder.Entity<TienNghiDanhMuc>(entity =>
        {
            entity.HasKey(e => e.MaDanhMuc).HasName("PK_TIEN_NGHI_DANH_MUC");
            entity.ToTable("TIEN_NGHI_DANH_MUC");
            entity.Property(e => e.MaDanhMuc)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenDanhMuc).HasMaxLength(50);
        });


        modelBuilder.Entity<TienNghiPhong>(entity =>
        {
            entity.HasKey(e => new { e.MaPhong, e.MaTienNghi }).HasName("PK_TNP");

            entity.ToTable("TIEN_NGHI_PHONG");

            entity.Property(e => e.MaPhong)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaTienNghi)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.TienNghiPhongs)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TNP_P");

            entity.HasOne(d => d.MaTienNghiNavigation).WithMany(p => p.TienNghiPhongs)
                .HasForeignKey(d => d.MaTienNghi)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TNP_TN");

            entity.HasOne(d => d.MaTrangThaiNavigation).WithMany(p => p.TienNghiPhongs)
                .HasForeignKey(d => d.MaTrangThai)
                .HasConstraintName("FK_TNP_TT");
        });

        modelBuilder.Entity<TienNghiTrangThai>(entity =>
        {
            entity.HasKey(e => e.MaTrangThai).HasName("PK_TNTT");

            entity.ToTable("TIEN_NGHI_TRANG_THAI");

            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<TrangThaiNhanVien>(entity =>
        {
            entity.HasKey(e => e.MaTrangThai).HasName("PK_TRANG_THAI_NV");

            entity.ToTable("TRANG_THAI_NHAN_VIEN");

            entity.Property(e => e.MaTrangThai)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenTrangThai).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}


