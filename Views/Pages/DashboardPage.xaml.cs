using System;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;
    private readonly QuanLyKhachSanContext _db = new();

    public DashboardPage()
    {
        InitializeComponent();

        var dashboardService = new DashboardService(_db);

        _viewModel = new DashboardViewModel(dashboardService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadDataAsync();
        Unloaded += (_, _) => _db.Dispose();
    }
}
