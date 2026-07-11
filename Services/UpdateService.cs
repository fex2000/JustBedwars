using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using JustBedwars.Services;
using DevWinUI;
using Microsoft.UI.Xaml;

namespace JustBedwars.Services
{
    public class UpdateService
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/fex2000/JustBedwars/releases/latest";
        private const string DownloadUrl = "https://fex2000.github.io/JustBedwars/download/JustBedwars.exe";

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
                DebugService.Instance.Log($"[UpdateService] Error checking for updates: {ex.Message}");
            }
        }

        private static async Task ShowUpdateDialog()
        {
            var instance = (App)Application.Current;
            var aptabaseClient = instance.AptabaseClient;

            var infoText = new TextBlock
            {
                Text = "Please update to the newest version for the best experience. Updates may be required because of Backend changes.",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords
            };

            var downloadButton = new ProgressButton
            {
                Content = "Download now",
                CheckedContent = "Downloading...",
                Progress = 0,
                IsIndeterminate = false,
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
            };

            var dialogContent = new StackPanel
            {
                Spacing = 12
            };
            dialogContent.Children.Add(infoText);
            dialogContent.Children.Add(downloadButton);

            var updateDialog = new ContentDialog
            {
                Title = "Update Available",
                Content = dialogContent,
                CloseButtonText = "Later"
            };

            if (App.Window?.Content?.XamlRoot is not null)
            {
                var downloadStarted = false;
                updateDialog.XamlRoot = App.Window.Content.XamlRoot;

                downloadButton.Click += async (_, _) =>
                {
                    if (downloadStarted) return;
                    downloadStarted = true;

                    downloadButton.IsChecked = true;
                    downloadButton.IsEnabled = false;
                    downloadButton.Progress = 0;
                    downloadButton.IsIndeterminate = true;

                    _ = aptabaseClient.TrackEvent("UpdateDownloading");

                    try
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(), "JustBedwars_update.exe");

                        using (var client = new HttpClient())
                        {
                            using (var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                                var downloadedBytes = 0L;

                                downloadButton.IsIndeterminate = totalBytes <= 0;

                                using (var contentStream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                {
                                    var buffer = new byte[8192];
                                    var bytesRead = 0;
                                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        downloadedBytes += bytesRead;
                                        if (totalBytes != -1)
                                        {
                                            downloadButton.Progress = (double)downloadedBytes / totalBytes * 100;
                                        }
                                    }
                                }
                            }
                        }

                        infoText.Text = "Download complete. Please continue in the new window to finish the installation.";
                        downloadButton.Progress = 100;
                        downloadButton.Content = "Installer started";
                        downloadButton.CheckedContent = "Installer started";


                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true,
                            Arguments = "/NOCANCEL /NORESTARTAPPLICATIONS /CLOSEAPPLICATIONS /SP-"
                        };
                        Process.Start(processStartInfo);
                    }
                    catch (Exception ex)
                    {
                        infoText.Text = $"An error occurred during download: {ex.Message}";
                        downloadButton.IsChecked = false;
                        downloadButton.IsEnabled = true;
                        downloadButton.Content = "Retry download";
                        downloadButton.CheckedContent = "Downloading...";
                        downloadButton.Progress = 0;
                        downloadButton.IsIndeterminate = false;
                        downloadStarted = false;
                    }
                };

                var result = await updateDialog.ShowAsync();

                if (result == ContentDialogResult.None)
                    _ = aptabaseClient.TrackEvent("UpdateDismissed");
            }
        }
    }
}