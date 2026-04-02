using System.Windows;
using PrivilegedAuditSuite.App.Composition;

namespace PrivilegedAuditSuite.App;

public partial class App : System.Windows.Application
{
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _bootstrapper = new AppBootstrapper();
        MainWindow = _bootstrapper.CreateShell();
        MainWindow.Show();
    }
}
