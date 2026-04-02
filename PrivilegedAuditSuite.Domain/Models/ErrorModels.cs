namespace PrivilegedAuditSuite.Domain.Models;

public enum ErrorCategory
{
    NoError = 0,
    AccessDenied,
    NetworkPathNotFound,
    PasswordPolicyViolation,
    AccountLocked,
    PlatformMisconfiguration,
    ConnectivityIssue,
    Unclassified,
}

public sealed record ErrorClassificationResult
{
    public required ErrorCategory Category { get; init; }
    public required string OriginalMessage { get; init; }
    public required string NormalizedMessage { get; init; }
}

public sealed record ErrorSummaryRow
{
    public required string Label { get; init; }
    public required ErrorCategory Category { get; init; }
    public required int Count { get; init; }
    public required string SampleMessage { get; init; }
}
