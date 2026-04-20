using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Reports.DTO;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class BaoCaoPageViewModel : BaseViewModel
{
    private readonly ReportService _reportService;
    private int _selectedSourceIndex = 0; // 0: View, 1: SP
    private DateTime? _tuNgay = new DateTime(TimeHelper.GetVietnamTime().Year, TimeHelper.GetVietnamTime().Month, 1);
    private DateTime? _denNgay = DateTime.Today;
    private ReportItem? _selectedReport;
    private ObservableCollection<ReportItem> _availableReports = new();
    private DataTable? _reportData;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _hasData;
    private bool _noData;

    public BaoCaoPageViewModel(ReportService reportService)
    {
        _reportService = reportService;

        LoadReportsCommand = new RelayCommand(async _ => await LoadReportsAsync());
        RunReportCommand = new RelayCommand(async _ => await RunReportAsync(), _ => CanRunReport());
        ExportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => HasData);
    }

    // Properties
    public int SelectedSourceIndex
    {
        get => _selectedSourceIndex;
        set
        {
            if (SetProperty(ref _selectedSourceIndex, value))
            {
                OnPropertyChanged(nameof(IsDateFilterEnabled));
                _ = LoadReportsAsync();
            }
        }
    }

    public bool IsDateFilterEnabled => SelectedSourceIndex == 1;

    public DateTime? TuNgay { get => _tuNgay; set => SetProperty(ref _tuNgay, value); }
    public DateTime? DenNgay { get => _denNgay; set => SetProperty(ref _denNgay, value); }

    public ReportItem? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (SetProperty(ref _selectedReport, value))
            {
                (RunReportCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ReportItem> AvailableReports => _availableReports;

    public DataTable? ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                HasData = value != null && value.Rows.Count > 0;
                NoData = value != null && value.Rows.Count == 0;
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                (RunReportCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
    public string? ErrorMessage { get => _errorMessage; set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData
    {
        get => _hasData;
        set
        {
            if (SetProperty(ref _hasData, value))
            {
                (ExportCsvCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
    public bool NoData { get => _noData; set => SetProperty(ref _noData, value); }

    // Commands
    public ICommand LoadReportsCommand { get; }
    public ICommand RunReportCommand { get; }
    public ICommand ExportCsvCommand { get; }

    // Methods
    public async Task LoadReportsAsync()
    {
        ErrorMessage = null;
        try
        {
            var reports = await _reportService.LoadDbObjectsAsync(IsDateFilterEnabled);
            AvailableReports.Clear();
            foreach (var r in reports) AvailableReports.Add(r);
            if (AvailableReports.Count > 0) SelectedReport = AvailableReports[0];
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ErrorMessage = $"Không tải được danh sách báo cáo: {ex.Message}";
        }
    }

    private bool CanRunReport() => SelectedReport != null && !IsLoading;

    public async Task RunReportAsync()
    {
        if (SelectedReport == null) return;

        IsLoading = true;
        ErrorMessage = null;
        ReportData = null;

        try
        {
            var data = await _reportService.RunReportAsync(SelectedReport.FullName, !IsDateFilterEnabled, TuNgay, DenNgay);
            ReportData = data;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi", ex);
            ErrorMessage = $"Lỗi chạy báo cáo: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ExportCsv()
    {
        if (ReportData == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv",
            FileName = $"BaoCao_{SelectedReport?.DisplayName ?? "Data"}_{TimeHelper.GetVietnamTime():yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            if (dialog.FileName.EndsWith(".xlsx"))
            {
                ExportHelper.ExportToExcel(ReportData, dialog.FileName, SelectedReport?.DisplayName ?? "Report");
            }
            else
            {
                ExportHelper.ExportToCsv(ReportData, dialog.FileName);
            }
            MessageBox.Show("Xuất file thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
