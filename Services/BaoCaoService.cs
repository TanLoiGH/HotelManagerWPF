using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Constants;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Reports.DTO;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class BaoCaoService
{
    private readonly QuanLyKhachSanContext _db;

    public BaoCaoService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách các View hoặc Stored Procedure phục vụ báo cáo.
    /// </summary>
    public async Task<List<ReportItem>> LoadDbObjectsAsync(bool loadProcedures,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ReportItem>();
        var conn = _db.Database.GetDbConnection();

        // Senior Fix: Quản lý trạng thái kết nối chặt chẽ để tránh Connection Leak
        bool isConnectionOwned = false;
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
            isConnectionOwned = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();

            if (loadProcedures)
            {
                cmd.CommandText = $@"
                    SELECT s.name + '.' + p.name as FullName, p.name as ProcName
                    FROM sys.procedures p
                    INNER JOIN sys.schemas s ON s.schema_id = p.schema_id
                    WHERE p.name LIKE '{ReportDbObjects.BaoCaoProcedureLikePattern}'
                    AND s.name = '{ReportDbObjects.DefaultSchema}'
                    ORDER BY s.name, p.name";
            }
            else
            {
                cmd.CommandText = $@"
                    SELECT s.name + '.' + v.name as FullName, v.name as ViewName
                    FROM sys.views v
                    INNER JOIN sys.schemas s ON s.schema_id = v.schema_id
                    WHERE v.name LIKE '{ReportDbObjects.ViewLikePattern}'
                    AND s.name = '{ReportDbObjects.DefaultSchema}'
                    ORDER BY s.name, v.name";
            }

            cmd.CommandType = CommandType.Text;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var fullName = reader.GetString(0);
                var baseName = reader.GetString(1);
                var displayName = FormatDisplayName(baseName, loadProcedures);

                result.Add(new ReportItem
                {
                    FullName = fullName,
                    DisplayName = displayName
                });
            }

            return result;
        }
        finally
        {
            // Senior Fix: Chỉ đóng kết nối nếu chính hàm này là người mở nó
            if (isConnectionOwned && conn.State == ConnectionState.Open)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Chạy báo cáo (View hoặc SP) và trả về DataTable để bind trực tiếp vào DataGrid WPF.
    /// </summary>
    public async Task<DataTable> RunReportAsync(string fullName, bool isView, DateTime? tuNgay = null,
        DateTime? denNgay = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Tên đối tượng báo cáo không được để trống.");

        var conn = _db.Database.GetDbConnection();
        bool isConnectionOwned = false;

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
            isConnectionOwned = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            var (schema, name) = ParseTwoPartName(fullName, ReportDbObjects.DefaultSchema);

            if (isView)
            {
                cmd.CommandType = CommandType.Text;
                // Defensive: Đảm bảo QuoteIdentifier để chống SQL Injection qua tên View
                var qualified = $"{QuoteIdentifier(schema)}.{QuoteIdentifier(name)}";
                cmd.CommandText = $"SELECT TOP ({ReportDbObjects.MaxRows}) * FROM {qualified}";
            }
            else
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = $"{schema}.{name}";

                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@TuNgay";
                p1.Value = (object?)tuNgay ?? DBNull.Value;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@DenNgay";
                p2.Value = (object?)denNgay ?? DBNull.Value;
                cmd.Parameters.Add(p2);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var table = new DataTable();

            // DataTable.Load là thao tác đồng bộ, nhưng chạy rất nhanh khi dữ liệu đã ở trong Reader
            table.Load(reader);

            return table;
        }
        catch (DbException ex)
        {
            // Bắt lỗi rành mạch từ DB để UI dễ xử lý
            throw new InvalidOperationException($"Lỗi khi chạy báo cáo {fullName}: {ex.Message}", ex);
        }
        finally
        {
            if (isConnectionOwned && conn.State == ConnectionState.Open)
            {
                await conn.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Chuẩn hóa tên Stored Procedure / View thành chuỗi thân thiện với người dùng.
    /// </summary>
    private static string FormatDisplayName(string objectName, bool isProcedure)
    {
        string clean = objectName;

        if (isProcedure && clean.StartsWith(ReportDbObjects.BaoCaoProcedureDisplayPrefix))
            clean = clean.Substring(ReportDbObjects.BaoCaoProcedureDisplayPrefix.Length);
        else if (!isProcedure && clean.StartsWith(ReportDbObjects.ViewDisplayPrefix))
            clean = clean.Substring(ReportDbObjects.ViewDisplayPrefix.Length);

        // 1. Thay thế dấu gạch dưới (VD: SO_SANH_THANG -> SO SANH THANG)
        clean = clean.Replace("_", " ");

        // 2. Tách từ viết hoa liền nhau (VD: BaoCaoDoanhThu -> Bao Cao Doanh Thu)
        clean = Regex.Replace(clean, "([a-z])([A-Z])", "$1 $2");

        // 3. Chuyển thành Tiêu đề chuẩn (VD: SO SANH THANG -> So Sanh Thang)
        clean = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean.ToLower());

        return clean.Trim();
    }

    private static (string Schema, string Name) ParseTwoPartName(string fullName, string defaultSchema)
    {
        var parts = fullName.Split('.');
        return parts.Length switch
        {
            1 => (defaultSchema, parts[0]),
            2 => (parts[0], parts[1]),
            _ => throw new ArgumentException($"Tên đối tượng '{fullName}' không đúng định dạng Schema.Name")
        };
    }

    // Đảm bảo bọc tên bằng ngoặc vuông để chống lỗi syntax và SQL Injection
    private static string QuoteIdentifier(string id) => $"[{id.Replace("]", "]]")}]";
}