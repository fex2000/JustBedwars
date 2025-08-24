using JustBedwars.Models;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Caching;

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using System.Threading;
using System.Diagnostics;

namespace JustBedwars.Services
{
    public class HypixelApi
    {
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private string? _apiKey;
        private readonly SemaphoreSlim _apiSemaphore = new SemaphoreSlim(1, 1);
        private readonly Stopwatch _apiStopwatch = new Stopwatch();
        private readonly SemaphoreSlim _errorDialogSemaphore = new SemaphoreSlim(1, 1);
        private readonly MemoryCache _playerCache = new MemoryCache("PlayerCache");

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<Player?> GetPlayerStats(string username)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                DebugService.Instance.Log("[HypixelApi] API key is not set.");
                return null;
            }

            // Check cache first
            if (_playerCache.Contains(username))
            {
                DebugService.Instance.Log($"[HypixelApi] Returning player {username} from cache.");
                return (Player)_playerCache.Get(username);
            }

            await _apiSemaphore.WaitAsync();
            try
            {
                if (_apiStopwatch.IsRunning && _apiStopwatch.ElapsedMilliseconds < 100)
                {
                    await Task.Delay(100 - (int)_apiStopwatch.ElapsedMilliseconds);
                }

                // Get UUID from Mojang API
                var uuid = await GetUuidFromUsername(username);
                if (uuid == null)
                {
                    DebugService.Instance.Log($"[HypixelApi] Couldn't find UUID for {username}.");
                    return null;
                }

                var url = $"https://api.hypixel.net/player?key={_apiKey}&uuid={uuid}";
                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);

                _apiStopwatch.Restart();

                Debug.WriteLine(response);
                var json = JObject.Parse(response);

                if (json["success"] != null && !(bool)json["success"])
                {
                    DebugService.Instance.Log($"[HypixelApi] API call failed: {json["cause"]}");
                    return null;
                }

                if (json["player"] == null)
                {
                    DebugService.Instance.Log("[HypixelApi] Player not found.");
                    return null;
                }

                var player = new Player
                {
                    Username = (string?)json["player"]?["displayname"],
                    Star = (int?)json["player"]?["achievements"]?["bedwars_level"] ?? 0,
                    FKDR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["final_kills_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["final_deaths_bedwars"] ?? 1), 2),
                    WLR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["wins_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["losses_bedwars"] ?? 1), 2),
                    KDR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["kills_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["deaths_bedwars"] ?? 1), 2),
                    Finals = (int?)json["player"]?["stats"]?["Bedwars"]?["final_kills_bedwars"] ?? 0,
                    Wins = (int?)json["player"]?["stats"]?["Bedwars"]?["wins_bedwars"] ?? 0,
                    FirstLogin = (long?)json["player"]?["firstLogin"] ?? 0,
                    PlayerTag = "-",
                };

                // Add to cache
                var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
                _playerCache.Add(username, player, policy);
                DebugService.Instance.Log($"[HypixelApi] Added player {username} to cache for 5 minutes.");

                return player;
            }
            catch (HttpRequestException ex)
            {
                DebugService.Instance.Log($"[HypixelApi] HTTP request failed: {ex.Message}");
                var player = new Player
                {
                    Username = username,
                    PlayerTag = "NICK",
                };
                return player;
            }
            catch (Exception ex)
            {
                DebugService.Instance.Log($"[HypixelApi] An error occurred: {ex.Message}");
                return null;
            }
            finally
            {
                _apiSemaphore.Release();
            }
        }

        private async Task<string?> GetUuidFromUsername(string username)
        {
            try
            {
                var url = $"https://api.mojang.com/users/profiles/minecraft/{username}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);
                return (string?)json["id"];
            }
            catch (HttpRequestException)
            {
                // This could happen if the user doesn't exist
                return null;
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            await _errorDialogSemaphore.WaitAsync();
            try
            {
                if (App.Window?.DispatcherQueue.HasThreadAccess ?? false)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = message,
                        CloseButtonText = "OK",
                        XamlRoot = App.Window?.Content.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    App.Window?.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = message,
                            CloseButtonText = "OK",
                            XamlRoot = App.Window?.Content.XamlRoot
                        };
                        await dialog.ShowAsync();
                    });
                }
            }
            finally
            {
                _errorDialogSemaphore.Release();
            }
        }
    }
}