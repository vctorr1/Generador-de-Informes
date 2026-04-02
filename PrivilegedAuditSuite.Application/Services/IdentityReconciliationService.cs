using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Services;

public sealed class IdentityReconciliationService
{
    public IdentityReconciliationResult Reconcile(
        IEnumerable<EntraUser> entraUsers,
        IEnumerable<CyberArkAccount> cyberArkAccounts,
        IdentityComparisonOptions? comparisonOptions = null)
    {
        var options = comparisonOptions ?? new IdentityComparisonOptions();
        var filteredEntraUsers = entraUsers
            .Where(user => !options.IgnoreDisabledEntraUsers || user.AccountEnabled)
            .ToList();
        var cyberArkList = cyberArkAccounts.ToList();
        var cyberArkLookup = BuildCyberArkLookup(cyberArkList);

        var discrepancies = new List<IdentityDiscrepancy>();
        var matchedAccountIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchedUsers = 0;

        foreach (var entraUser in filteredEntraUsers)
        {
            var matchedAccount = FindMatch(entraUser, cyberArkLookup);

            if (matchedAccount is null)
            {
                discrepancies.Add(new IdentityDiscrepancy
                {
                    Type = IdentityDiscrepancyType.MissingCyberArkAccount,
                    Identity = entraUser.UserPrincipalName,
                    Source = "Entra ID",
                    Details = "User exists in Entra ID but no equivalent account was found in CyberArk.",
                });
                continue;
            }

            matchedUsers++;
            matchedAccountIds.Add(matchedAccount.Id);

            var groupMismatch = BuildGroupMismatch(entraUser, matchedAccount, options.GroupMappings);
            if (groupMismatch is not null)
            {
                discrepancies.Add(groupMismatch);
            }
        }

        discrepancies.AddRange(
            cyberArkList
                .Where(account => !matchedAccountIds.Contains(account.Id))
                .Select(account => new IdentityDiscrepancy
                {
                    Type = IdentityDiscrepancyType.OrphanCyberArkAccount,
                    Identity = account.DisplayKey,
                    Source = "CyberArk",
                    Details = "Account exists in CyberArk but no matching Entra ID user was found.",
                }));

        return new IdentityReconciliationResult
        {
            TotalEntraUsers = filteredEntraUsers.Count,
            TotalCyberArkAccounts = cyberArkList.Count,
            MatchedAccounts = matchedUsers,
            Discrepancies = discrepancies
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Identity, StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    private static Dictionary<string, CyberArkAccount> BuildCyberArkLookup(IEnumerable<CyberArkAccount> accounts)
    {
        return accounts
            .SelectMany(account => GetCandidateKeys(account).Select(key => new KeyValuePair<string, CyberArkAccount>(Normalize(key), account)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);
    }

    private static CyberArkAccount? FindMatch(EntraUser user, IReadOnlyDictionary<string, CyberArkAccount> cyberArkLookup)
    {
        foreach (var key in GetCandidateKeys(user))
        {
            var normalized = Normalize(key);
            if (!string.IsNullOrWhiteSpace(normalized) && cyberArkLookup.TryGetValue(normalized, out var account))
            {
                return account;
            }
        }

        return null;
    }

    private static IdentityDiscrepancy? BuildGroupMismatch(
        EntraUser user,
        CyberArkAccount account,
        IReadOnlyDictionary<string, string> groupMappings)
    {
        var normalizedEntraGroups = NormalizeGroups(user.GroupNames, groupMappings);
        var normalizedCyberArkGroups = NormalizeGroups(account.GroupNames, null);

        if (normalizedEntraGroups.SetEquals(normalizedCyberArkGroups))
        {
            return null;
        }

        return new IdentityDiscrepancy
        {
            Type = IdentityDiscrepancyType.GroupMismatch,
            Identity = user.UserPrincipalName,
            RelatedIdentity = account.DisplayKey,
            Source = "Entra ID / CyberArk",
            Details = $"Entra groups: {string.Join(", ", normalizedEntraGroups.DefaultIfEmpty("-"))} | CyberArk groups: {string.Join(", ", normalizedCyberArkGroups.DefaultIfEmpty("-"))}",
        };
    }

    private static IEnumerable<string> GetCandidateKeys(EntraUser user)
    {
        yield return user.UserPrincipalName;

        if (!string.IsNullOrWhiteSpace(user.Mail))
        {
            yield return user.Mail;
        }

        if (!string.IsNullOrWhiteSpace(user.OnPremisesSamAccountName))
        {
            yield return user.OnPremisesSamAccountName;
        }
    }

    private static IEnumerable<string> GetCandidateKeys(CyberArkAccount account)
    {
        yield return account.UserName;
        yield return account.DisplayKey;

        if (!string.IsNullOrWhiteSpace(account.AccountName))
        {
            yield return account.AccountName;
        }
    }

    private static HashSet<string> NormalizeGroups(IEnumerable<string> groups, IReadOnlyDictionary<string, string>? groupMappings)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var trimmed = Normalize(group);
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (groupMappings is not null && groupMappings.TryGetValue(trimmed, out var mapped))
            {
                normalized.Add(Normalize(mapped));
                continue;
            }

            normalized.Add(trimmed);
        }

        return normalized;
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
