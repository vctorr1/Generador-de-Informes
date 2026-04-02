using PrivilegedAuditSuite.Application.Services;
using PrivilegedAuditSuite.Headless.Services;
using PrivilegedAuditSuite.Infrastructure.Services;

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
var classifier = new CyberArkErrorClassifier();
var filter = new CyberArkAccountFilter();
var reconciliationService = new IdentityReconciliationService();

var runner = new HeadlessRunner(configurationStore, cyberArkApiService, entraIdService, classifier, filter, reconciliationService);
return await runner.RunAsync(args);
