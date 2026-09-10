using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics;
using DevWinUI;
using JustBedwars.Services;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json.Linq;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars.Views
{
    /// <summary>
    /// The update notification Window. It requires updates to have been checked in the app at least once to launch.
    /// This window is likely to crash if Hot Reload is on because microslop was feeling like making my life harder.
    /// </summary>
    public sealed partial class UpdateWindow : Window
    {
        public bool IsUpdating = false;
        OverlappedPresenter _presenter = OverlappedPresenter.CreateForDialog();

        public UpdateWindow()
        {
            InitializeComponent();
            _ = LoadDataFromGitHub();
            SystemBackdrop = new MicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            SetTitleBar(TitleBar);
            WindowHelper.SetWindowOwner(parentWindow: CoreEnvironment.MainWindow, childWindow: this);
            _presenter.PreferredMaximumHeight = 800;
            _presenter.PreferredMaximumWidth = 1000;
            _presenter.PreferredMinimumHeight = 400;
            _presenter.PreferredMinimumWidth = 400;
            AppWindow.SetPresenter(_presenter);
            AppWindow.Resize(new SizeInt32(800, 600));

            if (UpdateService.CurrentVersion == null || UpdateService.LatestVersion == null)
            {
                Close();
                return;
            }

            NewVersion.Text = UpdateService.LatestVersion.ToString();
            CurrentVersion.Text = UpdateService.CurrentVersion.ToString();

            AppWindow.Closing += AppWindowOnClosing;

            Closed += UpdateWindow_Closed;
        }

        private void AppWindowOnClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (IsUpdating)
                args.Cancel = true;
        }

        private void UpdateWindow_Closed(object sender, WindowEventArgs args)
        {
            this.SystemBackdrop = null;
        }

        private async Task LoadDataFromGitHub()
        {
            // ReSharper disable once ShortLivedHttpClient
            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JustBedwars-App/1.0");

            var response = await httpClient.GetAsync("https://api.github.com/repos/fex2000/JustBedwars/releases/latest");
            if (!response.IsSuccessStatusCode)
            {
                Changelog.Text = $"Error while fetching Changelog: {response.ReasonPhrase}";
                ChangelogLoadingIndicator.Visibility = Visibility.Collapsed;
                return;
            }
            var data = JObject.Parse(await response.Content.ReadAsStringAsync());

            string changelogText = (string)data["body"]!;
            changelogText = Regex.Replace(changelogText, "Just looking for the download\\? \\[Go Here\\]\\(https://fex2000.github.io/JustBedwars\\)", "");
            Changelog.Text = changelogText;
            DispatcherQueue.TryEnqueue(() => Changelog.Visibility = Visibility.Visible);

            ChangelogLoadingIndicator.Visibility = Visibility.Collapsed;
        }

        private void InstallButton_OnClick(object sender, RoutedEventArgs e)
        {
            InstallButton.IsEnabled = false;
            DismissButton.IsEnabled = false;
            IsUpdating = true;
            _presenter.SetBorderAndTitleBar(true, false);
            CoreEnvironment.MainWindow!.AppWindow.Hide();
            _ = UpdateService.ShowUpdateDialog(this);
        }

        private void DismissButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
