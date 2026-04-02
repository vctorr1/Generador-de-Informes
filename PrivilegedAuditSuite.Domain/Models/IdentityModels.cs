namespace PrivilegedAuditSuite.Domain.Models;

public sealed record IdentityComparisonOptions
{
    public bool IgnoreDisabledEntraUsers { get; init; } = true;
    public IReadOnlyDictionary<string, string> GroupMappings { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public enum IdentityDiscrepancyType
{
    MissingCyberArkAccount = 0,
    OrphanCyberArkAccount,
    GroupMismatch,
}

public sealed record IdentityDiscrepancy
{
    public required IdentityDiscrepancyType Type { get; init; }
    public required string Identity { get; init; }
    public string Source { get; init; } = string.Empty;
    public string RelatedIdentity { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
}

public sealed record IdentityReconciliationResult
{
    public required int TotalEntraUsers { get; init; }
    public required int TotalCyberArkAccounts { get; init; }
    public required int MatchedAccounts { get; init; }
    public required IReadOnlyList<IdentityDiscrepancy> Discrepancies { get; init; }
}
