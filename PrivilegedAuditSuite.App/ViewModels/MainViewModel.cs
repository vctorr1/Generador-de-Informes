using System.Collections.ObjectModel;
using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private int _activeOperations;
    private bool _isBusy;
    private double _progressValue;
    private string _statusMessage = "Ready";

    public MainViewModel(
        SettingsViewModel settings,
        CyberArkAuditViewModel cyberArk,
        IdentityReconciliationViewModel identity)
    {
        Settings = settings;
        CyberArk = cyberArk;
        Identity = identity;

        settings.LogGenerated += AddLog;
        cyberArk.LogGenerated += AddLog;
        identity.LogGenerated += AddLog;
        cyberArk.ProgressChanged += ApplyProgress;
        identity.ProgressChanged += ApplyProgress;
        cyberArk.BusyStateChanged += UpdateBusyState;
        identity.BusyStateChanged += UpdateBusyState;
    }

    public SettingsViewModel Settings { get; }

    public CyberArkAuditViewModel CyberArk { get; }

    public IdentityReconciliationViewModel Identity { get; }

    public ObservableCollection<string> Logs { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void ApplyProgress(OperationProgress progress)
    {
        ProgressValue = progress.PercentComplete;
        StatusMessage = progress.Message;
    }

    private void UpdateBusyState(bool isBusy)
    {
        _activeOperations = isBusy ? _activeOperations + 1 : Math.Max(0, _activeOperations - 1);
        IsBusy = _activeOperations > 0;

        if (!IsBusy && ProgressValue >= 100d)
        {
            ProgressValue = 0d;
        }
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
    }
}
