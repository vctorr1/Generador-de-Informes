using System.Net.Http.Json;
using System.Text.Json;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class CyberArkAuthenticationService(HttpClient httpClient) : ICyberArkAuthenticationService
{
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CyberArkSession> LoginAsync(CyberArkCredentials credentials, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.BaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Username);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Password);

        var endpoint = $"{credentials.BaseUrl.TrimEnd('/')}/PasswordVault/API/Auth/{ResolveAuthenticationPath(credentials.AuthenticationType)}/Logon";
        var payload = new
        {
            username = credentials.Username,
            password = credentials.Password,
        };

        using var response = await httpClient.PostAsJsonAsync(endpoint, payload, _serializerOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var token = ExtractToken(responseBody);

        return new CyberArkSession
        {
            Token = token,
            AuthenticationType = credentials.AuthenticationType,
            CreatedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20),
        };
    }

    private static string ResolveAuthenticationPath(string? authenticationType)
    {
        return authenticationType?.Trim().ToLowerInvariant() switch
        {
            "ldap" => "LDAP",
            "radius" => "RADIUS",
            _ => "CyberArk",
        };
    }

    private static string ExtractToken(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException("CyberArk login returned an empty response.");
        }

        if (responseBody.TrimStart().StartsWith('\"'))
        {
            return JsonSerializer.Deserialize<string>(responseBody) ?? throw new InvalidOperationException("CyberArk login token could not be parsed.");
        }

        using var jsonDocument = JsonDocument.Parse(responseBody);
        var root = jsonDocument.RootElement;

        if (root.ValueKind == JsonValueKind.String)
        {
            return root.GetString() ?? throw new InvalidOperationException("CyberArk login token could not be parsed.");
        }

        if (TryGetPropertyIgnoreCase(root, "token", out var tokenProperty))
        {
            return tokenProperty.GetString() ?? throw new InvalidOperationException("CyberArk login token was empty.");
        }

        if (TryGetPropertyIgnoreCase(root, "CyberArkLogonResult", out var legacyTokenProperty))
        {
            return legacyTokenProperty.GetString() ?? throw new InvalidOperationException("CyberArk login token was empty.");
        }

        throw new InvalidOperationException("CyberArk login response did not contain a usable token.");
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
