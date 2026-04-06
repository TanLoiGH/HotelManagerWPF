using System;
using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class BaoCaoPage : Page
{
    private readonly BaoCaoPageViewModel _viewModel;
    private readonly QuanLyKhachSanContext _db;

    public BaoCaoPage()
    {
        InitializeComponent();

        _db = new QuanLyKhachSanContext();
        var reportService = new ReportService(_db);

        _viewModel = new BaoCaoPageViewModel(reportService);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.LoadReportsAsync();
        Unloaded += (_, _) => _db.Dispose();
    }
}
