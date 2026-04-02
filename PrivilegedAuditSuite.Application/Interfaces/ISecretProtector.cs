namespace PrivilegedAuditSuite.Application.Interfaces;

public interface ISecretProtector
{
    string Protect(string clearText);
    string Unprotect(string protectedText);
}
