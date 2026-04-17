using ClosedXML.Excel;
using Boilerate.Application.Common.Exporters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Infrastructure.Exporters;

/// <summary>
/// Excel writer implementation using ClosedXML
/// </summary>
public class ClosedXMLWriter : IExcelWriter
{
    private readonly ILogger<ClosedXMLWriter> _logger;

    public ClosedXMLWriter(ILogger<ClosedXMLWriter> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> WriteAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Sheet1",
        List<string>? headers = null,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            var dataList = data.ToList();

            if (!dataList.Any())
            {
                _logger.LogWarning("No data to export for sheet '{SheetName}'", sheetName);
                return SaveWorkbookToBytes(workbook);
            }

            // Get properties
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsExportableType(p.PropertyType))
                .ToList();

            int headerRow = 1;

            // Handle Title
            if (!string.IsNullOrEmpty(title))
            {
                var titleCell = worksheet.Cell(1, 1);
                titleCell.Value = title;
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 16;
                titleCell.Style.Font.FontColor = XLColor.FromHtml("#1F4E78"); // Darker Blue
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                worksheet.Range(1, 1, 1, properties.Count).Merge();
                headerRow = 2;
            }

            // Style headers
            for (int i = 0; i < properties.Count; i++)
            {
                var headerText = headers != null && i < headers.Count
                            ? headers[i]
                            : FormatPropertyName(properties[i].Name);

                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headerText;
                
                // Header Styling
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD"); // Royal Blue
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.White;
            }

            // Write data
            var currentRow = headerRow + 1;
            foreach (var item in dataList)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    var value = properties[i].GetValue(item);
                    var cell = worksheet.Cell(currentRow, i + 1);

                    SetCellValue(cell, value, properties[i].PropertyType);

                    // Cell Styling
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.LightGray;

                    // Zebra Striping (Alternating Row Colors)
                    if (currentRow % 2 != headerRow % 2)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7"); // Very Light Blue
                    }
                }

                currentRow++;
            }

            // Global table adjustments
            var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, properties.Count);
            dataRange.SetAutoFilter();

            // Auto-fit and freeze
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(headerRow);

            _logger.LogInformation(
               "Exported {Count} rows with title '{Title}' to Excel sheet '{SheetName}'",
                dataList.Count,
                title,
                sheetName);

            return SaveWorkbookToBytes(workbook);
        }, cancellationToken);
    }

    public async Task<byte[]> WriteAsync(
        DataTable dataTable,
        string sheetName = "Sheet1",
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Insert DataTable
            worksheet.Cell(1, 1).InsertTable(dataTable);

            // Style header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Freeze header
            worksheet.SheetView.FreezeRows(1);

            _logger.LogInformation(
                "Exported DataTable with {Count} rows to Excel sheet '{SheetName}'",
                dataTable.Rows.Count,
                sheetName);

            return SaveWorkbookToBytes(workbook);
        }, cancellationToken);
    }

    public async Task<byte[]> WriteAsync(
        Dictionary<string, DataTable> sheets,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();

            foreach (var kvp in sheets)
            {
                var worksheet = workbook.Worksheets.Add(kvp.Key);

                // Insert DataTable
                worksheet.Cell(1, 1).InsertTable(kvp.Value);

                // Style header row
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Freeze header
                worksheet.SheetView.FreezeRows(1);
            }

            _logger.LogInformation(
              "Exported {Count} sheets to Excel workbook",
               sheets.Count);

            return SaveWorkbookToBytes(workbook);
        }, cancellationToken);
    }

    /// <summary>
    /// Set cell value with proper type handling
    /// </summary>
    private static void SetCellValue(IXLCell cell, object? value, Type propertyType)
    {
        if (value == null)
        {
            cell.Value = string.Empty;
            return;
        }

        // Handle specific types
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            cell.Value = (DateTime)value;
            cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        }
        else if (propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
        {
            cell.Value = ((DateTimeOffset)value).DateTime;
            cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        }
        else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
        {
            cell.Value = (bool)value ? "Yes" : "No";
        }
        else if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
        {
            cell.Value = (decimal)value;
            cell.Style.NumberFormat.Format = "#,##0.00";
        }
        else if (IsNumericType(propertyType))
        {
            cell.Value = Convert.ToDouble(value);
            cell.Style.NumberFormat.Format = "#,##0";
        }
        else
        {
            cell.Value = value.ToString();
        }
    }

    /// <summary>
    /// Check if type is numeric
    /// </summary>
    private static bool IsNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// Check if type can be exported
    /// </summary>
    private static bool IsExportableType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(decimal) ||
               type == typeof(Guid);
    }

    /// <summary>
    /// Format property name to readable header
    /// </summary>
    private static string FormatPropertyName(string propertyName)
    {
        // CamelCase -> Camel Case
        return System.Text.RegularExpressions.Regex.Replace(
            propertyName,
            "([a-z])([A-Z])",
            "$1 $2");
    }

    /// <summary>
    /// Save workbook to byte array
    /// </summary>
    private static byte[] SaveWorkbookToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
