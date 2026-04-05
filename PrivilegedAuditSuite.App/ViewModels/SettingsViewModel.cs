using System.IO;
using System.Windows.Input;
using PrivilegedAuditSuite.App.Commands;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISecureConfigurationStore _configurationStore;
    private string _configurationFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PrivilegedAuditSuite",
        "appsettings.secure");
    private string _cyberArkBaseUrl = string.Empty;
    private string _cyberArkAuthenticationType = "cyberark";
    private string _cyberArkUsername = string.Empty;
    private string _cyberArkPassword = string.Empty;
    private string _entraTenantId = string.Empty;
    private string _entraClientId = string.Empty;
    private string _entraClientSecret = string.Empty;
    private string _oneDriveWorkbookUrl = string.Empty;
    private string _worksheetName = "AuditSummary";
    private string _targetColumn = "A";
    private string _platformAllowList = string.Empty;
    private string _platformBlockList = string.Empty;
    private string _safeAllowList = string.Empty;
    private string _safeBlockList = string.Empty;
    private string _excludedServers = string.Empty;
    private string _groupMappings = string.Empty;
    private bool _ignoreDisabledEntraUsers = true;

    public SettingsViewModel(ISecureConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore;
        SaveConfigurationCommand = new AsyncRelayCommand(SaveConfigurationAsync);
        LoadConfigurationCommand = new AsyncRelayCommand(LoadConfigurationAsync);
    }

    public event Action<string>? LogGenerated;

    public ICommand SaveConfigurationCommand { get; }

    public ICommand LoadConfigurationCommand { get; }

    public IReadOnlyList<string> CyberArkAuthenticationTypes { get; } = ["cyberark", "ldap", "radius"];

    public string ConfigurationFilePath
    {
        get => _configurationFilePath;
        set => SetProperty(ref _configurationFilePath, value);
    }

    public string CyberArkBaseUrl
    {
        get => _cyberArkBaseUrl;
        set => SetProperty(ref _cyberArkBaseUrl, value);
    }

    public string CyberArkAuthenticationType
    {
        get => _cyberArkAuthenticationType;
        set => SetProperty(ref _cyberArkAuthenticationType, value);
    }

    public string CyberArkUsername
    {
        get => _cyberArkUsername;
        set => SetProperty(ref _cyberArkUsername, value);
    }

    public string CyberArkPassword
    {
        get => _cyberArkPassword;
        set => SetProperty(ref _cyberArkPassword, value);
    }

    public string EntraTenantId
    {
        get => _entraTenantId;
        set => SetProperty(ref _entraTenantId, value);
    }

    public string EntraClientId
    {
        get => _entraClientId;
        set => SetProperty(ref _entraClientId, value);
    }

    public string EntraClientSecret
    {
        get => _entraClientSecret;
        set => SetProperty(ref _entraClientSecret, value);
    }

    public string OneDriveWorkbookUrl
    {
        get => _oneDriveWorkbookUrl;
        set => SetProperty(ref _oneDriveWorkbookUrl, value);
    }

    public string WorksheetName
    {
        get => _worksheetName;
        set => SetProperty(ref _worksheetName, value);
    }

    public string TargetColumn
    {
        get => _targetColumn;
        set => SetProperty(ref _targetColumn, value);
    }

    public string PlatformAllowList
    {
        get => _platformAllowList;
        set => SetProperty(ref _platformAllowList, value);
    }

    public string PlatformBlockList
    {
        get => _platformBlockList;
        set => SetProperty(ref _platformBlockList, value);
    }

    public string SafeAllowList
    {
        get => _safeAllowList;
        set => SetProperty(ref _safeAllowList, value);
    }

    public string SafeBlockList
    {
        get => _safeBlockList;
        set => SetProperty(ref _safeBlockList, value);
    }

    public string ExcludedServers
    {
        get => _excludedServers;
        set => SetProperty(ref _excludedServers, value);
    }

    public string GroupMappings
    {
        get => _groupMappings;
        set => SetProperty(ref _groupMappings, value);
    }

    public bool IgnoreDisabledEntraUsers
    {
        get => _ignoreDisabledEntraUsers;
        set => SetProperty(ref _ignoreDisabledEntraUsers, value);
    }

    public CyberArkCredentials GetCyberArkCredentials()
    {
        return new CyberArkCredentials
        {
            BaseUrl = CyberArkBaseUrl,
            AuthenticationType = CyberArkAuthenticationType,
            Username = CyberArkUsername,
            Password = CyberArkPassword,
        };
    }

    public EntraIdConnectionSettings GetEntraSettings()
    {
        return new EntraIdConnectionSettings
        {
            TenantId = EntraTenantId,
            ClientId = EntraClientId,
            ClientSecret = EntraClientSecret,
        };
    }

    public CyberArkFilterOptions GetCyberArkFilterOptions()
    {
        return new CyberArkFilterOptions
        {
            OnlyCpmDisabled = true,
            PlatformAllowList = SplitValues(PlatformAllowList),
            PlatformBlockList = SplitValues(PlatformBlockList),
            SafeAllowList = SplitValues(SafeAllowList),
            SafeBlockList = SplitValues(SafeBlockList),
            ExcludedServers = SplitValues(ExcludedServers),
        };
    }

    public IdentityComparisonOptions GetIdentityComparisonOptions()
    {
        return new IdentityComparisonOptions
        {
            IgnoreDisabledEntraUsers = IgnoreDisabledEntraUsers,
            GroupMappings = ParseMappings(GroupMappings),
        };
    }

    private async Task SaveConfigurationAsync()
    {
        var configuration = new AppConfiguration
        {
            CyberArk = new CyberArkConnectionSettings
            {
                BaseUrl = CyberArkBaseUrl,
                AuthenticationType = CyberArkAuthenticationType,
                Username = CyberArkUsername,
                Password = CyberArkPassword,
            },
            EntraId = new EntraIdConnectionSettings
            {
                TenantId = EntraTenantId,
                ClientId = EntraClientId,
                ClientSecret = EntraClientSecret,
            },
            OneDriveExcel = new OneDriveExcelSettings
            {
                WorkbookUrl = OneDriveWorkbookUrl,
                WorksheetName = WorksheetName,
                TargetColumn = TargetColumn,
            },
            AuditFilters = GetCyberArkFilterOptions(),
            IdentityComparison = GetIdentityComparisonOptions(),
        };

        await _configurationStore.SaveAsync(ConfigurationFilePath, configuration, CancellationToken.None);
        LogGenerated?.Invoke($"Encrypted configuration saved to '{ConfigurationFilePath}'.");
    }

    private async Task LoadConfigurationAsync()
    {
        var configuration = await _configurationStore.LoadAsync(ConfigurationFilePath, CancellationToken.None);
        if (configuration is null)
        {
            LogGenerated?.Invoke($"No configuration was found at '{ConfigurationFilePath}'.");
            return;
        }

        CyberArkBaseUrl = configuration.CyberArk.BaseUrl;
        CyberArkAuthenticationType = configuration.CyberArk.AuthenticationType;
        CyberArkUsername = configuration.CyberArk.Username;
        CyberArkPassword = configuration.CyberArk.Password;
        EntraTenantId = configuration.EntraId.TenantId;
        EntraClientId = configuration.EntraId.ClientId;
        EntraClientSecret = configuration.EntraId.ClientSecret;
        OneDriveWorkbookUrl = configuration.OneDriveExcel.WorkbookUrl;
        WorksheetName = configuration.OneDriveExcel.WorksheetName;
        TargetColumn = configuration.OneDriveExcel.TargetColumn;
        PlatformAllowList = string.Join(", ", configuration.AuditFilters.PlatformAllowList);
        PlatformBlockList = string.Join(", ", configuration.AuditFilters.PlatformBlockList);
        SafeAllowList = string.Join(", ", configuration.AuditFilters.SafeAllowList);
        SafeBlockList = string.Join(", ", configuration.AuditFilters.SafeBlockList);
        ExcludedServers = string.Join(Environment.NewLine, configuration.AuditFilters.ExcludedServers);
        IgnoreDisabledEntraUsers = configuration.IdentityComparison.IgnoreDisabledEntraUsers;
        GroupMappings = string.Join(", ", configuration.IdentityComparison.GroupMappings.Select(item => $"{item.Key}={item.Value}"));

        LogGenerated?.Invoke($"Encrypted configuration loaded from '{ConfigurationFilePath}'.");
    }

    private static IReadOnlyCollection<string> SplitValues(string rawValue)
    {
        return rawValue
            .Split([",", ";", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ParseMappings(string rawMappings)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in rawMappings.Split([",", ";", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
            {
                mappings[parts[0]] = parts[1];
            }
        }

        return mappings;
    }
}
