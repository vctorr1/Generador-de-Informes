using System.Text;
using ClosedXML.Excel;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class ErrorSummaryExportService : IErrorSummaryExportService
{
    public async Task ExportAsync(string filePath, IReadOnlyList<ErrorSummaryRow> rows, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".csv":
                await ExportCsvAsync(filePath, rows, cancellationToken);
                break;
            case ".xlsx":
                await ExportExcelAsync(filePath, rows, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Unsupported export format '{extension}'. Use CSV or XLSX.");
        }
    }

    private static async Task ExportCsvAsync(string filePath, IReadOnlyList<ErrorSummaryRow> rows, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Error,Num Errores");

        foreach (var row in rows)
        {
            builder.Append(Escape(row.Label));
            builder.Append(',');
            builder.AppendLine(row.Count.ToString());
        }

        builder.AppendLine(",");
        builder.AppendLine(",Total");
        builder.AppendLine($",{rows.Sum(row => row.Count)}");

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken);
    }

    private static Task ExportExcelAsync(string filePath, IReadOnlyList<ErrorSummaryRow> rows, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("Error Summary");

            worksheet.Cell(1, 1).Value = "Error";
            worksheet.Cell(1, 2).Value = "Num Errores";

            var currentRow = 2;
            foreach (var row in rows)
            {
                worksheet.Cell(currentRow, 1).Value = row.Label;
                worksheet.Cell(currentRow, 2).Value = row.Count;
                currentRow++;
            }

            currentRow++;
            worksheet.Cell(currentRow, 2).Value = "Total";
            worksheet.Cell(currentRow + 1, 2).Value = rows.Sum(row => row.Count);

            worksheet.Columns(1, 2).AdjustToContents();
            workbook.SaveAs(filePath);
        }, cancellationToken);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
