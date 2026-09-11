using System.Windows;
using SUPSHADE.App.Services;
using SUPSHADE.App.Views;

namespace SUPSHADE.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = SettingsService.Load();

        // Gate: user must accept the TOS/EULA before the app can be used.
        if (!settings.TermsAccepted)
        {
            var tos = new TermsWindow();
            var result = tos.ShowDialog();

            if (result != true || !tos.Accepted)
            {
                // Declined or closed without accepting -> app cannot run.
                Shutdown();
                return;
            }

            settings.TermsAccepted = true;
            settings.TermsAcceptedVersion = TermsWindow.CurrentTermsVersion;
            SettingsService.Save(settings);
        }

        var main = new MainWindow();
        main.Show();
    }
}
