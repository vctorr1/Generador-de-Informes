using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface ISecureConfigurationStore
{
    Task SaveAsync(string configurationPath, AppConfiguration configuration, CancellationToken cancellationToken);
    Task<AppConfiguration?> LoadAsync(string configurationPath, CancellationToken cancellationToken);
}
