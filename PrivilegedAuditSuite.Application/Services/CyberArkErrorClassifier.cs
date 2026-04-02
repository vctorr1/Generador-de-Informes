using System.Text.RegularExpressions;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Application.Services;

public sealed class CyberArkErrorClassifier
{
    private readonly IReadOnlyList<(string Prefix, Regex Regex)> _codeRules =
    [
        ("winRc", BuildRule("winrc\\s*=\\s*(-?\\d+)")),
        ("rc", BuildRule("(?<!win)rc\\s*=\\s*(-?\\d+)")),
        ("HRESULT", BuildRule("hresult\\s*[:=]?\\s*(0x[0-9a-f]+|\\d+)")),
        ("NTSTATUS", BuildRule("ntstatus\\s*[:=]?\\s*(0x[0-9a-f]+|\\d+)")),
        ("ErrorCode", BuildRule("error\\s*code\\s*[:=]?\\s*([a-z0-9_.-]+)")),
        ("ORA", BuildRule("\\b(ora-\\d{4,6})\\b")),
        ("SQL", BuildRule("\\b(sql\\d{4,6})\\b")),
    ];

    private readonly IReadOnlyList<(ErrorCategory Category, Regex Regex)> _rules =
    [
        (ErrorCategory.AccessDenied, BuildRule("access denied|permission denied|unauthorized")),
        (ErrorCategory.NetworkPathNotFound, BuildRule("network path.*not found|path.*not found|host.*unreachable|dns")),
        (ErrorCategory.PasswordPolicyViolation, BuildRule("password policy|complexity|password history|minimum length|violat")),
        (ErrorCategory.AccountLocked, BuildRule("account.*locked|locked out|user locked")),
        (ErrorCategory.PlatformMisconfiguration, BuildRule("platform .* not found|reconcile account missing|platform misconfig|dependency missing")),
        (ErrorCategory.ConnectivityIssue, BuildRule("timeout|timed out|connection refused|ssl|tls|http 5\\d\\d")),
    ];

    public ErrorClassificationResult Classify(string? lastCpmError)
    {
        if (string.IsNullOrWhiteSpace(lastCpmError))
        {
            return new ErrorClassificationResult
            {
                Category = ErrorCategory.NoError,
                OriginalMessage = string.Empty,
                NormalizedMessage = "No CPM error reported.",
            };
        }

        var trimmed = lastCpmError.Trim();
        var signature = GetErrorSignature(trimmed);

        foreach (var (category, regex) in _rules)
        {
            if (regex.IsMatch(trimmed))
            {
                return new ErrorClassificationResult
                {
                    Category = category,
                    OriginalMessage = trimmed,
                    NormalizedMessage = signature,
                };
            }
        }

        return new ErrorClassificationResult
        {
            Category = ErrorCategory.Unclassified,
            OriginalMessage = trimmed,
            NormalizedMessage = signature,
        };
    }

    public string GetErrorSignature(string? lastCpmError)
    {
        if (string.IsNullOrWhiteSpace(lastCpmError))
        {
            return string.Empty;
        }

        var trimmed = lastCpmError.Trim();

        foreach (var (prefix, regex) in _codeRules)
        {
            var match = regex.Match(trimmed);
            if (match.Success)
            {
                return $"{prefix}={match.Groups[1].Value.ToUpperInvariant()}";
            }
        }

        return SanitizeMessage(trimmed);
    }

    public IReadOnlyList<ErrorSummaryRow> Summarize(IEnumerable<string?> errors)
    {
        return errors
            .Select(Classify)
            .Where(result => result.Category != ErrorCategory.NoError)
            .GroupBy(result => result.Category)
            .Select(group => new ErrorSummaryRow
            {
                Label = group.Key.ToString(),
                Category = group.Key,
                Count = group.Count(),
                SampleMessage = group.Select(item => item.OriginalMessage).FirstOrDefault() ?? string.Empty,
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Category)
            .ToList();
    }

    private static Regex BuildRule(string pattern)
    {
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private static string SanitizeMessage(string message)
    {
        var normalized = message.ToLowerInvariant();
        normalized = Regex.Replace(normalized, "\"[^\"]+\"", "\"<value>\"", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, "'[^']+'", "'<value>'", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}\b", "<id>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b\d{1,3}(?:\.\d{1,3}){3}\b", "<ip>", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b[a-z0-9._-]+@[a-z0-9.-]+\.[a-z]{2,}\b", "<mail>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b[a-z0-9._-]+\\[a-z0-9$._-]+\b", "<account>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b(user|account|for user|for account)\s+[a-z0-9$._-]+\b", match => $"{match.Groups[1].Value} <account>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"(?<![a-z])[a-z]:\\[^\s,;]+", "<path>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\\\\[^\s,;]+", "<path>", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b[a-z0-9._-]+\.(?:local|corp|lan|int|com|net|org|es)\b", "<host>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\b\d+\b", "<num>", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\s+", " ", RegexOptions.CultureInvariant).Trim();

        return string.IsNullOrWhiteSpace(normalized) ? "unclassified" : normalized;
    }
}
