using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Diagnostics;

namespace JustBedwars.Views
{
    public sealed partial class SettingsView : Page
    {
        private const string ApiKeySettingName = "HypixelApiKey";
        private const string LogFileSettingName = "LogFilePath";
        private const string PlayerSortingSettingName = "PlayerSorting";
        private const string MediaPlayerSettingName = "ShowMediaPlayer";
        private const string SaveDebugLogsSettingName = "SaveDebugLogs";
        private const string EnableLogHistorySettingName = "EnableLogHistory";
        private const string EnableLogReaderLoggingSettingName = "EnableLogReaderLogging";
        private SettingsService _settingsService;
        public string Version { get; }

        public SettingsView()
        {
            InitializeComponent();
            Version = $"Version {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _settingsService = e.Parameter as SettingsService;
            LoadApiKey();
            LoadLogFilePath();
            LoadPlayerSorting();
            LoadMediaPlayerSetting();
            LoadSaveDebugLogsSetting();
            LoadEnableLogHistorySetting();
            LoadEnableLogReaderLoggingSetting();
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

        private void SortingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SavePlayerSorting();
        }

        private void LoadPlayerSorting()
        {
            var sorting = _settingsService.GetValue(PlayerSortingSettingName);
            if (sorting != null)
            {
                SortingComboBox.SelectedItem = sorting;
            }
            else
            {
                SortingComboBox.SelectedItem = "JustBedwars Score";
            }
        }

        private void SavePlayerSorting()
        {
            if (SortingComboBox.SelectedItem != null)
            {
                _settingsService.SetValue(PlayerSortingSettingName, SortingComboBox.SelectedItem.ToString());
            }
        }

        private void MediaPlayerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            _settingsService.SetValue(MediaPlayerSettingName, MediaPlayerToggle.IsOn);
        }

        private void LoadMediaPlayerSetting()
        {
            var showMediaPlayer = _settingsService.GetValue(MediaPlayerSettingName);
            if (showMediaPlayer != null)
            {
                MediaPlayerToggle.IsOn = (bool)showMediaPlayer;
            }
        }

        private void SaveDebugLogsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var isOn = ((ToggleSwitch)sender).IsOn;
            _settingsService.SetValue(SaveDebugLogsSettingName, isOn);
            UpdateDebugServiceLogging();
        }

        private void LoadSaveDebugLogsSetting()
        {
            var saveDebugLogs = _settingsService.GetValue(SaveDebugLogsSettingName);
            SaveDebugLogsToggle.IsOn = saveDebugLogs as bool? ?? true;
        }

        private void EnableLogHistoryToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var isOn = ((ToggleSwitch)sender).IsOn;
            _settingsService.SetValue(EnableLogHistorySettingName, isOn);
            UpdateDebugServiceLogging();
        }

        private void LoadEnableLogHistorySetting()
        {
            var enableLogHistory = _settingsService.GetValue(EnableLogHistorySettingName);
            EnableLogHistoryToggle.IsOn = enableLogHistory as bool? ?? true;
        }

        private void UpdateDebugServiceLogging()
        {
            var saveDebugLogs = _settingsService.GetValue(SaveDebugLogsSettingName) as bool? ?? true;
            var enableLogHistory = _settingsService.GetValue(EnableLogHistorySettingName) as bool? ?? true;
            string logFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars", "logs");
            string logFilePath = System.IO.Path.Combine(logFolderPath, "latest.log");
            DebugService.Instance.SetFileLogging(saveDebugLogs, logFilePath, enableLogHistory);
        }

        private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string logFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars");
            Process.Start("explorer.exe", logFolderPath);
        }

        private void EnableLogReaderLoggingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var isOn = ((ToggleSwitch)sender).IsOn;
            _settingsService.SetValue(EnableLogReaderLoggingSettingName, isOn);
        }

        private void LoadEnableLogReaderLoggingSetting()
        {
            var enableLogReaderLogging = _settingsService.GetValue(EnableLogReaderLoggingSettingName);
            EnableLogReaderLoggingToggle.IsOn = enableLogReaderLogging as bool? ?? true;
        }
    }
}