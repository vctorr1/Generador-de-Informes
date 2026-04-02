using System.Text;
using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;
using PrivilegedAuditSuite.Infrastructure.Services;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class ManualCyberArkImportTests
{
    [Fact]
    public async Task ImportCyberArkAccountsAsync_WithRepresentativeDisabledCpmCsv_ImportsAndClassifiesRows()
    {
        var csvContent = """
                         UserName,Address,SafeName,PlatformID,CPM Disabled,Last CPM Error,Name
                         svc_sql,sql01.contoso.local,Windows Servers,WinServerLocal,true,Access denied while changing password,svc_sql
                         svc_oracle,db01.contoso.local,Unix Safes,UnixSSH,true,The network path was not found,svc_oracle
                         admin_web,web01.contoso.local,Windows Servers,WinServerLocal,false,,admin_web
                         """;

        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);

        try
        {
            var importService = new ManualReportImportService();
            var filter = new CyberArkAccountFilter();
            var classifier = new CyberArkErrorClassifier();

            var importedAccounts = await importService.ImportCyberArkAccountsAsync(filePath, CancellationToken.None);
            var filteredAccounts = filter.Apply(importedAccounts, new CyberArkFilterOptions { OnlyCpmDisabled = true });
            var summary = classifier.Summarize(filteredAccounts.Select(account => account.LastCpmError));

            Assert.Equal(3, importedAccounts.Count);
            Assert.Equal(2, filteredAccounts.Count);
            Assert.All(filteredAccounts, account => Assert.True(account.IsCpmDisabled));

            Assert.Contains(filteredAccounts, account =>
                account.UserName == "svc_sql" &&
                account.PlatformId == "WinServerLocal" &&
                account.SafeName == "Windows Servers");

            Assert.Contains(summary, row => row.Category == ErrorCategory.AccessDenied && row.Count == 1);
            Assert.Contains(summary, row => row.Category == ErrorCategory.NetworkPathNotFound && row.Count == 1);
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
