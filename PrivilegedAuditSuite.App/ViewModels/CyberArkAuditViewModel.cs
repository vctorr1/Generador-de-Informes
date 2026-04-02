using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PrivilegedAuditSuite.App.Commands;
using PrivilegedAuditSuite.App.Services;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class CyberArkAuditViewModel : ObservableObject
{
    private const string InputFileFilter = "Supported files|*.csv;*.xlsx;*.xlsm|CSV files|*.csv|Excel files|*.xlsx;*.xlsm";
    private const string OutputFileFilter = "Excel workbook|*.xlsx|CSV file|*.csv";

    private readonly ICyberArkApiService _cyberArkApiService;
    private readonly IManualReportImportService _manualReportImportService;
    private readonly IErrorSummaryExportService _errorSummaryExportService;
    private readonly IFilePickerService _filePickerService;
    private readonly CyberArkErrorClassifier _errorClassifier;
    private readonly CyberArkAccountFilter _accountFilter;
    private readonly SettingsViewModel _settingsViewModel;
    private IReadOnlyList<CyberArkAccount> _loadedAccounts = [];
    private string _searchText = string.Empty;
    private int _pageSize = 200;
    private string _sourceDescription = "No source loaded.";
    private string _manualFilePath = string.Empty;
    private string _selectionSummary = "Load CyberArk accounts to select platforms and safes.";
    private int _loadedAccountCount;
    private bool _isAuditReadyToRun;
    private ErrorCategoryOptionViewModel? _selectedErrorCategory;

    public CyberArkAuditViewModel(
        ICyberArkApiService cyberArkApiService,
        IManualReportImportService manualReportImportService,
        IErrorSummaryExportService errorSummaryExportService,
        IFilePickerService filePickerService,
        CyberArkErrorClassifier errorClassifier,
        CyberArkAccountFilter accountFilter,
        SettingsViewModel settingsViewModel)
    {
        _cyberArkApiService = cyberArkApiService;
        _manualReportImportService = manualReportImportService;
        _errorSummaryExportService = errorSummaryExportService;
        _filePickerService = filePickerService;
        _errorClassifier = errorClassifier;
        _accountFilter = accountFilter;
        _settingsViewModel = settingsViewModel;

        SyncNowCommand = new AsyncRelayCommand(SyncNowAsync);
        ImportAccountsFileCommand = new AsyncRelayCommand(ImportAccountsFileAsync);
        RunAuditCommand = new AsyncRelayCommand(RunAuditAsync, () => _loadedAccounts.Count > 0);
        ExportErrorSummaryCommand = new AsyncRelayCommand(ExportErrorSummaryAsync, () => ErrorSummary.Count > 0);
        SelectAllPlatformsCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailablePlatforms, true));
        ClearPlatformSelectionCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailablePlatforms, false));
        SelectAllSafesCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailableSafes, true));
        ClearSafeSelectionCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailableSafes, false));
        SelectAllErrorCategoriesCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailableErrorCategories, true));
        ClearErrorCategorySelectionCommand = new AsyncRelayCommand(() => SetAllSelectionsAsync(AvailableErrorCategories, false));
        MoveErrorCategoryUpCommand = new AsyncRelayCommand(() => MoveErrorCategoryAsync(-1), CanMoveErrorCategoryUp);
        MoveErrorCategoryDownCommand = new AsyncRelayCommand(() => MoveErrorCategoryAsync(1), CanMoveErrorCategoryDown);

        InitializeErrorCategories();
    }

    public event Action<string>? LogGenerated;

    public event Action<OperationProgress>? ProgressChanged;

    public event Action<bool>? BusyStateChanged;

    public ObservableCollection<CyberArkAccount> Accounts { get; } = [];

    public ObservableCollection<ErrorSummaryRow> ErrorSummary { get; } = [];

    public ObservableCollection<SelectionOptionViewModel> AvailablePlatforms { get; } = [];

    public ObservableCollection<SelectionOptionViewModel> AvailableSafes { get; } = [];

    public ObservableCollection<ErrorCategoryOptionViewModel> AvailableErrorCategories { get; } = [];

    public ICommand SyncNowCommand { get; }

    public ICommand ImportAccountsFileCommand { get; }

    public ICommand RunAuditCommand { get; }

    public ICommand ExportErrorSummaryCommand { get; }

    public ICommand SelectAllPlatformsCommand { get; }

    public ICommand ClearPlatformSelectionCommand { get; }

    public ICommand SelectAllSafesCommand { get; }

    public ICommand ClearSafeSelectionCommand { get; }

    public ICommand SelectAllErrorCategoriesCommand { get; }

    public ICommand ClearErrorCategorySelectionCommand { get; }

    public ICommand MoveErrorCategoryUpCommand { get; }

    public ICommand MoveErrorCategoryDownCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public string SourceDescription
    {
        get => _sourceDescription;
        private set => SetProperty(ref _sourceDescription, value);
    }

    public string ManualFilePath
    {
        get => _manualFilePath;
        private set => SetProperty(ref _manualFilePath, value);
    }

    public int LoadedAccountCount
    {
        get => _loadedAccountCount;
        private set => SetProperty(ref _loadedAccountCount, value);
    }

    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => SetProperty(ref _selectionSummary, value);
    }

    public ErrorCategoryOptionViewModel? SelectedErrorCategory
    {
        get => _selectedErrorCategory;
        set
        {
            if (SetProperty(ref _selectedErrorCategory, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public IReadOnlyList<CyberArkAccount> CurrentAccounts => Accounts.ToList();

    private async Task ImportAccountsFileAsync()
    {
        var filePath = _filePickerService.PickFile("Select CyberArk Accounts File", InputFileFilter);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Importing CyberArk accounts file...", PercentComplete = 10d });
            var importedAccounts = await _manualReportImportService.ImportCyberArkAccountsAsync(filePath, CancellationToken.None);
            ManualFilePath = filePath;
            LoadAccounts(importedAccounts, $"Manual file: {Path.GetFileName(filePath)}");
            ProgressChanged?.Invoke(new OperationProgress { Message = $"Imported {LoadedAccountCount} CyberArk accounts from file. Review filters and run the audit.", PercentComplete = 100d });
            LogGenerated?.Invoke($"Imported {LoadedAccountCount} CyberArk accounts from '{filePath}'. Audit is ready to run.");
        }
        catch (Exception exception)
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "CyberArk file import failed.", PercentComplete = 0d });
            LogGenerated?.Invoke($"CyberArk file import failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private async Task SyncNowAsync()
    {
        var credentials = _settingsViewModel.GetCyberArkCredentials();
        if (string.IsNullOrWhiteSpace(credentials.BaseUrl) ||
            string.IsNullOrWhiteSpace(credentials.Username) ||
            string.IsNullOrWhiteSpace(credentials.Password))
        {
            LogGenerated?.Invoke("CyberArk sync skipped because base URL, username or password is missing.");
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Authenticating against CyberArk PVWA...", PercentComplete = 5d });
            var session = await _cyberArkApiService.LoginAsync(credentials, CancellationToken.None);

            var pageProgress = new Progress<OperationProgress>(progress =>
            {
                ProgressChanged?.Invoke(progress with { PercentComplete = Math.Min(95d, 10d + progress.PercentComplete * 0.85d) });
            });

            var accounts = await _cyberArkApiService.GetAccountsAsync(
                credentials,
                session,
                new CyberArkAccountQuery
                {
                    SearchText = SearchText,
                    PageSize = PageSize,
                },
                pageProgress,
                CancellationToken.None);

            ManualFilePath = string.Empty;
            LoadAccounts(accounts, "CyberArk PVWA API");
            ProgressChanged?.Invoke(new OperationProgress
            {
                Message = $"CyberArk sync completed. {LoadedAccountCount} accounts loaded. Review filters and run the audit.",
                PercentComplete = 100d,
                CurrentItem = LoadedAccountCount,
                TotalItems = LoadedAccountCount,
            });

            LogGenerated?.Invoke($"CyberArk sync completed. Loaded {LoadedAccountCount} accounts from API. Audit is ready to run.");
        }
        catch (Exception exception)
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "CyberArk sync failed.", PercentComplete = 0d });
            LogGenerated?.Invoke($"CyberArk sync failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private Task RunAuditAsync()
    {
        if (_loadedAccounts.Count == 0)
        {
            LogGenerated?.Invoke("Run audit skipped because no CyberArk source has been loaded.");
            return Task.CompletedTask;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            ProgressChanged?.Invoke(new OperationProgress
            {
                Message = "Applying CyberArk filters and building the error summary...",
                PercentComplete = 30d,
                CurrentItem = 0,
                TotalItems = LoadedAccountCount,
            });

            RefreshVisibleAccountsCore();
            _isAuditReadyToRun = false;
            UpdateSelectionSummary();
            RaiseCommandStates();

            ProgressChanged?.Invoke(new OperationProgress
            {
                Message = $"CyberArk audit completed. {Accounts.Count} accounts are in scope.",
                PercentComplete = 100d,
                CurrentItem = Accounts.Count,
                TotalItems = LoadedAccountCount,
            });

            LogGenerated?.Invoke($"CyberArk audit completed. {Accounts.Count} accounts matched the selected filters.");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }

        return Task.CompletedTask;
    }

    private async Task ExportErrorSummaryAsync()
    {
        if (ErrorSummary.Count == 0)
        {
            LogGenerated?.Invoke("Error summary export skipped because the audit has not been run yet.");
            return;
        }

        var filePath = _filePickerService.PickSaveFile(
            "Export Error Summary",
            OutputFileFilter,
            $"CyberArk-Error-Summary-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        BusyStateChanged?.Invoke(true);

        try
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Exporting error summary...", PercentComplete = 40d });
            await _errorSummaryExportService.ExportAsync(filePath, ErrorSummary.ToList(), CancellationToken.None);
            ProgressChanged?.Invoke(new OperationProgress { Message = $"Error summary exported to {Path.GetFileName(filePath)}.", PercentComplete = 100d });
            LogGenerated?.Invoke($"Error summary exported to '{filePath}'.");
        }
        catch (Exception exception)
        {
            ProgressChanged?.Invoke(new OperationProgress { Message = "Error summary export failed.", PercentComplete = 0d });
            LogGenerated?.Invoke($"Error summary export failed: {exception.Message}");
        }
        finally
        {
            BusyStateChanged?.Invoke(false);
        }
    }

    private Task SetAllSelectionsAsync(IEnumerable<SelectionOptionViewModel> options, bool isSelected)
    {
        foreach (var option in options.ToList())
        {
            option.IsSelected = isSelected;
        }

        UpdateSelectionSummary();
        return Task.CompletedTask;
    }

    private Task SetAllSelectionsAsync(IEnumerable<ErrorCategoryOptionViewModel> options, bool isSelected)
    {
        foreach (var option in options.ToList())
        {
            option.IsSelected = isSelected;
        }

        UpdateSelectionSummary();
        RaiseCommandStates();
        return Task.CompletedTask;
    }

    private Task MoveErrorCategoryAsync(int offset)
    {
        if (SelectedErrorCategory is null)
        {
            return Task.CompletedTask;
        }

        var currentIndex = AvailableErrorCategories.IndexOf(SelectedErrorCategory);
        var targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= AvailableErrorCategories.Count)
        {
            return Task.CompletedTask;
        }

        AvailableErrorCategories.Move(currentIndex, targetIndex);
        SelectedErrorCategory = AvailableErrorCategories[targetIndex];
        UpdateSelectionSummary();
        RaiseCommandStates();
        return Task.CompletedTask;
    }

    private bool CanMoveErrorCategoryUp()
    {
        return SelectedErrorCategory is not null && AvailableErrorCategories.IndexOf(SelectedErrorCategory) > 0;
    }

    private bool CanMoveErrorCategoryDown()
    {
        return SelectedErrorCategory is not null && AvailableErrorCategories.IndexOf(SelectedErrorCategory) >= 0 && AvailableErrorCategories.IndexOf(SelectedErrorCategory) < AvailableErrorCategories.Count - 1;
    }

    private void LoadAccounts(IReadOnlyList<CyberArkAccount> accounts, string sourceDescription)
    {
        _loadedAccounts = accounts;
        LoadedAccountCount = accounts.Count;
        SourceDescription = sourceDescription;
        _isAuditReadyToRun = accounts.Count > 0;

        PopulateSelectionOptions(AvailablePlatforms, accounts, account => account.PlatformId, "No platform");
        PopulateSelectionOptions(AvailableSafes, accounts, account => account.SafeName, "No safe");
        RefreshErrorCategoryOptions(accounts);
        ResetErrorCounts();
        Accounts.Clear();
        ErrorSummary.Clear();
        UpdateSelectionSummary();
        RaiseCommandStates();
    }

    private void PopulateSelectionOptions(
        ObservableCollection<SelectionOptionViewModel> target,
        IEnumerable<CyberArkAccount> accounts,
        Func<CyberArkAccount, string> selector,
        string emptyLabel)
    {
        foreach (var existingOption in target)
        {
            existingOption.SelectionChanged -= UpdateSelectionSummary;
        }

        var options = accounts
            .GroupBy(account =>
            {
                var value = selector(account);
                return string.IsNullOrWhiteSpace(value) ? emptyLabel : value.Trim();
            }, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SelectionOptionViewModel(group.Key, group.Count()))
            .ToList();

        ReplaceItems(target, options);

        foreach (var option in target)
        {
            option.SelectionChanged += UpdateSelectionSummary;
        }
    }

    private IReadOnlyList<CyberArkAccount> GetPreFilteredAccounts(IEnumerable<CyberArkAccount> accounts)
    {
        return _accountFilter.Apply(accounts, _settingsViewModel.GetCyberArkFilterOptions());
    }

    private void RefreshVisibleAccountsCore()
    {
        var preFilteredAccounts = GetPreFilteredAccounts(_loadedAccounts);
        var selectedPlatforms = AvailablePlatforms.Where(option => option.IsSelected).Select(option => option.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedSafes = AvailableSafes.Where(option => option.IsSelected).Select(option => option.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasPlatformOptions = AvailablePlatforms.Count > 0;
        var hasSafeOptions = AvailableSafes.Count > 0;

        var filteredAccounts = preFilteredAccounts
            .Where(account => !hasPlatformOptions || selectedPlatforms.Contains(NormalizeForSelection(account.PlatformId, "No platform")))
            .Where(account => !hasSafeOptions || selectedSafes.Contains(NormalizeForSelection(account.SafeName, "No safe")))
            .ToList();

        ReplaceItems(Accounts, filteredAccounts);
        ApplyOrderedErrorSummary(filteredAccounts);
    }

    private void ApplyOrderedErrorSummary(IReadOnlyList<CyberArkAccount> filteredAccounts)
    {
        var orderedRows = AvailableErrorCategories
            .Where(option => option.IsSelected)
            .Select(option => BuildErrorSummaryRow(option, filteredAccounts))
            .ToList();

        foreach (var row in orderedRows)
        {
            var option = AvailableErrorCategories.First(item => item.DisplayName == row.Label);
            option.Count = row.Count;
        }

        foreach (var option in AvailableErrorCategories.Except(AvailableErrorCategories.Where(item => item.IsSelected)))
        {
            option.Count = CountMatches(option, filteredAccounts);
        }

        ReplaceItems(ErrorSummary, orderedRows);
    }

    private void InitializeErrorCategories()
    {
        var categories = Enum.GetValues<ErrorCategory>()
            .Where(category => category is not ErrorCategory.NoError and not ErrorCategory.Unclassified)
            .Select(category => new ErrorCategoryOptionViewModel(category, category.ToString()))
            .ToList();
        ApplyErrorCategoryOptions(categories);
    }

    private void OnErrorCategorySelectionChanged()
    {
        if (_loadedAccounts.Count > 0)
        {
            _isAuditReadyToRun = true;
        }

        UpdateSelectionSummary();
        RaiseCommandStates();
    }

    private void ResetErrorCounts()
    {
        foreach (var option in AvailableErrorCategories)
        {
            option.Count = 0;
        }
    }

    private void UpdateSelectionSummary()
    {
        var selectedPlatformCount = AvailablePlatforms.Count(option => option.IsSelected);
        var selectedSafeCount = AvailableSafes.Count(option => option.IsSelected);
        var selectedErrorCount = AvailableErrorCategories.Count(option => option.IsSelected);

        SelectionSummary = _loadedAccounts.Count == 0
            ? "Load CyberArk accounts to select platforms, safes and output error categories."
            : _isAuditReadyToRun
                ? $"Loaded {LoadedAccountCount} accounts. Platforms selected: {selectedPlatformCount}/{AvailablePlatforms.Count}. Safes selected: {selectedSafeCount}/{AvailableSafes.Count}. Error categories selected: {selectedErrorCount}/{AvailableErrorCategories.Count}. Run the audit to refresh the report."
                : $"Loaded {LoadedAccountCount} accounts. Visible after CPM/rule filter and selection: {Accounts.Count}. Error rows prepared for export: {ErrorSummary.Count}.";
    }

    private void RaiseCommandStates()
    {
        if (RunAuditCommand is AsyncRelayCommand runAuditCommand)
        {
            runAuditCommand.RaiseCanExecuteChanged();
        }

        if (ExportErrorSummaryCommand is AsyncRelayCommand exportErrorSummaryCommand)
        {
            exportErrorSummaryCommand.RaiseCanExecuteChanged();
        }

        if (MoveErrorCategoryUpCommand is AsyncRelayCommand moveErrorCategoryUpCommand)
        {
            moveErrorCategoryUpCommand.RaiseCanExecuteChanged();
        }

        if (MoveErrorCategoryDownCommand is AsyncRelayCommand moveErrorCategoryDownCommand)
        {
            moveErrorCategoryDownCommand.RaiseCanExecuteChanged();
        }
    }

    private static string NormalizeForSelection(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void RefreshErrorCategoryOptions(IEnumerable<CyberArkAccount> accounts)
    {
        var currentSelections = AvailableErrorCategories.ToDictionary(
            option => BuildOptionKey(option.Category, option.MatchText),
            option => option.IsSelected);

        var currentOrder = AvailableErrorCategories
            .Select(option => BuildOptionKey(option.Category, option.MatchText))
            .ToList();

        var baseOptions = Enum.GetValues<ErrorCategory>()
            .Where(category => category is not ErrorCategory.NoError and not ErrorCategory.Unclassified)
            .Select(category => new ErrorCategoryOptionViewModel(
                category,
                category.ToString(),
                null,
                currentSelections.GetValueOrDefault(BuildOptionKey(category, null), true)))
            .ToList();

        var extraOptions = accounts
            .Select(account => account.LastCpmError)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!)
            .Where(message => _errorClassifier.Classify(message).Category == ErrorCategory.Unclassified)
            .GroupBy(message => _errorClassifier.GetErrorSignature(message), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ErrorCategoryOptionViewModel(
                ErrorCategory.Unclassified,
                group.Key,
                group.Key,
                currentSelections.GetValueOrDefault(BuildOptionKey(ErrorCategory.Unclassified, group.Key), true)))
            .ToList();

        var orderedOptions = baseOptions
            .Concat(extraOptions)
            .OrderBy(option =>
            {
                var key = BuildOptionKey(option.Category, option.MatchText);
                var index = currentOrder.IndexOf(key);
                return index >= 0 ? index : int.MaxValue;
            })
            .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ApplyErrorCategoryOptions(orderedOptions);
    }

    private void ApplyErrorCategoryOptions(IReadOnlyList<ErrorCategoryOptionViewModel> options)
    {
        foreach (var option in AvailableErrorCategories)
        {
            option.SelectionChanged -= OnErrorCategorySelectionChanged;
        }

        ReplaceItems(AvailableErrorCategories, options);

        foreach (var option in AvailableErrorCategories)
        {
            option.SelectionChanged += OnErrorCategorySelectionChanged;
        }

        SelectedErrorCategory = AvailableErrorCategories.FirstOrDefault();
    }

    private ErrorSummaryRow BuildErrorSummaryRow(ErrorCategoryOptionViewModel option, IReadOnlyList<CyberArkAccount> filteredAccounts)
    {
        var count = CountMatches(option, filteredAccounts);
        var sampleMessage = FindSampleMessage(option, filteredAccounts);

        return new ErrorSummaryRow
        {
            Label = option.DisplayName,
            Category = option.Category,
            Count = count,
            SampleMessage = sampleMessage,
        };
    }

    private int CountMatches(ErrorCategoryOptionViewModel option, IReadOnlyList<CyberArkAccount> filteredAccounts)
    {
        return filteredAccounts.Count(account => MatchesOption(option, account.LastCpmError));
    }

    private string FindSampleMessage(ErrorCategoryOptionViewModel option, IReadOnlyList<CyberArkAccount> filteredAccounts)
    {
        return filteredAccounts
            .Select(account => account.LastCpmError)
            .FirstOrDefault(message => MatchesOption(option, message)) ?? string.Empty;
    }

    private bool MatchesOption(ErrorCategoryOptionViewModel option, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (option.IsCustom)
        {
            return string.Equals(_errorClassifier.GetErrorSignature(message), option.MatchText, StringComparison.OrdinalIgnoreCase);
        }

        return _errorClassifier.Classify(message).Category == option.Category;
    }

    private static string BuildOptionKey(ErrorCategory category, string? matchText)
    {
        return $"{category}|{matchText ?? string.Empty}";
    }
}
