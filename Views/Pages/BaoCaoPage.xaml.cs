using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Reports.DTO;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class BaoCaoPage : Page
{
    private DataTable? _procTable;

    private static readonly Regex SafeNameRegex =
        new(@"^[A-Za-z0-9_\.\[\]]+$", RegexOptions.Compiled);

    public BaoCaoPage()
    {
        InitializeComponent();
        SetReportState(ReportState.NoData);
    }

    private enum ReportState
    {
        Loading,
        NoData,
        HasData,
        Error
    }

    private void SetReportState(ReportState state, string? error = null)
    {
        PanelLoading.Visibility = Visibility.Collapsed;
        PanelNoData.Visibility = Visibility.Collapsed;
        PanelError.Visibility = Visibility.Collapsed;
        GridProc.Visibility = Visibility.Collapsed;

        switch (state)
        {
            case ReportState.Loading:
                PanelLoading.Visibility = Visibility.Visible;
                break;

            case ReportState.NoData:
                PanelNoData.Visibility = Visibility.Visible;
                break;

            case ReportState.HasData:
                GridProc.Visibility = Visibility.Visible;
                break;

            case ReportState.Error:
                PanelError.Visibility = Visibility.Visible;
                TxtError.Text = error ?? "Có lỗi xảy ra.";
                break;
        }
    }




    private async void BaoCaoPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Set giá trị mặc định (sẽ trigger SelectionChanged)
            if (CboSource.SelectedIndex == -1)
            {
                CboSource.SelectedIndex = 0;  // Trigger event
                                              // Đợi SelectionChanged chạy xong
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            SetReportState(ReportState.Error, $"Lỗi load trang: {ex.Message}");
        }
    }

    private async void CboSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboSource?.SelectedIndex >= 0)  // Bảo vệ
        {
            await UpdateSourceUi();
        }
    }

    private async Task UpdateSourceUi()
    {
        try
        {
            if (CboSource == null) return;

            bool isProc = CboSource.SelectedIndex == 1;  // ✅ FIX: 1 = SP

            if (DpFrom != null) DpFrom.IsEnabled = isProc;
            if (DpTo != null) DpTo.IsEnabled = isProc;

            if (!isProc)
            {
                if (DpFrom != null) DpFrom.SelectedDate = null;
                if (DpTo != null) DpTo.SelectedDate = null;
            }

            if (CboProcName != null)
            {
                var items = await LoadDbObjectsAsync(isProc);
                CboProcName.ItemsSource = items;

                if (items.Count > 0)
                    CboProcName.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            SetReportState(ReportState.Error, $"Không tải được danh sách báo cáo: {ex.Message}");
        }
    }




    private async Task<List<ReportItem>> LoadDbObjectsAsync(bool loadProcedures)
    {
        var result = new List<ReportItem>();

        using var db = new QuanLyKhachSanContext();
        var conn = db.Database.GetDbConnection();

        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        if (loadProcedures)
        {
            cmd.CommandText = """
        SELECT s.name + '.' + p.name as FullName, p.name as ProcName
        FROM sys.procedures p
        INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
        WHERE p.name LIKE 'SP_BAO_CAO_%'
        AND s.name = 'dbo'
        ORDER BY s.name, p.name
        """;
        }
        else
        {
            cmd.CommandText = """
        SELECT s.name + '.' + v.name as FullName, v.name as ViewName
        FROM sys.views v
        INNER JOIN sys.schemas s ON s.schema_id = v.schema_id
        WHERE v.name LIKE 'VW_%'
        AND s.name = 'dbo'
        ORDER BY s.name, v.name
        """;
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fullName = reader.GetString(0);
            var baseName = reader.GetString(1);

            // Parse tên hiển thị
            var displayName = FormatDisplayName(baseName, loadProcedures);

            result.Add(new ReportItem
            {
                FullName = fullName,
                DisplayName = displayName
            });
        }

        return result;
    }

    private static string FormatDisplayName(string objectName, bool isProcedure)
    {
        string clean = objectName;

        // Loại bỏ tiền tố
        if (isProcedure && clean.StartsWith("SP_BAO_CAO_"))
            clean = clean["SP_BAO_CAO_".Length..];
        else if (!isProcedure && clean.StartsWith("VW_"))
            clean = clean["VW_".Length..];

        // Chuyển "DoanhThu" thành "Doanh Thu"
        clean = Regex.Replace(clean, "([a-z])([A-Z])", "$1 $2");

        return clean.Trim();
    }

    private async void BtnRunProc_Click(object sender, RoutedEventArgs e)
    {
        BtnRunProc.IsEnabled = false;

        SetReportState(ReportState.Loading);
        var isView = CboSource.SelectedIndex == 0;

        try
        {
            var selectedItem = CboProcName.SelectedItem as ReportItem;

            if (selectedItem == null)
            {
                SetReportState(ReportState.Error, "Chưa chọn báo cáo.");
                return;
            }

            var inputName = selectedItem.FullName.Trim(); // Dùng FullName thay vì Text

            if (!SafeNameRegex.IsMatch(inputName))
            {
                SetReportState(ReportState.Error, "Tên View/SP không hợp lệ.");
                return;
            }

            using var db = new QuanLyKhachSanContext();
            var conn = db.Database.GetDbConnection();

            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            var (schema, name) = ParseTwoPartName(inputName, "dbo");

            if (isView)
            {
                if (!await ViewExistsAsync(conn, schema, name))
                {
                    SetReportState(ReportState.Error, "View không tồn tại.");
                    return;
                }

                cmd.CommandType = CommandType.Text;

                var qualified = $"{QuoteIdentifier(schema)}.{QuoteIdentifier(name)}";

                cmd.CommandText = $"SELECT TOP (5000) * FROM {qualified}";
            }
            else
            {
                if (!await ProcExistsAsync(conn, schema, name))
                {
                    SetReportState(ReportState.Error, "Stored Procedure không tồn tại.");
                    return;
                }

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = $"{schema}.{name}";

                if (DpFrom.SelectedDate.HasValue)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@TuNgay";
                    p.Value = DpFrom.SelectedDate.Value;
                    cmd.Parameters.Add(p);
                }

                if (DpTo.SelectedDate.HasValue)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@DenNgay";
                    p.Value = DpTo.SelectedDate.Value;
                    cmd.Parameters.Add(p);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            var table = new DataTable();
            table.Load(reader);

            _procTable = table;

            if (table.Rows.Count == 0)
            {
                GridProc.ItemsSource = null;
                SetReportState(ReportState.NoData);
            }
            else
            {
                GridProc.ItemsSource = table.DefaultView;
                SetReportState(ReportState.HasData);
            }

            BaoCaoTabs.SelectedItem = TabKetQua;
        }
        catch (Exception ex)
        {
            GridProc.ItemsSource = null;

            SetReportState(
                ReportState.Error,
                $"Lỗi chạy báo cáo: {ex.Message}");
        }
        finally
        {
            BtnRunProc.IsEnabled = true;
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_procTable == null || _procTable.Rows.Count == 0)
        {
            SetReportState(ReportState.Error, "Không có dữ liệu để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel file (*.xlsx)|*.xlsx",
            FileName = "BaoCao.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            ExportHelper.ExportToExcel(_procTable, dialog.FileName);
        }
    }

    private static (string schema, string name)
        ParseTwoPartName(string input, string defaultSchema)
    {
        var s = input.Trim();

        if (s.Contains('.'))
        {
            var parts = s.Split('.', 2);

            var schema = parts[0].Trim('[', ']', ' ');
            var name = parts[1].Trim('[', ']', ' ');

            return (
                string.IsNullOrWhiteSpace(schema)
                    ? defaultSchema
                    : schema,
                name
            );
        }

        return (defaultSchema, s.Trim('[', ']', ' '));
    }

    private static string QuoteIdentifier(string part)
    {
        var clean = part.Replace("]", "]]");
        return $"[{clean}]";
    }

    private static async Task<bool> ViewExistsAsync(DbConnection conn, string schema, string viewName)
    {
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
        SELECT 1
        FROM sys.views v
        INNER JOIN sys.schemas s ON s.schema_id = v.schema_id
        WHERE s.name = @schema AND v.name = @name
        """;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@schema";
        p1.Value = schema;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@name";
        p2.Value = viewName;
        cmd.Parameters.Add(p2);

        return await cmd.ExecuteScalarAsync() != null;
    }

    private static async Task<bool> ProcExistsAsync(DbConnection conn, string schema, string procName)
    {
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = """
        SELECT 1
        FROM sys.procedures p
        INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
        WHERE s.name = @schema AND p.name = @name
        """;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@schema";
        p1.Value = schema;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@name";
        p2.Value = procName;
        cmd.Parameters.Add(p2);

        return await cmd.ExecuteScalarAsync() != null;
    }
}