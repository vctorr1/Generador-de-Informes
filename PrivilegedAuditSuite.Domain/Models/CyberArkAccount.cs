namespace PrivilegedAuditSuite.Domain.Models;

public sealed record CyberArkAccount
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string Address { get; init; }
    public required string PlatformId { get; init; }
    public required string SafeName { get; init; }
    public bool IsCpmDisabled { get; init; }
    public string? LastCpmError { get; init; }
    public string? AccountName { get; init; }
    public IReadOnlyCollection<string> GroupNames { get; init; } = Array.Empty<string>();

    public string DisplayKey => string.IsNullOrWhiteSpace(Address) ? UserName : $"{Address}\\{UserName}";
}

public sealed record CyberArkCredentials
{
    public string BaseUrl { get; init; } = string.Empty;
    public string AuthenticationType { get; init; } = "cyberark";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record CyberArkSession
{
    public string Token { get; init; } = string.Empty;
    public string AuthenticationType { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresUtc { get; init; }
}

public sealed record CyberArkAccountQuery
{
    public string? SearchText { get; init; }
    public int PageSize { get; init; } = 200;
    public int? MaxRecords { get; init; }
}

public sealed record CyberArkFilterOptions
{
    public bool OnlyCpmDisabled { get; init; } = true;
    public IReadOnlyCollection<string> PlatformAllowList { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> PlatformBlockList { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SafeAllowList { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> SafeBlockList { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> ExcludedServers { get; init; } = Array.Empty<string>();
}
