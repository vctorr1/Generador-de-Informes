using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PrivilegedAuditSuite.App.Commands;
using PrivilegedAuditSuite.App.Services;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class IdentityReconciliationViewModel : ObservableObject
{
    private const string InputFileFilter = "Supported files|*.csv;*.xlsx;*.xlsm|CSV files|*.csv|Excel files|*.xlsx;*.xlsm";

    private readonly IEntraIdService _entraIdService;
    private readonly IManualReportImportService _manualReportImportService;
    private readonly IFilePickerService _filePickerService;
    private readonly IdentityReconciliationService _identityReconciliationService;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly CyberArkAuditViewModel _cyberArkAuditViewModel;
    private IReadOnlyList<EntraUser> _loadedEntraUsers = [];
    private IReadOnlyList<CyberArkAccount> _loadedCyberArkIdentityUsers = [];
    private string _summaryText = "Load Entra users and CyberArk Identity users or use API plus audit accounts.";
    private string _entraSourceDescription = "No Entra source loaded.";
    private string _cyberArkIdentitySourceDescription = "Using CyberArk Audit accounts unless a manual identity file is loaded.";
    private string _manualEntraFilePath = string.Empty;
    private string _manualCyberArkIdentityFilePath = string.Empty;

    public IdentityReconciliationViewModel(
        IEntraIdService entraIdService,
        IManualReportImportService manualReportImportService,
        IFilePickerService filePickerService,
        IdentityReconciliationService identityReconciliationService,
        SettingsViewModel settingsViewModel,
        CyberArkAuditViewModel cyberArkAuditViewModel)
    {
        _entraIdService = entraIdService;
        _manualReportImportService = manualReportImportService;
        _filePickerService = filePickerService;
        _identityReconciliationService = identityReconciliationService;
        _settingsViewModel = settingsViewModel;
        _cyberArkAuditViewModel = cyberArkAuditViewModel;
        RunReconciliationCommand = new AsyncRelayCommand(RunReconciliationAsync);
        ImportEntraFileCommand = new AsyncRelayCommand(ImportEntraFileAsync);
        ImportCyberArkIdentityFileCommand = new AsyncRelayCommand(ImportCyberArkIdentityFileAsync);
        LoadEntraFromApiCommand = new AsyncRelayCommand(LoadEntraFromApiAsync);
    }

    public event Action<string>? LogGenerated;

    public event Action<OperationProgress>? ProgressChanged;

    public event Action<bool>? BusyStateChanged;

    public ObservableCollection<EntraUser> EntraUsers { get; } = [];

    public ObservableCollection<CyberArkAccount> CyberArkIdentityUsers { get; } = [];

    public ObservableCollection<IdentityDiscrepancy> Discrepancies { get; } = [];

    public ICommand RunReconciliationCommand { get; }

    public ICommand ImportEntraFileCommand { get; }

    public ICommand ImportCyberArkIdentityFileCommand { get; }

    public ICommand LoadEntraFromApiCommand { get; }

    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    public string EntraSourceDescription
    {
        get => _entraSourceDescription;
        private set => SetProperty(ref _entraSourceDescription, value);
    }

    public string CyberArkIdentitySourceDescription
    {
        get => _cyberArkIdentitySourceDescription;
        private set => SetProperty(ref _cyberArkIdentitySourceDescription, value);
    }

    public string ManualEntraFilePath
    {
        get => _manualEntraFilePath;
        private set => SetProperty(ref _manualEntraFilePath, value);
    }

    public string ManualCyberArkIdentityFilePath
    {
        get => _manualCyberArkIdentityFilePath;
        private set => SetProperty(ref _manualCyberArkIdentityFilePath, value);
    }

    private async Task ImportEntraFileAsync()
    {
        var filePath = _filePickerService.PickFile("Select Entra ID Users File", InputFileFilter);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            var users = await _manualReportImportService.ImportEntraUsersAsync(filePath, CancellationToken.None);
            _loadedEntraUsers = users;
            ReplaceItems(EntraUsers, users);
            ManualEntraFilePath = filePath;
            EntraSourceDescription = $"Manual file: {Path.GetFileName(filePath)} ({users.Count} users)";
            SummaryText = "Manual Entra file loaded. Load CyberArk Identity file or run reconciliation against audit accounts.";
            LogGenerated?.Invoke($"Imported {users.Count} Entra users from '{filePath}'.");
        }
        catch (Exception exception)
        {
            LogGenerated?.Invoke($"Entra file import failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private async Task ImportCyberArkIdentityFileAsync()
    {
        var filePath = _filePickerService.PickFile("Select CyberArk Identity Users File", InputFileFilter);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            var users = await _manualReportImportService.ImportCyberArkIdentityUsersAsync(filePath, CancellationToken.None);
            _loadedCyberArkIdentityUsers = users;
            ReplaceItems(CyberArkIdentityUsers, users);
            ManualCyberArkIdentityFilePath = filePath;
            CyberArkIdentitySourceDescription = $"Manual file: {Path.GetFileName(filePath)} ({users.Count} users)";
            SummaryText = "Manual CyberArk Identity file loaded. Run reconciliation when both sources are ready.";
            LogGenerated?.Invoke($"Imported {users.Count} CyberArk Identity users from '{filePath}'.");
        }
        catch (Exception exception)
        {
            LogGenerated?.Invoke($"CyberArk Identity file import failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private async Task LoadEntraFromApiAsync()
    {
        var settings = _settingsViewModel.GetEntraSettings();
        if (string.IsNullOrWhiteSpace(settings.TenantId) ||
            string.IsNullOrWhiteSpace(settings.ClientId) ||
            string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            LogGenerated?.Invoke("Entra ID API load skipped because tenant/client credentials are missing.");
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            var progress = new Progress<OperationProgress>(update => ProgressChanged?.Invoke(update));
            var users = await _entraIdService.GetUsersAsync(settings, includeGroups: true, progress, CancellationToken.None);
            _loadedEntraUsers = users;
            ReplaceItems(EntraUsers, users);
            ManualEntraFilePath = string.Empty;
            EntraSourceDescription = $"Microsoft Graph API ({users.Count} users)";
            SummaryText = "Entra users loaded from API. Run reconciliation against audit accounts or a manual CyberArk Identity file.";
            LogGenerated?.Invoke($"Loaded {users.Count} Entra users from API.");
        }
        catch (Exception exception)
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Entra ID API load failed.", PercentComplete = 0d });
            LogGenerated?.Invoke($"Entra ID API load failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private async Task RunReconciliationAsync()
    {
        if (_loadedEntraUsers.Count == 0)
        {
            await LoadEntraFromApiAsync();
        }

        var sourceUsers = _loadedEntraUsers;
        var sourceCyberArkUsers = _loadedCyberArkIdentityUsers.Count > 0
            ? _loadedCyberArkIdentityUsers
            : _cyberArkAuditViewModel.CurrentAccounts;

        if (sourceUsers.Count == 0)
        {
            LogGenerated?.Invoke("Reconciliation skipped because no Entra users are loaded.");
            return;
        }

        if (sourceCyberArkUsers.Count == 0)
        {
            LogGenerated?.Invoke("Reconciliation skipped because no CyberArk Identity or CyberArk Audit users are loaded.");
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            if (_loadedCyberArkIdentityUsers.Count == 0)
            {
                ReplaceItems(CyberArkIdentityUsers, sourceCyberArkUsers);
                CyberArkIdentitySourceDescription = $"CyberArk Audit dataset ({sourceCyberArkUsers.Count} identities)";
            }

            var result = _identityReconciliationService.Reconcile(
                sourceUsers,
                sourceCyberArkUsers,
                _settingsViewModel.GetIdentityComparisonOptions());

            ReplaceItems(Discrepancies, result.Discrepancies);
            SummaryText = $"Matched {result.MatchedAccounts} identities. Found {result.Discrepancies.Count} discrepancies.";

            ProgressChanged?.Invoke(new OperationProgress
            {
                Message = SummaryText,
                CurrentItem = result.Discrepancies.Count,
                TotalItems = result.TotalEntraUsers,
                PercentComplete = 100d,
            });

            LogGenerated?.Invoke(
                $"Identity reconciliation completed using {EntraSourceDescription} against {CyberArkIdentitySourceDescription}. " +
                $"Matched {result.MatchedAccounts} users and found {result.Discrepancies.Count} discrepancies.");
        }
        catch (Exception exception)
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Identity reconciliation failed.", PercentComplete = 0d });
            LogGenerated?.Invoke($"Identity reconciliation failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }
}
