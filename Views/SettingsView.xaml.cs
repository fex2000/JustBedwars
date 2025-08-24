using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace JustBedwars.Views
{
    public sealed partial class SettingsView : Page
    {
        private const string ApiKeySettingName = "HypixelApiKey";
        private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        private const string LogFileSettingName = "LogFilePath";

        public SettingsView()
        {
            InitializeComponent();
            LoadApiKey();
            LoadLogFilePath();
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SaveApiKey();
        }

        private void LoadApiKey()
        {
            if (_localSettings.Values.TryGetValue(ApiKeySettingName, out object? apiKey))
            {
                ApiKeyPasswordBox.Password = (string)apiKey!;
            }
        }

        private void SaveApiKey()
        {
            _localSettings.Values[ApiKeySettingName] = ApiKeyPasswordBox.Password;
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
            if (_localSettings.Values.TryGetValue(LogFileSettingName, out object? logFilePath))
            {
                LogFilePathTextBox.Text = (string)logFilePath!;
            }
        }

        private void SaveLogFilePath(string path)
        {
            _localSettings.Values[LogFileSettingName] = path;
        }

        private void OpenDebugWindow_Click(object sender, RoutedEventArgs e)
        {
            var debugWindow = new DebugWindow();
            debugWindow.Activate();
        }
    }
}