using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;
using PrivilegedAuditSuite.Infrastructure.Services;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class EncryptedJsonConfigurationStoreTests
{
    [Fact]
    public async Task SaveAndLoadAsync_PersistsExcludedServers()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.secure");
        var store = new EncryptedJsonConfigurationStore(new PassThroughSecretProtector());
        var configuration = new AppConfiguration
        {
            AuditFilters = new CyberArkFilterOptions
            {
                ExcludedServers = ["sql01.contoso.local", "web01.contoso.local"],
            },
        };

        try
        {
            await store.SaveAsync(filePath, configuration, CancellationToken.None);
            var loadedConfiguration = await store.LoadAsync(filePath, CancellationToken.None);

            Assert.NotNull(loadedConfiguration);
            Assert.Equal(configuration.AuditFilters.ExcludedServers, loadedConfiguration!.AuditFilters.ExcludedServers);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private sealed class PassThroughSecretProtector : ISecretProtector
    {
        public string Protect(string clearText) => clearText;

        public string Unprotect(string protectedText) => protectedText;
    }
}
