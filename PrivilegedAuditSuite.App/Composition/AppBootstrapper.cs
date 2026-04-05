using System.Net.Http;
using PrivilegedAuditSuite.App.Services;
using PrivilegedAuditSuite.App.ViewModels;
using PrivilegedAuditSuite.Application.Interfaces;
using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Infrastructure.Services;

namespace PrivilegedAuditSuite.App.Composition;

public sealed class AppBootstrapper
{
    public MainWindow CreateShell()
    {
        var cyberArkHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3),
        };

        var graphHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3),
        };

        var configurationStore = new EncryptedJsonConfigurationStore(new DpapiSecretProtector());
        var cyberArkAuthenticationService = new CyberArkAuthenticationService(cyberArkHttpClient);
        var cyberArkApiService = new CyberArkApiService(cyberArkHttpClient, cyberArkAuthenticationService);
        var entraIdService = new EntraIdGraphService(graphHttpClient);
        IManualReportImportService manualReportImportService = new ManualReportImportService();
        IErrorSummaryExportService errorSummaryExportService = new ErrorSummaryExportService();
        IFilePickerService filePickerService = new WindowsFilePickerService();
        var classifier = new CyberArkErrorClassifier();
        var filter = new CyberArkAccountFilter();
        var serverExclusionParser = new ServerExclusionParser();
        var reconciliationService = new IdentityReconciliationService();

        var settingsViewModel = new SettingsViewModel(configurationStore);
        var cyberArkViewModel = new CyberArkAuditViewModel(cyberArkApiService, manualReportImportService, errorSummaryExportService, filePickerService, classifier, filter, serverExclusionParser, settingsViewModel);
        var identityViewModel = new IdentityReconciliationViewModel(entraIdService, manualReportImportService, filePickerService, reconciliationService, settingsViewModel, cyberArkViewModel);
        var mainViewModel = new MainViewModel(settingsViewModel, cyberArkViewModel, identityViewModel);

        return new MainWindow
        {
            DataContext = mainViewModel,
        };
    }
}
