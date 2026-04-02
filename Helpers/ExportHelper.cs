using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace QuanLyKhachSan_PhamTanLoi.Helpers
{
    public static class ExportHelper
    {
        public static void ExportToExcel(DataTable table, string filePath, string sheetName = "Report")
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            int columnCount = table.Columns.Count;
            int rowCount = table.Rows.Count;

            // ===== Header =====
            for (int c = 0; c < columnCount; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = table.Columns[c].ColumnName;

                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // ===== Data =====
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < columnCount; c++)
                {
                    var value = table.Rows[r][c];
                    var cell = ws.Cell(r + 2, c + 1);

                    if (value == DBNull.Value)
                        continue;

                    if (value is DateTime dt)
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                    }
                    else if (value is decimal or double or float)
                    {
                        cell.Value = Convert.ToDouble(value);
                        cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                    else if (value is int or long or short)
                    {
                        cell.Value = Convert.ToInt64(value);
                        cell.Style.NumberFormat.Format = "#,##0";
                    }
                    else
                    {
                        cell.Value = value.ToString();
                    }
                }
            }

            // ===== Table Area =====
            var range = ws.Range(1, 1, rowCount + 1, columnCount);

            range.SetAutoFilter();

            // Freeze header
            ws.SheetView.FreezeRows(1);

            // Auto column width
            ws.Columns().AdjustToContents();

            // Border table
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Dotted;

            workbook.SaveAs(filePath);
        }

        public static void ExportToCsv(DataTable table, string filePath)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            var sb = new StringBuilder();

            // Header
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(EscapeCsv(table.Columns[i].ColumnName));
                if (i < table.Columns.Count - 1)
                    sb.Append(",");
            }

            sb.AppendLine();

            // Rows
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    sb.Append(EscapeCsv(row[i]?.ToString()));

                    if (i < table.Columns.Count - 1)
                        sb.Append(",");
                }

                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }
    }
}