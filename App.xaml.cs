using JustBedwars.Services;
using Microsoft.UI.Xaml;
using System;
using System.IO;

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

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            this.UnhandledException += App_UnhandledException;

            // Ensure file logging is enabled if the setting is on
            var saveDebugLogs = _settingsService.GetValue("SaveDebugLogs");
            if (saveDebugLogs != null && (bool)saveDebugLogs)
            {
                string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars", "debug.log");
                DebugService.Instance.SetFileLogging(true, logPath);
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            DebugService.Instance.Log($"[UnhandledException] {e.Exception}");
            e.Handled = true; // Optional: Mark the exception as handled
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow(_settingsService);
            _window.Activate();
        }
    }
}