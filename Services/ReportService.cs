using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Reports.DTO;

namespace QuanLyKhachSan_PhamTanLoi.Services;

public class ReportService
{
    private readonly QuanLyKhachSanContext _db;

    public ReportService(QuanLyKhachSanContext db)
    {
        _db = db;
    }

    public async Task<List<ReportItem>> LoadDbObjectsAsync(bool loadProcedures)
    {
        var result = new List<ReportItem>();
        var conn = _db.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
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
            var displayName = FormatDisplayName(baseName, loadProcedures);

            result.Add(new ReportItem
            {
                FullName = fullName,
                DisplayName = displayName
            });
        }

        return result;
    }

    public async Task<DataTable> RunReportAsync(string fullName, bool isView, DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        var (schema, name) = ParseTwoPartName(fullName, "dbo");

        if (isView)
        {
            cmd.CommandType = CommandType.Text;
            var qualified = $"{QuoteIdentifier(schema)}.{QuoteIdentifier(name)}";
            cmd.CommandText = $"SELECT TOP (5000) * FROM {qualified}";
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

        await using var reader = await cmd.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static string FormatDisplayName(string objectName, bool isProcedure)
    {
        string clean = objectName;
        if (isProcedure && clean.StartsWith("SP_BAO_CAO_"))
            clean = clean["SP_BAO_CAO_".Length..];
        else if (!isProcedure && clean.StartsWith("VW_"))
            clean = clean["VW_".Length..];

        clean = Regex.Replace(clean, "([a-z])([A-Z])", "$1 $2");
        return clean.Trim();
    }

    private static (string Schema, string Name) ParseTwoPartName(string fullName, string defaultSchema)
    {
        var parts = fullName.Split('.');
        return parts.Length switch
        {
            1 => (defaultSchema, parts[0]),
            2 => (parts[0], parts[1]),
            _ => throw new ArgumentException("Tên đối tượng không hợp lệ.")
        };
    }

    private static string QuoteIdentifier(string id) => $"[{id.Replace("]", "]]")}]";
}
