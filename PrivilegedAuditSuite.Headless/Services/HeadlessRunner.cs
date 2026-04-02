using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.Headless.Services;

public sealed class HeadlessRunner(
    ISecureConfigurationStore configurationStore,
    ICyberArkApiService cyberArkApiService,
    IEntraIdService entraIdService,
    CyberArkErrorClassifier errorClassifier,
    CyberArkAccountFilter accountFilter,
    IdentityReconciliationService reconciliationService)
{
    public async Task<int> RunAsync(string[] args)
    {
        var taskName = GetOption(args, "--task") ?? "audit";
        var configurationPath = GetOption(args, "--config") ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrivilegedAuditSuite",
            "appsettings.secure");

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 0;
        }

        var configuration = await configurationStore.LoadAsync(configurationPath, CancellationToken.None);
        if (configuration is null)
        {
            Console.Error.WriteLine($"Configuration not found: {configurationPath}");
            return 1;
        }

        return taskName.Trim().ToLowerInvariant() switch
        {
            "audit" => await RunAuditAsync(configuration),
            "reconcile" => await RunReconciliationAsync(configuration),
            _ => PrintInvalidTask(taskName),
        };
    }

    private async Task<int> RunAuditAsync(AppConfiguration configuration)
    {
        var credentials = new CyberArkCredentials
        {
            BaseUrl = configuration.CyberArk.BaseUrl,
            AuthenticationType = configuration.CyberArk.AuthenticationType,
            Username = configuration.CyberArk.Username,
            Password = configuration.CyberArk.Password,
        };

        var session = await cyberArkApiService.LoginAsync(credentials, CancellationToken.None);
        var accounts = await cyberArkApiService.GetAccountsAsync(
            credentials,
            session,
            new CyberArkAccountQuery(),
            new Progress<OperationProgress>(progress => Console.WriteLine($"{progress.PercentComplete,6:N1}%  {progress.Message}")),
            CancellationToken.None);

        var filteredAccounts = accountFilter.Apply(accounts, configuration.AuditFilters);
        var summary = errorClassifier.Summarize(filteredAccounts.Select(item => item.LastCpmError));

        Console.WriteLine($"CyberArk accounts returned: {accounts.Count}");
        Console.WriteLine($"CyberArk accounts after filter: {filteredAccounts.Count}");
        foreach (var row in summary)
        {
            Console.WriteLine($"{row.Category,-28} {row.Count,5}  {row.SampleMessage}");
        }

        return 0;
    }

    private async Task<int> RunReconciliationAsync(AppConfiguration configuration)
    {
        var credentials = new CyberArkCredentials
        {
            BaseUrl = configuration.CyberArk.BaseUrl,
            AuthenticationType = configuration.CyberArk.AuthenticationType,
            Username = configuration.CyberArk.Username,
            Password = configuration.CyberArk.Password,
        };

        var session = await cyberArkApiService.LoginAsync(credentials, CancellationToken.None);
        var accounts = await cyberArkApiService.GetAccountsAsync(credentials, session, new CyberArkAccountQuery(), null, CancellationToken.None);
        var filteredAccounts = accountFilter.Apply(accounts, configuration.AuditFilters);
        var users = await entraIdService.GetUsersAsync(configuration.EntraId, includeGroups: true, null, CancellationToken.None);
        var result = reconciliationService.Reconcile(users, filteredAccounts, configuration.IdentityComparison);

        Console.WriteLine($"Matched: {result.MatchedAccounts}");
        Console.WriteLine($"Discrepancies: {result.Discrepancies.Count}");
        foreach (var discrepancy in result.Discrepancies.Take(20))
        {
            Console.WriteLine($"{discrepancy.Type,-24} {discrepancy.Identity}  {discrepancy.Details}");
        }

        return 0;
    }

    private static string? GetOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  PrivilegedAuditSuite.Headless --task audit --config <path>");
        Console.WriteLine("  PrivilegedAuditSuite.Headless --task reconcile --config <path>");
    }

    private static int PrintInvalidTask(string taskName)
    {
        Console.Error.WriteLine($"Unsupported task '{taskName}'. Use 'audit' or 'reconcile'.");
        return 1;
    }
}
