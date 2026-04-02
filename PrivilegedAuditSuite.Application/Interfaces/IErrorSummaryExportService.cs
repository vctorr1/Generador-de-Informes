using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface IErrorSummaryExportService
{
    Task ExportAsync(string filePath, IReadOnlyList<ErrorSummaryRow> rows, CancellationToken cancellationToken);
}
