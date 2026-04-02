using System.Text.Json;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class EncryptedJsonConfigurationStore(ISecretProtector secretProtector) : ISecureConfigurationStore
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task SaveAsync(string configurationPath, AppConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);
        ArgumentNullException.ThrowIfNull(configuration);

        var directory = Path.GetDirectoryName(configurationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, _serializerOptions);
        var protectedPayload = secretProtector.Protect(json);
        await File.WriteAllTextAsync(configurationPath, protectedPayload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppConfiguration?> LoadAsync(string configurationPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        if (!File.Exists(configurationPath))
        {
            return null;
        }

        var protectedPayload = await File.ReadAllTextAsync(configurationPath, cancellationToken).ConfigureAwait(false);
        var json = secretProtector.Unprotect(protectedPayload);

        return JsonSerializer.Deserialize<AppConfiguration>(json, _serializerOptions);
    }
}
