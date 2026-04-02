namespace PrivilegedAuditSuite.Domain.Models;

public sealed record EntraUser
{
    public required string Id { get; init; }
    public required string UserPrincipalName { get; init; }
    public string? Mail { get; init; }
    public string? DisplayName { get; init; }
    public string? OnPremisesSamAccountName { get; init; }
    public bool AccountEnabled { get; init; } = true;
    public IReadOnlyCollection<string> GroupNames { get; init; } = Array.Empty<string>();
}
