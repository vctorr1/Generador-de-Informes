namespace PrivilegedAuditSuite.Domain.Models;

public sealed record OperationProgress
{
    public required string Message { get; init; }
    public double PercentComplete { get; init; }
    public int CurrentItem { get; init; }
    public int TotalItems { get; init; }
}
