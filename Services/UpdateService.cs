using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using JustBedwars.Services;

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
                // Handle exceptions (e.g., no internet connection, API rate limit)
                DebugService.Instance.Log($"[UpdateService] Error checking for updates: {ex.Message}");
            }
        }

        private static async Task ShowUpdateDialog()
        {
            var updateDialog = new ContentDialog
            {
                Title = "Update Available",
                Content = "Please update to the newest version for the best experience. Updates may be required because of Backend changed.",
                PrimaryButtonText = "Download now",
                CloseButtonText = "Later"
            };

            if (App.Window?.Content?.XamlRoot is not null)
            {
                updateDialog.XamlRoot = App.Window.Content.XamlRoot;

                var downloadStarted = false;
                updateDialog.PrimaryButtonClick += async (dialog, args) =>
                {
                    // Prevent the dialog from closing
                    args.Cancel = true;

                    // Prevent starting multiple downloads
                    if (downloadStarted) return;
                    downloadStarted = true;

                    dialog.IsPrimaryButtonEnabled = false;
                    dialog.IsSecondaryButtonEnabled = false;
                    
                    var progressBar = new ProgressBar { IsIndeterminate = false, Minimum = 0, Maximum = 100, Value = 0 };
                    dialog.Content = progressBar;

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
                                            progressBar.Value = (int)((double)downloadedBytes / totalBytes * 100);
                                        }
                                    }
                                }
                            }
                        }

                        dialog.Content = "Download complete. Please continue in the new window to finish the installation.";
                        dialog.CloseButtonText = "Ok";
                        dialog.IsSecondaryButtonEnabled = true;


                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = tempPath,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                    }
                    catch (Exception ex)
                    {
                        dialog.Content = $"An error occurred during download: {ex.Message}";
                        dialog.CloseButtonText = "Ok";
                        dialog.IsSecondaryButtonEnabled = true;
                    }
                };

                await updateDialog.ShowAsync();
            }
        }
    }
}