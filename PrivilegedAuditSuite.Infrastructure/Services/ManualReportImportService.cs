using System.Text;
using ClosedXML.Excel;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Infrastructure.Services;

public sealed class ManualReportImportService : IManualReportImportService
{
    public async Task<IReadOnlyList<CyberArkAccount>> ImportCyberArkAccountsAsync(string filePath, CancellationToken cancellationToken)
    {
        var rows = await ReadRowsAsync(filePath, cancellationToken);
        return rows
            .Select(MapCyberArkAccount)
            .Where(account => !string.IsNullOrWhiteSpace(account.UserName))
            .ToList();
    }

    public async Task<IReadOnlyList<EntraUser>> ImportEntraUsersAsync(string filePath, CancellationToken cancellationToken)
    {
        var rows = await ReadRowsAsync(filePath, cancellationToken);
        return rows
            .Select(MapEntraUser)
            .Where(user => !string.IsNullOrWhiteSpace(user.UserPrincipalName))
            .ToList();
    }

    public async Task<IReadOnlyList<CyberArkAccount>> ImportCyberArkIdentityUsersAsync(string filePath, CancellationToken cancellationToken)
    {
        var rows = await ReadRowsAsync(filePath, cancellationToken);
        return rows
            .Select(MapCyberArkIdentityUser)
            .Where(account => !string.IsNullOrWhiteSpace(account.UserName))
            .ToList();
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ReadRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".csv" or ".txt" => await ReadCsvRowsAsync(filePath, cancellationToken),
            ".xlsx" or ".xlsm" => await Task.Run(() => ReadExcelRows(filePath), cancellationToken),
            _ => throw new NotSupportedException($"Unsupported file format '{extension}'. Use CSV or Excel."),
        };
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ReadCsvRowsAsync(string filePath, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        if (lines.Length == 0)
        {
            return [];
        }

        var delimiter = DetectDelimiter(lines[0]);
        var headers = ParseDelimitedLine(lines[0], delimiter).Select(NormalizeHeader).ToArray();
        var rows = new List<IReadOnlyDictionary<string, string>>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseDelimitedLine(line, delimiter);
            rows.Add(BuildRow(headers, values));
        }

        return rows;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadExcelRows(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(sheet => sheet.RangeUsed() is not null)
            ?? throw new InvalidOperationException("The workbook does not contain any populated worksheet.");
        var usedRange = worksheet.RangeUsed()
            ?? throw new InvalidOperationException("The workbook does not contain any populated worksheet.");
        var headerRow = usedRange.FirstRowUsed();
        var dataRows = usedRange.RowsUsed().Skip(1);
        var headers = headerRow.Cells().Select(cell => NormalizeHeader(cell.GetString())).ToArray();
        var rows = new List<IReadOnlyDictionary<string, string>>();

        foreach (var row in dataRows)
        {
            var values = row.Cells(1, headers.Length).Select(cell => cell.GetFormattedString()).ToArray();
            rows.Add(BuildRow(headers, values));
        }

        return rows;
    }

    private static CyberArkAccount MapCyberArkAccount(IReadOnlyDictionary<string, string> row)
    {
        var userName = GetValue(row, "username", "user", "userid", "userprincipalname", "upn", "logonname");
        var address = GetValue(row, "address", "hostname", "machine", "machinename", "systemname");
        var safeName = GetValue(row, "safename", "safe");
        var platformId = GetValue(row, "platformid", "platform");
        var accountName = GetValue(row, "name", "accountname", "displayname");
        var id = GetValue(row, "id", "accountid");
        var groupNames = SplitGroups(GetValue(row, "groupnames", "groups", "memberof"));

        if (string.IsNullOrWhiteSpace(id))
        {
            id = BuildFallbackId(safeName, platformId, address, userName, accountName);
        }

        var isCpmDisabled = ParseBoolean(GetValue(row, "cpmdisabled", "cpm disabled", "cpm_disabled")) ??
                            InvertBoolean(ParseBoolean(GetValue(row, "automaticmanagementenabled", "automatic management enabled")));

        return new CyberArkAccount
        {
            Id = id,
            UserName = userName,
            Address = address,
            PlatformId = platformId,
            SafeName = safeName,
            AccountName = accountName,
            IsCpmDisabled = isCpmDisabled ?? false,
            LastCpmError = GetValue(row, "lastcpmerror", "last cpm error", "manualmanagementreason", "failurereason"),
            GroupNames = groupNames,
        };
    }

    private static EntraUser MapEntraUser(IReadOnlyDictionary<string, string> row)
    {
        var upn = GetValue(row, "userprincipalname", "upn", "mail", "email", "username");
        var id = GetValue(row, "id", "objectid", "userid");

        if (string.IsNullOrWhiteSpace(id))
        {
            id = upn;
        }

        return new EntraUser
        {
            Id = id,
            UserPrincipalName = upn,
            Mail = GetValue(row, "mail", "email"),
            DisplayName = GetValue(row, "displayname", "name"),
            OnPremisesSamAccountName = GetValue(row, "onpremisessamaccountname", "samaccountname"),
            AccountEnabled = ParseBoolean(GetValue(row, "accountenabled", "enabled", "isenabled")) ?? true,
            GroupNames = SplitGroups(GetValue(row, "groupnames", "groups", "memberof")),
        };
    }

    private static CyberArkAccount MapCyberArkIdentityUser(IReadOnlyDictionary<string, string> row)
    {
        var userName = GetValue(row, "username", "userprincipalname", "upn", "mail", "email", "loginname");
        var displayName = GetValue(row, "displayname", "name");
        var address = GetValue(row, "tenant", "directory", "source", "domain");
        var platformId = GetValue(row, "platform", "usertype", "profile");
        var groupNames = SplitGroups(GetValue(row, "groupnames", "groups", "memberof", "roles"));

        return new CyberArkAccount
        {
            Id = GetValue(row, "id", "userid", "user id") is { Length: > 0 } explicitId ? explicitId : BuildFallbackId(address, platformId, userName, displayName),
            UserName = userName,
            Address = address,
            PlatformId = platformId,
            SafeName = GetValue(row, "directory", "tenant"),
            AccountName = displayName,
            IsCpmDisabled = false,
            GroupNames = groupNames,
        };
    }

    private static IReadOnlyDictionary<string, string> BuildRow(IReadOnlyList<string> headers, IReadOnlyList<string> values)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < headers.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(headers[index]))
            {
                continue;
            }

            row[headers[index]] = index < values.Count ? values[index].Trim() : string.Empty;
        }

        return row;
    }

    private static List<string> ParseDelimitedLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '\"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '\"')
                {
                    current.Append('\"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == delimiter && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        values.Add(current.ToString());
        return values;
    }

    private static char DetectDelimiter(string headerLine)
    {
        var commaCount = headerLine.Count(character => character == ',');
        var semicolonCount = headerLine.Count(character => character == ';');
        return semicolonCount > commaCount ? ';' : ',';
    }

    private static string NormalizeHeader(string header)
    {
        return new string(header
            .Trim()
            .ToLowerInvariant()
            .Where(character => !char.IsWhiteSpace(character) && character is not '_' and not '-')
            .ToArray());
    }

    private static string GetValue(IReadOnlyDictionary<string, string> row, params string[] candidateHeaders)
    {
        foreach (var header in candidateHeaders.Select(NormalizeHeader))
        {
            if (row.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static IReadOnlyCollection<string> SplitGroups(string rawGroups)
    {
        return rawGroups
            .Split(["|", ";", ",", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool? ParseBoolean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "yes" or "y" or "1" or "enabled" or "active" => true,
            "false" or "no" or "n" or "0" or "disabled" or "inactive" => false,
            _ => bool.TryParse(value, out var parsed) ? parsed : null,
        };
    }

    private static bool? InvertBoolean(bool? value)
    {
        return value.HasValue ? !value.Value : null;
    }

    private static string BuildFallbackId(params string[] values)
    {
        var fallback = string.Join("|", values.Where(value => !string.IsNullOrWhiteSpace(value))).Trim('|');
        return string.IsNullOrWhiteSpace(fallback) ? Guid.NewGuid().ToString("N") : fallback;
    }
}
