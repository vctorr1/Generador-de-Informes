using System.Net.Http.Headers;
using System.Text.Json;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class EntraIdGraphService(HttpClient httpClient) : IEntraIdService
{
    public async Task<IReadOnlyList<EntraUser>> GetUsersAsync(
        EntraIdConnectionSettings settings,
        bool includeGroups,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var accessToken = await AcquireAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
        var users = new List<EntraUser>();
        var nextLink = $"{settings.GraphBaseUrl.TrimEnd('/')}/v1.0/users?$select=id,displayName,userPrincipalName,mail,accountEnabled,onPremisesSamAccountName&$top=100";

        while (!string.IsNullOrWhiteSpace(nextLink))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextLink);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (TryGetPropertyIgnoreCase(root, "value", out var usersProperty))
            {
                foreach (var userElement in usersProperty.EnumerateArray())
                {
                    users.Add(new EntraUser
                    {
                        Id = ReadString(userElement, "id"),
                        UserPrincipalName = ReadString(userElement, "userPrincipalName"),
                        Mail = ReadNullableString(userElement, "mail"),
                        DisplayName = ReadNullableString(userElement, "displayName"),
                        OnPremisesSamAccountName = ReadNullableString(userElement, "onPremisesSamAccountName"),
                        AccountEnabled = ReadBoolean(userElement, "accountEnabled"),
                    });
                }
            }

            nextLink = TryGetPropertyIgnoreCase(root, "@odata.nextLink", out var nextLinkProperty)
                ? nextLinkProperty.GetString()
                : null;

            progress?.Report(new OperationProgress
            {
                Message = $"Downloaded {users.Count} Entra ID users.",
                CurrentItem = users.Count,
                PercentComplete = includeGroups ? 40d : 100d,
            });
        }

        if (!includeGroups || users.Count == 0)
        {
            return users;
        }

        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var groups = await GetGroupsForUserAsync(settings, accessToken, user.Id, cancellationToken).ConfigureAwait(false);
            users[index] = user with { GroupNames = groups };

            progress?.Report(new OperationProgress
            {
                Message = $"Resolved groups for {index + 1} of {users.Count} Entra ID users.",
                CurrentItem = index + 1,
                TotalItems = users.Count,
                PercentComplete = 40d + ((index + 1d) / users.Count * 60d),
            });
        }

        return users;
    }

    private async Task<string> AcquireAccessTokenAsync(EntraIdConnectionSettings settings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ClientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ClientSecret);

        var tokenEndpoint = $"https://login.microsoftonline.com/{settings.TenantId}/oauth2/v2.0/token";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = settings.ClientId,
            ["client_secret"] = settings.ClientSecret,
            ["scope"] = settings.Scope,
            ["grant_type"] = "client_credentials",
        });

        using var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(responseBody);

        return TryGetPropertyIgnoreCase(document.RootElement, "access_token", out var tokenProperty)
            ? tokenProperty.GetString() ?? throw new InvalidOperationException("Microsoft Graph access token was empty.")
            : throw new InvalidOperationException("Microsoft Graph token response did not include access_token.");
    }

    private async Task<IReadOnlyCollection<string>> GetGroupsForUserAsync(
        EntraIdConnectionSettings settings,
        string accessToken,
        string userId,
        CancellationToken cancellationToken)
    {
        var groups = new List<string>();
        var nextLink = $"{settings.GraphBaseUrl.TrimEnd('/')}/v1.0/users/{Uri.EscapeDataString(userId)}/memberOf/microsoft.graph.group?$select=displayName&$top=999";

        while (!string.IsNullOrWhiteSpace(nextLink))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextLink);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (TryGetPropertyIgnoreCase(root, "value", out var groupsProperty))
            {
                foreach (var groupElement in groupsProperty.EnumerateArray())
                {
                    var displayName = ReadNullableString(groupElement, "displayName");
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        groups.Add(displayName);
                    }
                }
            }

            nextLink = TryGetPropertyIgnoreCase(root, "@odata.nextLink", out var nextLinkProperty)
                ? nextLinkProperty.GetString()
                : null;
        }

        return groups;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? ReadNullableString(JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var property)
            ? property.GetString()
            : null;
    }

    private static bool ReadBoolean(JsonElement element, string propertyName)
    {
        return TryGetPropertyIgnoreCase(element, propertyName, out var property) &&
               property.ValueKind is JsonValueKind.True or JsonValueKind.False &&
               property.GetBoolean();
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
