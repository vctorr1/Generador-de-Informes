namespace PrivilegedAuditSuite.Domain.Models;

public sealed record AppConfiguration
{
    public CyberArkConnectionSettings CyberArk { get; init; } = new();
    public EntraIdConnectionSettings EntraId { get; init; } = new();
    public OneDriveExcelSettings OneDriveExcel { get; init; } = new();
    public CyberArkFilterOptions AuditFilters { get; init; } = new();
    public IdentityComparisonOptions IdentityComparison { get; init; } = new();
}

public sealed record CyberArkConnectionSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public string AuthenticationType { get; init; } = "cyberark";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record EntraIdConnectionSettings
{
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string Scope { get; init; } = "https://graph.microsoft.com/.default";
    public string GraphBaseUrl { get; init; } = "https://graph.microsoft.com";
}

public sealed record OneDriveExcelSettings
{
    public string WorkbookUrl { get; init; } = string.Empty;
    public string WorksheetName { get; init; } = "AuditSummary";
    public string TargetColumn { get; init; } = "A";
}
