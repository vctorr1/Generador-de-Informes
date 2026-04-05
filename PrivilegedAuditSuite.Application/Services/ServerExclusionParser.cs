using Microsoft.VisualBasic.FileIO;

namespace PrivilegedAuditSuite.Application.Services;

public sealed class ServerExclusionParser
{
    public IReadOnlyCollection<string> ParseManualEntries(string? rawValue)
    {
        return SplitLines(rawValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<string> ParseImportedContent(string fileName, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsvContent(content)
            : SplitLines(content).ToArray();
    }

    public IReadOnlyCollection<string> Merge(params IEnumerable<string>[] sources)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            foreach (var item in source.Select(Normalize).Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                merged.Add(item);
            }
        }

        return merged
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyCollection<string> ParseCsvContent(string content)
    {
        using var reader = new StringReader(content);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = true,
        };

        parser.SetDelimiters(",", ";", "\t");

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields() ?? [];
            var firstValue = fields
                .Select(Normalize)
                .FirstOrDefault(field => !string.IsNullOrWhiteSpace(field));

            if (!string.IsNullOrWhiteSpace(firstValue) && !IsHeader(firstValue))
            {
                values.Add(firstValue);
            }
        }

        return values
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> SplitLines(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            yield break;
        }

        using var reader = new StringReader(rawValue);
        while (reader.ReadLine() is { } line)
        {
            var normalized = Normalize(line);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                yield return normalized;
            }
        }
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static bool IsHeader(string value)
    {
        return value.Equals("server", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("address", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("host", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("hostname", StringComparison.OrdinalIgnoreCase);
    }
}
