using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface IEntraIdService
{
    Task<IReadOnlyList<EntraUser>> GetUsersAsync(
        EntraIdConnectionSettings settings,
        bool includeGroups,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken);
}
