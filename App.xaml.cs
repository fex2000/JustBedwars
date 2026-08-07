using System;
using System.IO;
using JustBedwars.Services;
using JustBedwars.Views;
using Microsoft.UI.Xaml;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly SettingsService _settingsService;
    public AptabaseClient AptabaseClient;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
        AptabaseClient = new AptabaseClient("https://stat.fexei.at", "A-SH-5040461685", _settingsService);
        UnhandledException += App_UnhandledException;

        // Ensure file logging is enabled if the setting is on
        var saveDebugLogs = _settingsService.GetValue("SaveDebugLogs") as bool? ?? true;
        var enableLogHistory = _settingsService.GetValue("EnableLogHistory") as bool? ?? true;
        if (saveDebugLogs)
        {
            var logFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JustBedwars", "logs");
            var logFilePath = Path.Combine(logFolderPath, "latest.log");
            DebugService.Instance.SetFileLogging(true, logFilePath, enableLogHistory);
        }
    }

    public static Window? Window { get; private set; }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        DebugService.Instance.Log($"[UnhandledException] {e.Exception}");
        _ = AptabaseClient.TrackEvent("UnhandledException", e.Exception);
        e.Handled = true;
    }

    /// <summary>
    ///     Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        WelcomeWindow? welcomeWindow = null;
        if (_settingsService.GetValue("FirstLaunch") as bool? ?? true)
            try
            {
                welcomeWindow = new WelcomeWindow(_settingsService);
                await welcomeWindow.RunSetup();

                _ = AptabaseClient.TrackEvent("WelcomeFinished");
            }
            catch
            {
                // 
            }

        Window = new MainWindow(_settingsService, AptabaseClient);
        Window.Activate();

        _ = AptabaseClient.TrackEvent("AppLaunch");

        if (welcomeWindow is not null)
            welcomeWindow!.Close();
    }
}