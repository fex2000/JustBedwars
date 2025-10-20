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
            string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars", "debug.log");
            DebugService.Instance.SetFileLogging(isOn, logPath);
        }

        private void LoadSaveDebugLogsSetting()
        {
            var saveDebugLogs = _settingsService.GetValue(SaveDebugLogsSettingName);
            if (saveDebugLogs != null)
            {
                SaveDebugLogsToggle.IsOn = (bool)saveDebugLogs;
            }
        }

        private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string logFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustBedwars");
            Process.Start("explorer.exe", logFolderPath);
        }
    }
}