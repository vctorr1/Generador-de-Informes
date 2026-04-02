using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface ICyberArkApiService
{
    Task<CyberArkSession> LoginAsync(CyberArkCredentials credentials, CancellationToken cancellationToken);

    Task<IReadOnlyList<CyberArkAccount>> GetAccountsAsync(
        CyberArkCredentials credentials,
        CyberArkSession session,
        CyberArkAccountQuery query,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken);
}
