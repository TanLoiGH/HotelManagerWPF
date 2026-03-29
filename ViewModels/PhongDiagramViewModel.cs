using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class SoDoPhongViewModel : BaseViewModel
{
    // ── Collections ────────────────────────────────────────────────────────
    private List<PhongCardViewModel> _allPhongs = new();

    public ObservableCollection<PhongCardViewModel> FilteredPhongs { get; } = new();

    // ── Filter state ────────────────────────────────────────────────────────
    private string _selectedFilter = "Tất cả";
    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
                ApplyFilter();
        }
    }

    private int _selectedFloor = 0; // 0 = tất cả tầng
    public int SelectedFloor
    {
        get => _selectedFloor;
        set
        {
            if (SetProperty(ref _selectedFloor, value))
                ApplyFilter();
        }
    }

    // ── Loading state ───────────────────────────────────────────────────────
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ── Commands ────────────────────────────────────────────────────────────
    public RelayCommand FilterCommand { get; }
    public RelayCommand FloorCommand { get; }
    public RelayCommand RefreshCommand { get; }

    // ── Constructor ─────────────────────────────────────────────────────────
    public SoDoPhongViewModel()
    {
        FilterCommand = new RelayCommand(p => SelectedFilter = p?.ToString() ?? "Tất cả");
        FloorCommand = new RelayCommand(p => SelectedFloor = int.TryParse(p?.ToString(), out int f) ? f : 0);
        RefreshCommand = new RelayCommand(_ => _ = LoadAsync());

        _ = LoadAsync();
    }

    //  ─────────────────────────────────────────────────────
    // ── Load data từ DB ─────────────────────────────────────────────────────
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            using var db = new QuanLyKhachSanContext();

            // Bước 1: Query DB — chỉ lấy những gì SQL hiểu được
            var rawData = await db.Phongs
                .Include(p => p.MaLoaiPhongNavigation)
                .OrderBy(p => p.MaPhong)
                .Select(p => new
                {
                    p.MaPhong,
                    TenLoaiPhong = p.MaLoaiPhongNavigation.TenLoaiPhong ?? "",
                    MaTrangThai = p.MaTrangThaiPhong ?? "PTT01",
                })
                .ToListAsync(); // ← Materialize về bộ nhớ trước

            // Bước 2: Map sang ViewModel — tính Tang bằng C# thuần
            _allPhongs = rawData.Select(p => new PhongCardViewModel
            {
                MaPhong = p.MaPhong,
                TenLoaiPhong = p.TenLoaiPhong,
                MaTrangThaiPhong = p.MaTrangThai,
                Tang = ExtractFloor(p.MaPhong),
            }).ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải phòng: {ex.Message}", "Lỗi");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Helper tách số tầng từ mã phòng: "P101" → 1, "P302" → 3
    private static int ExtractFloor(string maPhong)
    {
        // MaPhong format: "P101" → index 1 là số tầng
        if (maPhong.Length >= 2 && int.TryParse(maPhong[1].ToString(), out int tang))
            return tang;
        return 0;
    }

    // ── Apply filter ─────────────────────────────────────────────────────────
    private void ApplyFilter()
    {
        var query = _allPhongs.Where(p =>
            (SelectedFloor <= 0 || p.Tang == SelectedFloor) &&
            (SelectedFilter == "Tất cả" ||
             (SelectedFilter == "Phòng trống" && p.MaTrangThaiPhong == "PTT01") ||
             (SelectedFilter == "Đang có khách" && p.MaTrangThaiPhong == "PTT02") ||
             (SelectedFilter == "Dọn dẹp" && p.MaTrangThaiPhong == "PTT03") ||
             (SelectedFilter == "Đã đặt trước" && p.MaTrangThaiPhong == "PTT05"))
        );

        FilteredPhongs.Clear();

        foreach (var p in query)
            FilteredPhongs.Add(p);
    }


}


