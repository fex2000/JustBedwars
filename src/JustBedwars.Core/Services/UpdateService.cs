using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
#if WINDOWS
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif
using Newtonsoft.Json.Linq;

namespace JustBedwars.Services;

public class UpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/fex2000/JustBedwars/releases/latest";
    private const string DownloadUrl = "https://github.com/fex2000/JustBedwars/releases/latest/download/JustBedwars.exe";

    public static Version? CurrentVersion;
    public static Version? LatestVersion;

    public static async Task<bool> CheckForUpdates(Version currentVersion)
    {
        CurrentVersion = currentVersion;
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "JustBedwarsApp");

            var response = await client.GetStringAsync(GitHubApiUrl);
            var latestRelease = JObject.Parse(response);
            var latestVersionStr = latestRelease["tag_name"]?.ToString().TrimStart('v');

            if (Version.TryParse(latestVersionStr, out var latestVersion))
            {
                LatestVersion = latestVersion;
                if (latestVersion > currentVersion) return true;
            }
        }
        catch (Exception ex)
        {
            DebugService.Instance.Log($"[UpdateService] Error checking for updates: {ex.Message}");
        }

        return false;
    }

#if WINDOWS
    public static async Task ShowUpdateDialog(Window updateWindow)
    {
        var aptabaseClient = CoreEnvironment.AptabaseClient;

        var infoText = new TextBlock
        {
            Text =
                "Please wait while the update is being downloaded.",
            TextWrapping = TextWrapping.WrapWholeWords
        };

        var progressBar = new ProgressBar()
        {
            IsIndeterminate = true
        };
        
        var dialogContent = new StackPanel
        {
            Spacing = 12
        };
        dialogContent.Children.Add(infoText);
        dialogContent.Children.Add(progressBar);

        var updateDialog = new ContentDialog
        {
            Title = "Getting Ready",
            Content = dialogContent,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
        };

        if (updateWindow.Content.XamlRoot is not null)
        {
            var downloadStarted = false;
            updateDialog.XamlRoot = updateWindow.Content.XamlRoot;

            if (downloadStarted) return;
            downloadStarted = true;
            _ = updateDialog.ShowAsync();

            progressBar.Value = 0;
            progressBar.IsIndeterminate = true;

            if (aptabaseClient is not null)
                _ = aptabaseClient.TrackEvent("UpdateDownloading");

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "JustBedwars_update.exe");
                updateDialog.Title = "Downloading...";
                using (var client = new HttpClient())
                {
                    using (var response =
                           await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var downloadedBytes = 0L;

                        progressBar.IsIndeterminate = totalBytes <= 0;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                                   FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            var bytesRead = 0;
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;
                                DebugService.Instance.Log($"[UpdateService] Downloaded {downloadedBytes} Bytes");
                                if (totalBytes != -1)
                                    progressBar.Value = (double)downloadedBytes / totalBytes * 100;
                            }
                        }
                    }
                }

                infoText.Text = "Download complete. Please continue in the new window to finish the installation.";


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
                infoText.Text = $"An error occurred during download: {ex.Message}\n\n JustBedwars Updater will now exit. Please retry downloading from GitHub.";
                await Task.Delay(10000);
                CoreEnvironment.MainWindow!.AppWindow.Show();
                updateWindow.Close();
            }
        }
    }
#endif
}