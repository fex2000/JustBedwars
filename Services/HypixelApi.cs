using JustBedwars.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

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
        private readonly MemoryCache _leaderboardCache = new MemoryCache("LeaderboardCache");
        private readonly MemoryCache _uuidCache = new MemoryCache("UuidCache");

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<Player?> GetPlayerStats(string username)
        {

            // Check cache first
            if (_playerCache.Contains(username))
            {
                DebugService.Instance.Log($"[HypixelApi] Returning player {username} from cache.");
                return (Player)_playerCache.Get(username);
            }

            await _apiSemaphore.WaitAsync();
            try
            {
                if (_apiStopwatch.IsRunning && _apiStopwatch.ElapsedMilliseconds < 10)
                {
                    await Task.Delay(10 - (int)_apiStopwatch.ElapsedMilliseconds);
                }

                // Get UUID from Mojang API
                var uuid = await GetUuidFromUsername(username);
                if (uuid == null)
                {
                    DebugService.Instance.Log($"[HypixelApi] Couldn't find UUID for {username}.");
                    var nickplayer = new Player
                    {
                        Username = username,
                        PlayerTag = (string?)"NICK",
                    };
                    return nickplayer;
                }

               
                var url = $"http://185.194.216.210:3000/player?uuid={uuid}";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url = $"https://api.hypixel.net/player?key={_apiKey}&uuid={uuid}";
                }

                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);

                _apiStopwatch.Restart();

                Debug.WriteLine(response);
                var json = JObject.Parse(response);

                if (json["success"] != null && !(bool)json["success"])
                {
                    DebugService.Instance.Log($"[HypixelApi] API call failed: {json["cause"]}");
                    var errorplayer = new Player
                    {
                        Username = username,
                        PlayerTag = (string?)"ERROR",
                    };
                    return errorplayer;
                }

                if (json["player"] == null)
                {
                    DebugService.Instance.Log("[HypixelApi] Player not found.");
                    var nickplayer = new Player
                    {
                        Username = username,
                        PlayerTag = (string?)"NICK",
                    };
                    return nickplayer;
                }

                                var player = new Player
                {
                    Username = (string?)json["player"]?["displayname"],
                    Star = (int?)json["player"]?["achievements"]?["bedwars_level"] ?? 0,
                    FKDR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["final_kills_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["final_deaths_bedwars"] ?? 1), 2),
                    WLR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["wins_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["losses_bedwars"] ?? 1), 2),
                    KDR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["kills_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["deaths_bedwars"] ?? 1), 2),
                    BBLR = Math.Round(((double?)json["player"]?["stats"]?["Bedwars"]?["beds_broken_bedwars"] ?? 0) / ((double?)json["player"]?["stats"]?["Bedwars"]?["beds_lost_bedwars"] ?? 1), 2),
                    Finals = (int?)json["player"]?["stats"]?["Bedwars"]?["final_kills_bedwars"] ?? 0,
                    FinalDeaths = (int?)json["player"]?["stats"]?["Bedwars"]?["final_deaths_bedwars"] ?? 0,
                    Wins = (int?)json["player"]?["stats"]?["Bedwars"]?["wins_bedwars"] ?? 0,
                    Losses = (int?)json["player"]?["stats"]?["Bedwars"]?["losses_bedwars"] ?? 0,
                    Kills = (int?)json["player"]?["stats"]?["Bedwars"]?["kills_bedwars"] ?? 0,
                    Deaths = (int?)json["player"]?["stats"]?["Bedwars"]?["deaths_bedwars"] ?? 0,
                    Beds = (int?)json["player"]?["stats"]?["Bedwars"]?["beds_broken_bedwars"] ?? 0,
                    BedsLost = (int?)json["player"]?["stats"]?["Bedwars"]?["beds_lost_bedwars"] ?? 0,
                    FirstLogin = (long?)json["player"]?["firstLogin"] ?? 0,
                    PlayerTag = (string?)"-",
                    PlayerUUID = uuid,
                    NetworkExp = (long?)json["player"]?["networkExp"] ?? 0,
                    BedwarsExperience = (int?)json["player"]?["stats"]?["Bedwars"]?["Experience"] ?? 0,
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
                var errorplayer = new Player
                {
                    Username = username,
                    PlayerTag = (string?)"ERROR",
                };
                return errorplayer;
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

        public async Task<List<LeaderboardEntry>> GetLeaderboard(string leaderboard, string time)
        {
            string cacheKey = $"{leaderboard}_{time}_uuids";
            if (_leaderboardCache.Contains(cacheKey))
            {
                DebugService.Instance.Log($"[HypixelApi] Returning {leaderboard} {time} leaderboard UUIDs from cache.");
                return (List<LeaderboardEntry>)_leaderboardCache.Get(cacheKey);
            }

            await _apiSemaphore.WaitAsync();
            try
            {
                if (_apiStopwatch.IsRunning && _apiStopwatch.ElapsedMilliseconds < 10)
                {
                    await Task.Delay(10 - (int)_apiStopwatch.ElapsedMilliseconds);
                }

                string boardPath = "";
                switch (leaderboard)
                {
                    case "Stars":
                        boardPath = "bedwars_level";
                        break;
                    case "Wins":
                        if (time == "Weekly")
                        {
                            boardPath = "wins_1";
                        }
                        else // Lifetime
                        {
                            boardPath = "wins_new";
                        }
                        break;
                    case "Finals":
                        if (time == "Weekly")
                        {
                            boardPath = "final_kills_1";
                        }
                        else // Lifetime
                        {
                            boardPath = "final_kills_new";
                        }
                        break;
                }

                var url = $"http://185.194.216.210:3000/leaderboards";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url = $"https://api.hypixel.net/leaderboards?key={_apiKey}";
                }

                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);
                _apiStopwatch.Restart();
                var json = JObject.Parse(response);

                if (json["success"] != null && !(bool)json["success"])
                {
                    DebugService.Instance.Log($"[HypixelApi] API call failed: {json["cause"]}");
                    return new List<LeaderboardEntry>();
                }

                var leaders = new List<LeaderboardEntry>();
                var bedwarsLeaderboards = json["leaderboards"]?["BEDWARS"];
                if (bedwarsLeaderboards != null)
                {
                    var board = bedwarsLeaderboards.FirstOrDefault(b => (string)b["path"] == boardPath);
                    if (board != null)
                    {
                        var leaderUuids = board["leaders"].ToObject<List<string>>();
                        int rank = 1;
                        foreach (var uuid in leaderUuids)
                        {
                            leaders.Add(new LeaderboardEntry
                            {
                                Rank = rank++,
                                Uuid = uuid
                            });
                        }
                    }
                }

                var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10) };
                _leaderboardCache.Add(cacheKey, leaders, policy);
                return leaders;
            }
            catch (HttpRequestException ex)
            {
                DebugService.Instance.Log($"[HypixelApi] HTTP request failed: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
            catch (Exception ex)
            {
                DebugService.Instance.Log($"[HypixelApi] An error occurred: {ex.Message}");
                return new List<LeaderboardEntry>();
            }
            finally
            {
                _apiSemaphore.Release();
            }
        }

        public async Task GetNamesForLeaderboardEntries(List<LeaderboardEntry> entries, IProgress<double> progress)
        {
            var uuidsToFetch = entries.Where(e => string.IsNullOrEmpty(e.Name)).Select(e => e.Uuid).ToList();
            var names = await GetUsernamesFromUuids(uuidsToFetch, progress);
            foreach (var entry in entries)
            {
                if (names.ContainsKey(entry.Uuid))
                {
                    entry.Name = names[entry.Uuid];
                }
                else if (string.IsNullOrEmpty(entry.Name))
                {
                    entry.Name = "Unknown";
                }
            }
        }

        private async Task<Dictionary<string, string>> GetUsernamesFromUuids(List<string> uuids, IProgress<double> progress)
        {
            var names = new Dictionary<string, string>();
            var uuidsToFetch = new List<string>();

            // Check cache first
            foreach (var uuid in uuids)
            {
                if (_uuidCache.Contains(uuid))
                {
                    names[uuid] = (string)_uuidCache.Get(uuid);
                }
                else
                {
                    uuidsToFetch.Add(uuid);
                }
            }

            if (uuidsToFetch.Count > 0)
            {
                int fetchedCount = 0;
                foreach (var uuid in uuidsToFetch)
                {
                    try
                    {
                        var url = $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}";
                        var response = await _httpClient.GetStringAsync(url);
                        var json = JObject.Parse(response);
                        var name = (string)json["name"];
                        names[uuid] = name;
                        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
                        _uuidCache.Add(uuid, name, policy);
                    }
                    catch (Exception ex)
                    {
                        DebugService.Instance.Log($"[HypixelApi] Couldn't get username for uuid {uuid}: {ex.Message}");
                    }
                    finally
                    {
                        fetchedCount++;
                        progress?.Report((double)fetchedCount / uuidsToFetch.Count * 100);
                    }
                }
            }
            return names;
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