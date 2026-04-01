using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Dtos;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class BaoCaoPage : Page
{
    private readonly ObservableCollection<VwDoanhThuThang> _doanhThu = new();
    private readonly ObservableCollection<VwCoCauChiPhi> _chiPhi = new();
    private readonly ObservableCollection<VwTopDichVu> _topDichVu = new();
    private DataTable? _procTable;
    private static readonly Regex SafeNameRegex = new(@"^[A-Za-z0-9_\.\[\]]+$", RegexOptions.Compiled);

    public BaoCaoPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadViewsAsync();
        UpdateSourceUi();
    }

    private void CboSource_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSourceUi();

    private void UpdateSourceUi()
    {
        var isProc = CboSource?.SelectedIndex != 1;
        if (DpFrom != null) DpFrom.IsEnabled = isProc;
        if (DpTo != null) DpTo.IsEnabled = isProc;
        if (!isProc)
        {
            if (DpFrom != null) DpFrom.SelectedDate = null;
            if (DpTo != null) DpTo.SelectedDate = null;
        }

        // Tự động gợi ý View/SP khi chọn Loại
        if (CboProcName != null)
        {
            if (isProc)
            {
                CboProcName.ItemsSource = new List<string>
                {
                    "dbo.SP_BAO_CAO_DOANH_THU",
                    "dbo.SP_BAO_CAO_CHI_PHI",
                    "dbo.SP_BAO_CAO_LOI_NHUAN"
                };
            }
            else
            {
                CboProcName.ItemsSource = new List<string>
                {
                    "dbo.VW_DOANH_THU_LOAI_PHONG",
                    "dbo.VW_CONG_SUAT_PHONG",
                    "dbo.VW_TOP_DICH_VU",
                    "dbo.VW_KPI_TONG_HOP",
                    "dbo.VW_CO_CAU_CHI_PHI",
                    "dbo.VW_LOI_NHUAN_NAM",
                    "dbo.VW_LOI_NHUAN_THANG",
                    "dbo.VW_CHI_PHI_NAM",
                    "dbo.VW_CHI_PHI_THANG",
                    "dbo.VW_DOANH_THU_NAM",
                    "dbo.VW_DOANH_THU_THANG"
                };
            }
            CboProcName.SelectedIndex = 0;
        }
    }

    private async Task LoadViewsAsync()
    {
        using var db = new QuanLyKhachSanContext();
        var failedSections = new List<string>();

        try
        {
            var dt = await db.VwDoanhThuThangs.OrderBy(x => x.Nam).ThenBy(x => x.Thang).ToListAsync();
            _doanhThu.Clear();
            foreach (var x in dt) _doanhThu.Add(x);
        }
        catch
        {
            _doanhThu.Clear();
            failedSections.Add("Doanh thu theo tháng");
        }

        try
        {
            var cp = await db.VwCoCauChiPhis.OrderByDescending(x => x.TongChiPhi).ToListAsync();
            _chiPhi.Clear();
            foreach (var x in cp) _chiPhi.Add(x);
        }
        catch
        {
            _chiPhi.Clear();
            failedSections.Add("Chi phí theo loại");
        }

        try
        {
            var top = await db.VwTopDichVus.OrderByDescending(x => x.TongSuDung).ToListAsync();
            _topDichVu.Clear();
            foreach (var x in top) _topDichVu.Add(x);
        }
        catch
        {
            _topDichVu.Clear();
            failedSections.Add("Top dịch vụ");
        }

        GridDoanhThu.ItemsSource = _doanhThu;
        GridChiPhi.ItemsSource = _chiPhi;
        GridTopDichVu.ItemsSource = _topDichVu;

        if (failedSections.Count > 0)
        {
            MessageBox.Show($"Một số báo cáo không tải được: {string.Join(", ", failedSections)}", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void ExportToCsv(DataView view)
    {
        if (view.Table == null)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "bao_cao.csv" };
        if (dlg.ShowDialog() != true) return;
        using var fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
        using var sw = new StreamWriter(fs, new UTF8Encoding(true));
        for (int i = 0; i < view.Table.Columns.Count; i++)
        {
            if (i > 0) sw.Write(",");
            sw.Write(EscapeCsv(view.Table.Columns[i].ColumnName));
        }
        sw.WriteLine();
        foreach (DataRowView row in view)
        {
            for (int i = 0; i < view.Table.Columns.Count; i++)
            {
                if (i > 0) sw.Write(",");
                var val = row[i]?.ToString() ?? "";
                sw.Write(EscapeCsv(val));
            }
            sw.WriteLine();
        }
    }

    private static string EscapeCsv(string input)
    {
        var mustQuote = input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
        if (!mustQuote) return input;
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var ch in input)
        {
            if (ch == '"') sb.Append("\"\"");
            else sb.Append(ch);
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static DataTable ConvertToDataTable<T>(IEnumerable<T> items)
    {
        var table = new DataTable();
        var props = typeof(T).GetProperties();
        foreach (var p in props) table.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
        foreach (var item in items)
        {
            var values = new object?[props.Length];
            for (int i = 0; i < props.Length; i++)
                values[i] = props[i].GetValue(item, null);
            table.Rows.Add(values);
        }
        return table;
    }

    private void ExportDoanhThu_Click(object sender, RoutedEventArgs e)
    {
        var dt = ConvertToDataTable(_doanhThu);
        ExportToCsv(dt.DefaultView);
    }

    private void ExportChiPhi_Click(object sender, RoutedEventArgs e)
    {
        var dt = ConvertToDataTable(_chiPhi);
        ExportToCsv(dt.DefaultView);
    }


    private void ExportTopDichVu_Click(object sender, RoutedEventArgs e)
    {
        var dt = ConvertToDataTable(_topDichVu);
        ExportToCsv(dt.DefaultView);
    }

    private async void BtnRunProc_Click(object sender, RoutedEventArgs e)
    {
        var inputName = CboProcName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(inputName))
        {
            MessageBox.Show("Nhập tên View hoặc Stored Procedure.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!SafeNameRegex.IsMatch(inputName))
        {
            MessageBox.Show("Tên không hợp lệ. Chỉ dùng ký tự chữ/số, dấu gạch dưới (_) và dấu chấm (.).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            using var db = new QuanLyKhachSanContext();
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();

            var isView = CboSource.SelectedIndex == 1;
            var (schema, name) = ParseTwoPartName(inputName, isView ? "dbo" : "dbo");

            if (isView)
            {
                if (!await ViewExistsAsync(conn, schema, name))
                {
                    MessageBox.Show("View không tồn tại trong database.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show("Stored Procedure không tồn tại trong database.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                cmd.CommandText = $"{schema}.{name}";
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
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
            GridProc.ItemsSource = table.DefaultView;
            if (BaoCaoTabs != null && TabKetQua != null)
                BaoCaoTabs.SelectedItem = TabKetQua;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi chạy SP: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnExportProc_Click(object sender, RoutedEventArgs e)
    {
        if (_procTable == null || _procTable.Rows.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ExportToCsv(_procTable.DefaultView);
    }

    private static (string schema, string name) ParseTwoPartName(string input, string defaultSchema)
    {
        var s = input.Trim();
        if (s.Contains('.'))
        {
            var parts = s.Split('.', 2);
            var schema = parts[0].Trim('[', ']', ' ');
            var name = parts[1].Trim('[', ']', ' ');
            return (string.IsNullOrWhiteSpace(schema) ? defaultSchema : schema, name);
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
        cmd.CommandType = CommandType.Text;
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
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private static async Task<bool> ProcExistsAsync(DbConnection conn, string schema, string procName)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
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
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
}
