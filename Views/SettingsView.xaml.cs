using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace JustBedwars.Views
{
    public sealed partial class SettingsView : Page
    {
        private const string ApiKeySettingName = "HypixelApiKey";
        private const string LogFileSettingName = "LogFilePath";
        private readonly SettingsService _settingsService;

        public SettingsView()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            LoadApiKey();
            LoadLogFilePath();
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SaveApiKey();
        }

        private void LoadApiKey()
        {
            var apiKey = _settingsService.GetValue(ApiKeySettingName);
            if (apiKey != null)
            {
                ApiKeyPasswordBox.Password = (string)apiKey;
            }
        }

        private void SaveApiKey()
        {
            _settingsService.SetValue(ApiKeySettingName, ApiKeyPasswordBox.Password);
        }

        private async void SelectLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".log");

            var hwnd = WindowNative.GetWindowHandle(App.Window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                LogFilePathTextBox.Text = file.Path;
                SaveLogFilePath(file.Path);
            }
        }

        private void LoadLogFilePath()
        {
            var logFilePath = _settingsService.GetValue(LogFileSettingName);
            if (logFilePath != null)
            {
                LogFilePathTextBox.Text = (string)logFilePath;
            }
        }

        private void SaveLogFilePath(string path)
        {
            _settingsService.SetValue(LogFileSettingName, path);
        }

        private void OpenDebugWindow_Click(object sender, RoutedEventArgs e)
        {
            var debugWindow = new DebugWindow();
            debugWindow.Activate();
        }
    }
}