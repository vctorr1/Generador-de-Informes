using System.Net.Http.Headers;
using System.Text.Json;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class CyberArkApiService(HttpClient httpClient, ICyberArkAuthenticationService authenticationService) : ICyberArkApiService
{
    public Task<CyberArkSession> LoginAsync(CyberArkCredentials credentials, CancellationToken cancellationToken)
    {
        return authenticationService.LoginAsync(credentials, cancellationToken);
    }

    public async Task<IReadOnlyList<CyberArkAccount>> GetAccountsAsync(
        CyberArkCredentials credentials,
        CyberArkSession session,
        CyberArkAccountQuery query,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.BaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.Token);

        var accounts = new List<CyberArkAccount>();
        var offset = 0;
        string? nextLink = null;
        var pageSize = Math.Clamp(query.PageSize, 1, 1000);
        var totalItems = 0;

        do
        {
            var requestUri = string.IsNullOrWhiteSpace(nextLink)
                ? BuildAccountsUri(credentials.BaseUrl, query, offset, pageSize)
                : BuildAbsoluteUri(credentials.BaseUrl, nextLink);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var jsonDocument = JsonDocument.Parse(responseBody);
            var root = jsonDocument.RootElement;

            if (TryGetPropertyIgnoreCase(root, "count", out var countProperty) && countProperty.TryGetInt32(out var count))
            {
                totalItems = count;
            }

            var currentPage = ParseAccounts(root).ToList();
            accounts.AddRange(currentPage);
            offset += currentPage.Count;

            progress?.Report(new OperationProgress
            {
                Message = $"Downloaded {accounts.Count} CyberArk accounts.",
                CurrentItem = accounts.Count,
                TotalItems = totalItems,
                PercentComplete = totalItems > 0 ? Math.Min(100d, accounts.Count * 100d / totalItems) : Math.Min(95d, accounts.Count),
            });

            if (query.MaxRecords.HasValue && accounts.Count >= query.MaxRecords.Value)
            {
                return accounts.Take(query.MaxRecords.Value).ToList();
            }

            nextLink = TryGetPropertyIgnoreCase(root, "nextLink", out var nextLinkProperty)
                ? nextLinkProperty.GetString()
                : null;
        }
        while (!string.IsNullOrWhiteSpace(nextLink));

        return accounts;
    }

    private static IEnumerable<CyberArkAccount> ParseAccounts(JsonElement root)
    {
        if (!TryGetPropertyIgnoreCase(root, "value", out var valueProperty) || valueProperty.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in valueProperty.EnumerateArray())
        {
            var automaticManagementEnabled = TryGetNestedBoolean(item, "secretManagement", "automaticManagementEnabled");
            var lastCpmError =
                TryGetNestedString(item, "secretManagement", "manualManagementReason") ??
                TryGetNestedString(item, "secretManagement", "failureReason") ??
                TryGetNestedString(item, "platformAccountProperties", "LastCPMError") ??
                TryGetNestedString(item, "platformAccountProperties", "Last CPM Error") ??
                TryGetNestedString(item, "secretManagement", "status");

            yield return new CyberArkAccount
            {
                Id = GetRequiredString(item, "id"),
                UserName = GetRequiredString(item, "userName"),
                Address = ReadString(item, "address"),
                PlatformId = ReadString(item, "platformId"),
                SafeName = ReadString(item, "safeName"),
                AccountName = ReadString(item, "name"),
                IsCpmDisabled = automaticManagementEnabled.HasValue && !automaticManagementEnabled.Value,
                LastCpmError = lastCpmError,
            };
        }
    }

    private static string BuildAccountsUri(string baseUrl, CyberArkAccountQuery query, int offset, int pageSize)
    {
        var parameters = new List<string>
        {
            $"limit={pageSize}",
            $"offset={offset}",
        };

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            parameters.Add($"search={Uri.EscapeDataString(query.SearchText)}");
        }

        return $"{baseUrl.TrimEnd('/')}/PasswordVault/API/Accounts?{string.Join("&", parameters)}";
    }

    private static string BuildAbsoluteUri(string baseUrl, string nextLink)
    {
        return nextLink.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? nextLink
            : $"{baseUrl.TrimEnd('/')}/{nextLink.TrimStart('/')}";
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? TryGetNestedString(JsonElement element, string parentPropertyName, string childPropertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, parentPropertyName, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryGetPropertyIgnoreCase(parent, childPropertyName, out var value)
            ? value.GetString()
            : null;
    }

    private static bool? TryGetNestedBoolean(JsonElement element, string parentPropertyName, string childPropertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, parentPropertyName, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!TryGetPropertyIgnoreCase(parent, childPropertyName, out var value) || value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            return null;
        }

        return value.GetBoolean();
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
