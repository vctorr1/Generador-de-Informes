using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Interfaces;

public interface ICyberArkAuthenticationService
{
    Task<CyberArkSession> LoginAsync(CyberArkCredentials credentials, CancellationToken cancellationToken);
}
