using JustBedwars.Services;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;
using JustBedwars.Views;
using JustBedwars.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Window? _window;
        public static Window? Window { get { return _window; } }
        private readonly SettingsService _settingsService;
        public AptabaseClient AptabaseClient;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            AptabaseClient = new AptabaseClient("https://stat.fexei.at", "A-SH-5040461685", _settingsService);
            this.UnhandledException += App_UnhandledException;

            // Ensure file logging is enabled if the setting is on
            var saveDebugLogs = _settingsService.GetValue("SaveDebugLogs") as bool? ?? true;
            var enableLogHistory = _settingsService.GetValue("EnableLogHistory") as bool? ?? true;
            if (saveDebugLogs)
            {
                string logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars", "logs");
                string logFilePath = Path.Combine(logFolderPath, "latest.log");
                DebugService.Instance.SetFileLogging(true, logFilePath, enableLogHistory);
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            DebugService.Instance.Log($"[UnhandledException] {e.Exception}");
            _ = AptabaseClient.TrackEvent("UnhandledException", e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            WelcomeWindow? welcomeWindow = null;
            if (_settingsService.GetValue("FirstLaunch") as bool? ?? true)
                try
                {
                    welcomeWindow = new(_settingsService);
                    await welcomeWindow.RunSetup();

                    _ = AptabaseClient.TrackEvent("WelcomeFinished");
                }
                catch
                {
                    // 
                }
            _window = new MainWindow(_settingsService, AptabaseClient);
            _window.Activate();

            _ = AptabaseClient.TrackEvent("AppLaunch");

            if (welcomeWindow is not null)
                welcomeWindow!.Close();
        }
    }
}