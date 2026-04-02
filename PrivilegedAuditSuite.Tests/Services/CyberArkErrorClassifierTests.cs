using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Tests.Services;

public sealed class CyberArkErrorClassifierTests
{
    private readonly CyberArkErrorClassifier _classifier = new();

    [Theory]
    [InlineData("Access denied while changing password", ErrorCategory.AccessDenied)]
    [InlineData("The network path was not found", ErrorCategory.NetworkPathNotFound)]
    [InlineData("Password policy violation: complexity not met", ErrorCategory.PasswordPolicyViolation)]
    public void Classify_ReturnsExpectedCategory(string errorMessage, ErrorCategory expectedCategory)
    {
        var result = _classifier.Classify(errorMessage);

        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("CPM change failed for account DOMAIN\\svc_sql on server01.contoso.local. winRc=1326", "winRc=1326")]
    [InlineData("Password update failed on host unix01.local with ORA-01017 for user appsvc", "ORA=ORA-01017")]
    [InlineData("Reconcile failed for user admin01 on linux01.local: permission denied for account DOMAIN\\admin01", "reconcile failed for user admin<num> on <host>: permission denied for account <account>")]
    public void GetErrorSignature_ReturnsStableGroupingKey(string errorMessage, string expectedSignature)
    {
        var signature = _classifier.GetErrorSignature(errorMessage);

        Assert.Equal(expectedSignature, signature);
    }
}
