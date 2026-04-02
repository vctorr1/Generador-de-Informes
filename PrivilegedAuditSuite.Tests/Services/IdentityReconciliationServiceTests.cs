using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class IdentityReconciliationServiceTests
{
    [Fact]
    public void Reconcile_FindsMissingAndOrphanAccounts()
    {
        var service = new IdentityReconciliationService();
        var entraUsers = new[]
        {
            new EntraUser
            {
                Id = "entra-1",
                UserPrincipalName = "alice@contoso.com",
                DisplayName = "Alice",
                AccountEnabled = true,
                GroupNames = ["Finance"],
            },
            new EntraUser
            {
                Id = "entra-2",
                UserPrincipalName = "bob@contoso.com",
                DisplayName = "Bob",
                AccountEnabled = true,
                GroupNames = ["Operations"],
            },
        };

        var cyberArkAccounts = new[]
        {
            new CyberArkAccount
            {
                Id = "ca-1",
                UserName = "alice@contoso.com",
                Address = "tenant.local",
                PlatformId = "WinDomain",
                SafeName = "Finance",
                IsCpmDisabled = true,
                GroupNames = ["Finance"],
            },
            new CyberArkAccount
            {
                Id = "ca-2",
                UserName = "svc-orphan",
                Address = "tenant.local",
                PlatformId = "UnixSSH",
                SafeName = "Operations",
                IsCpmDisabled = true,
            },
        };

        var result = service.Reconcile(entraUsers, cyberArkAccounts);

        Assert.Equal(1, result.MatchedAccounts);
        Assert.Contains(result.Discrepancies, item => item.Type == IdentityDiscrepancyType.MissingCyberArkAccount && item.Identity == "bob@contoso.com");
        Assert.Contains(result.Discrepancies, item => item.Type == IdentityDiscrepancyType.OrphanCyberArkAccount && item.Identity.Contains("svc-orphan", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Reconcile_FlagsGroupMismatchAfterMapping()
    {
        var service = new IdentityReconciliationService();
        var entraUsers = new[]
        {
            new EntraUser
            {
                Id = "entra-1",
                UserPrincipalName = "alice@contoso.com",
                AccountEnabled = true,
                GroupNames = ["Entra-Finance"],
            },
        };

        var cyberArkAccounts = new[]
        {
            new CyberArkAccount
            {
                Id = "ca-1",
                UserName = "alice@contoso.com",
                Address = "tenant.local",
                PlatformId = "WinDomain",
                SafeName = "Finance",
                IsCpmDisabled = true,
                GroupNames = ["CyberArk-Finance"],
            },
        };

        var result = service.Reconcile(
            entraUsers,
            cyberArkAccounts,
            new IdentityComparisonOptions
            {
                GroupMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Entra-Finance"] = "Mapped-Finance",
                },
            });

        Assert.Contains(result.Discrepancies, item => item.Type == IdentityDiscrepancyType.GroupMismatch);
    }
}
