using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface IManualReportImportService
{
    Task<IReadOnlyList<CyberArkAccount>> ImportCyberArkAccountsAsync(string filePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<EntraUser>> ImportEntraUsersAsync(string filePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<CyberArkAccount>> ImportCyberArkIdentityUsersAsync(string filePath, CancellationToken cancellationToken);
}
