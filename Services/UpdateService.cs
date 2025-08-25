
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;

namespace JustBedwars.Services
{
    public class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/fex2000/JustBedwars/releases/latest";
        private const string DownloadUrl = "https://fex2000.github.io/JustBedwars";

        public static async Task CheckForUpdates()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "JustBedwarsApp");

                var response = await client.GetStringAsync(GitHubApiUrl);
                var latestRelease = JObject.Parse(response);
                var latestVersionStr = latestRelease["tag_name"]?.ToString().TrimStart('v');

                if (Version.TryParse(latestVersionStr, out var latestVersion))
                {
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (latestVersion > currentVersion)
                    {
                        await ShowUpdateDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., no internet connection, API rate limit)
                Console.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        private static async Task ShowUpdateDialog()
        {
            var updateDialog = new ContentDialog
            {
                Title = "Update Available",
                Content = "A new version of JustBedwars is available. Please download the latest version to get the newest features and bug fixes.",
                PrimaryButtonText = "Go to Download Page",
                CloseButtonText = "Later"
            };

            if (App.Window?.Content?.XamlRoot is not null)
            {
                updateDialog.XamlRoot = App.Window.Content.XamlRoot;
                var result = await updateDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    var uri = new Uri(DownloadUrl);
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
        }
    }
}
