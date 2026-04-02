using System.Security.Cryptography;
using System.Text;
using PrivilegedAuditSuite.Application.Interfaces;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] AdditionalEntropy = Encoding.UTF8.GetBytes("PrivilegedAuditSuite.DPAPI.v1");

    public string Protect(string clearText)
    {
        if (string.IsNullOrWhiteSpace(clearText))
        {
            return string.Empty;
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(clearText);
        var protectedBytes = ProtectedData.Protect(plaintextBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return string.Empty;
        }

        var protectedBytes = Convert.FromBase64String(protectedText);
        var plaintextBytes = ProtectedData.Unprotect(protectedBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
