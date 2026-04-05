using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class CyberArkAccountFilterTests
{
    private readonly CyberArkAccountFilter _filter = new();

    [Fact]
    public void Apply_ExcludesServersByAddress_CaseInsensitive()
    {
        var accounts = new[]
        {
            BuildAccount("1", "sql01.contoso.local", isCpmDisabled: true),
            BuildAccount("2", "WEB01.contoso.local", isCpmDisabled: true),
            BuildAccount("3", "jump01.contoso.local", isCpmDisabled: false),
        };

        var result = _filter.Apply(
            accounts,
            new CyberArkFilterOptions
            {
                OnlyCpmDisabled = true,
                ExcludedServers = ["web01.contoso.local"],
            });

        Assert.Single(result);
        Assert.Equal("sql01.contoso.local", result[0].Address);
    }

    private static CyberArkAccount BuildAccount(string id, string address, bool isCpmDisabled)
    {
        return new CyberArkAccount
        {
            Id = id,
            UserName = $"user{id}",
            Address = address,
            PlatformId = "WinServerLocal",
            SafeName = "Windows Servers",
            IsCpmDisabled = isCpmDisabled,
            LastCpmError = "Access denied while changing password",
        };
    }
}
