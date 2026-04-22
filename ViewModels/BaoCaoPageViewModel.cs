using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Reports.DTO;
using QuanLyKhachSan_PhamTanLoi.Services;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class BaoCaoPageViewModel : BaseViewModel
{
    private readonly BaoCaoService _baoCaoService;
    private int _selectedSourceIndex = 0; // 0: View, 1: SP

    // Senior Fix: Dùng TimeHelper cho đồng nhất toàn hệ thống
    private DateTime? _tuNgay = new DateTime(TimeHelper.GetVietnamTime().Year, TimeHelper.GetVietnamTime().Month, 1);
    private DateTime? _denNgay = TimeHelper.GetVietnamTime().Date;

    private ReportItem? _selectedReport;
    private ObservableCollection<ReportItem> _availableReports = new();
    private DataTable? _reportData;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _hasData;
    private bool _noData;

    public BaoCaoPageViewModel(BaoCaoService baoCaoService)
    {
        _baoCaoService = baoCaoService;

        // Senior Fix: Tận dụng AsyncRelayCommand xịn sò vừa tạo để chống Double-click và Crash
        LoadReportsCommand = new AsyncRelayCommand(async _ => await LoadReportsAsync());
        RunReportCommand = new AsyncRelayCommand(async _ => await RunReportAsync(), _ => CanRunReport());
        ExportCsvCommand = new AsyncRelayCommand(async _ => await ExportCsvAsync(), _ => HasData);

        // Nạp danh sách báo cáo lần đầu tiên
        _ = LoadReportsAsync();
    }

    #region PROPERTIES

    public int SelectedSourceIndex
    {
        get => _selectedSourceIndex;
        set
        {
            if (SetProperty(ref _selectedSourceIndex, value))
            {
                OnPropertyChanged(nameof(IsDateFilterEnabled));
                // Tải lại danh sách báo cáo (View hoặc SP) theo Source mới
                _ = LoadReportsAsync();
            }
        }
    }

    public bool IsDateFilterEnabled => SelectedSourceIndex == 1;

    public DateTime? TuNgay
    {
        get => _tuNgay;
        set => SetProperty(ref _tuNgay, value);
    }

    public DateTime? DenNgay
    {
        get => _denNgay;
        set => SetProperty(ref _denNgay, value);
    }

    public ReportItem? SelectedReport
    {
        get => _selectedReport;
        set
        {
            if (SetProperty(ref _selectedReport, value))
            {
                // Khi đổi loại báo cáo, xóa dữ liệu cũ đi để tránh nhầm lẫn
                ReportData = null;
                (RunReportCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
                (ExportCsvCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
                (RunReportCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public bool NoData
    {
        get => _noData;
        private set => SetProperty(ref _noData, value);
    }

    #endregion

    #region COMMANDS

    public ICommand LoadReportsCommand { get; }
    public ICommand RunReportCommand { get; }
    public ICommand ExportCsvCommand { get; }

    #endregion

    #region LOGIC METHODS

    public async Task LoadReportsAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            var reports = await _baoCaoService.LoadDbObjectsAsync(IsDateFilterEnabled);
            AvailableReports.Clear();
            foreach (var r in reports) AvailableReports.Add(r);

            if (AvailableReports.Count > 0)
                SelectedReport = AvailableReports[0];
            else
                SelectedReport = null;
        }
        catch (Exception ex)
        {
            Logger.LogError("Lỗi tải danh mục báo cáo", ex);
            ErrorMessage = $"Không tải được danh sách báo cáo: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRunReport() => SelectedReport != null && !IsLoading;

    public async Task RunReportAsync()
    {
        if (SelectedReport == null) return;

        // Senior Fix: Validation khoảng ngày
        if (IsDateFilterEnabled && TuNgay > DenNgay)
        {
            MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Thông báo", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        ReportData = null;

        try
        {
            var data = await _baoCaoService.RunReportAsync(SelectedReport.FullName, !IsDateFilterEnabled, TuNgay,
                DenNgay);
            ReportData = data;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Lỗi chạy báo cáo {SelectedReport.FullName}", ex);
            ErrorMessage = $"Lỗi xử lý dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportCsvAsync()
    {
        if (ReportData == null || SelectedReport == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv",
            FileName = $"BaoCao_{SelectedReport.DisplayName}_{TimeHelper.GetVietnamTime():yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                string filePath = dialog.FileName;
                string displayName = SelectedReport.DisplayName;

                // Senior Fix: Đưa tiến trình Ghi file nặng nề xuống Background Thread
                await Task.Run(() =>
                {
                    if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        ExportHelper.ExportToExcel(ReportData, filePath, displayName);
                    }
                    else
                    {
                        ExportHelper.ExportToCsv(ReportData, filePath);
                    }
                });

                MessageBox.Show("Xuất file thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Lỗi xuất file báo cáo", ex);
                MessageBox.Show(
                    $"Không thể lưu file: {ex.Message}\n\nVui lòng kiểm tra xem file có đang bị mở bởi ứng dụng khác không.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    #endregion
}