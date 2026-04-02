using ClosedXML.Excel;
using PrivilegedAuditSuite.Domain.Models;
using PrivilegedAuditSuite.Infrastructure.Services;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class ErrorSummaryExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WithCsv_WritesConfiguredSummaryLayout()
    {
        var exportService = new ErrorSummaryExportService();
        var rows = new List<ErrorSummaryRow>
        {
            new() { Label = "AccessDenied", Category = ErrorCategory.AccessDenied, Count = 4, SampleMessage = "Access denied" },
            new() { Label = "NetworkPathNotFound", Category = ErrorCategory.NetworkPathNotFound, Count = 2, SampleMessage = "Path not found" },
        };

        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        try
        {
            await exportService.ExportAsync(filePath, rows, CancellationToken.None);
            var lines = await File.ReadAllLinesAsync(filePath);

            Assert.Equal("Error,Num Errores", lines[0]);
            Assert.Equal("AccessDenied,4", lines[1]);
            Assert.Equal("NetworkPathNotFound,2", lines[2]);
            Assert.Equal(",", lines[3]);
            Assert.Equal(",Total", lines[4]);
            Assert.Equal(",6", lines[5]);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_WithExcel_WritesConfiguredSummaryLayout()
    {
        var exportService = new ErrorSummaryExportService();
        var rows = new List<ErrorSummaryRow>
        {
            new() { Label = "PasswordPolicyViolation", Category = ErrorCategory.PasswordPolicyViolation, Count = 3, SampleMessage = "Policy violation" },
        };

        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        try
        {
            await exportService.ExportAsync(filePath, rows, CancellationToken.None);

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet("Error Summary");

            Assert.Equal("Error", worksheet.Cell(1, 1).GetString());
            Assert.Equal("Num Errores", worksheet.Cell(1, 2).GetString());
            Assert.Equal("PasswordPolicyViolation", worksheet.Cell(2, 1).GetString());
            Assert.Equal(3d, worksheet.Cell(2, 2).GetDouble());
            Assert.Equal("Total", worksheet.Cell(4, 2).GetString());
            Assert.Equal(3d, worksheet.Cell(5, 2).GetDouble());
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
