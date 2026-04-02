using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Services;

public sealed class CyberArkAccountFilter
{
    public IReadOnlyList<CyberArkAccount> Apply(IEnumerable<CyberArkAccount> accounts, CyberArkFilterOptions filterOptions)
    {
        var platformAllow = CreateSet(filterOptions.PlatformAllowList);
        var platformBlock = CreateSet(filterOptions.PlatformBlockList);
        var safeAllow = CreateSet(filterOptions.SafeAllowList);
        var safeBlock = CreateSet(filterOptions.SafeBlockList);

        return accounts
            .Where(account => !filterOptions.OnlyCpmDisabled || account.IsCpmDisabled)
            .Where(account => platformAllow.Count == 0 || platformAllow.Contains(Normalize(account.PlatformId)))
            .Where(account => !platformBlock.Contains(Normalize(account.PlatformId)))
            .Where(account => safeAllow.Count == 0 || safeAllow.Contains(Normalize(account.SafeName)))
            .Where(account => !safeBlock.Contains(Normalize(account.SafeName)))
            .ToList();
    }

    private static HashSet<string> CreateSet(IEnumerable<string> values)
    {
        return values
            .Select(Normalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
