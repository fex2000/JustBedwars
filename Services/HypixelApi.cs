using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace JustBedwars.Services
{
    public class HypixelApi
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        private string? _apiKey;
        private static readonly SemaphoreSlim _apiSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Stopwatch _apiStopwatch = new Stopwatch();
        private static readonly SemaphoreSlim _errorDialogSemaphore = new SemaphoreSlim(1, 1);
        private static readonly MemoryCache _playerCache = new MemoryCache("PlayerCache");
        private static readonly MemoryCache _leaderboardCache = new MemoryCache("LeaderboardCache");
        private static readonly MemoryCache _uuidCache = new MemoryCache("UuidCache");
        private static readonly MemoryCache _guildCache = new MemoryCache("GuildCache");
        private const int MaxConcurrentMojangLookups = 100;

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<Guild?> GetGuildAsync(string query, string type)
        {
            string cacheKey = $"{type}_{query}";
            if (_guildCache.Contains(cacheKey))
            {
                DebugService.Instance.Log($"[HypixelApi] Returning guild {query} from cache.");
                return (Guild)_guildCache.Get(cacheKey);
            }

            try
            {
                string url = "";
                string queryParam = "";
                switch (type)
                {
                    case "Guild Name":
                        queryParam = $"name={query}";
                        break;
                    case "Guild ID":
                        queryParam = $"id={query}";
                        break;
                    case "Member Name":
                        var uuid = await GetUuidFromUsername(query);
                        if (uuid == null)
                        {
                            DebugService.Instance.Log($"[HypixelApi] Couldn't find UUID for {query}.");
                            return null;
                        }
                        queryParam = $"player={uuid}";
                        break;
                    case "Member UUID":
                        queryParam = $"player={query}";
                        break;
                }

                url = $"https://jbw.fexei.at/api/justbedwars/v2/guild?{queryParam}";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url = $"https://api.hypixel.net/guild?key={_apiKey}&{queryParam}";
                }

                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);

                DebugService.Instance.Log($"[HypixelApi] Response: {response.Substring(0, Math.Min(response.Length, 100))}...");
                var json = JObject.Parse(response);

                if ((bool?)json["success"] == false)
                {
                    DebugService.Instance.Log($"[HypixelApi] API call failed: {json["cause"]}");
                    return null;
                }

                var guildObj = json["guild"];
                if (guildObj == null)
                {
                    DebugService.Instance.Log("[HypixelApi] Guild not found.");
                    return null;
                }

                var membersToken = guildObj["members"] as JArray;
                var ranksToken = guildObj["ranks"] as JArray;

                var guild = new Guild
                {
                    Id = (string?)guildObj["_id"] ?? string.Empty,
                    Name = (string?)guildObj["name"] ?? string.Empty,
                    Exp = (long?)guildObj["exp"] ?? 0,
                    Tag = (string?)guildObj["tag"] ?? string.Empty,
                    Created = (long?)guildObj["created"] ?? 0,
                    Description = (string?)guildObj["description"] ?? string.Empty,
                    PreferredGames = guildObj["preferredGames"]?.ToObject<List<string>>() ?? new List<string>(),
                    ExpByGameType = guildObj["guildExpByGameType"]?.ToObject<Dictionary<string, long>>() ?? new Dictionary<string, long>(),
                    OnlinePlayers = (int?)guildObj["achievements"]?["ONLINE_PLAYERS"] ?? 0,
                    Members = membersToken?.Select(m => new GuildMember
                    {
                        Uuid = (string?)m["uuid"] ?? string.Empty,
                        Rank = (string?)m["rank"] ?? string.Empty,
                        Joined = (long?)m["joined"] ?? 0
                    }).ToList() ?? new List<GuildMember>(),
                    Ranks = ranksToken?.Select(r => new GuildRank
                    {
                        Name = (string?)r["name"] ?? string.Empty,
                        Priority = (int?)r["priority"] ?? 0,
                        Tag = (string?)r["tag"] ?? string.Empty
                    }).ToList() ?? new List<GuildRank>()
                };

                guild.Level = GetGuildLevel(guild.Exp);

                await GetNamesForGuildMembers(guild.Members, null);

                var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10) };
                _guildCache.Add(cacheKey, guild, policy);

                return guild;
            }
            catch (HttpRequestException ex)
            {
                DebugService.Instance.Log($"[HypixelApi] HTTP request failed: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                DebugService.Instance.Log($"[HypixelApi] An error occurred: {ex.Message}");
                return null;
            }
        }

        public async Task GetNamesForGuildMembers(List<GuildMember> members, IProgress<double>? progress)
        {
            var uuidsToFetch = members.Where(m => string.IsNullOrEmpty(m.Name)).Select(m => m.Uuid).ToList();
            var names = await GetUsernamesFromUuids(uuidsToFetch, progress);
            foreach (var member in members)
            {
                if (names.ContainsKey(member.Uuid))
                {
                    member.Name = names[member.Uuid];
                }
                else if (string.IsNullOrEmpty(member.Name))
                {
                    member.Name = "-- Username Error --";
                }
            }
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

                bool useV2Api = string.IsNullOrEmpty(_apiKey);
                var url = useV2Api
                    ? $"https://jbw.fexei.at/api/justbedwars/v2/player?uuid={uuid}"
                    : $"https://api.hypixel.net/player?key={_apiKey}&uuid={uuid}";


                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);

                _apiStopwatch.Restart();

                DebugService.Instance.Log($"[HypixelApi] Response: {response.Substring(0, Math.Min(response.Length, 100))}...");
                var json = JObject.Parse(response);

                Player player;
                if (useV2Api)
                {
                    if (json["FKDR"] == null)
                    {
                        DebugService.Instance.Log("[HypixelApi] Player not found in v2 API.");
                        var nickplayer = new Player
                        {
                            Username = username,
                            PlayerTag = (string?)"NICK",
                        };
                        return nickplayer;
                    }

                    player = new Player
                    {
                        Username = (string?)json["Username"],
                        Star = (int?)json["Stars"] ?? 0,
                        FKDR = (double?)json["FKDR"] ?? 0,
                        WLR = (double?)json["WLR"] ?? 0,
                        KDR = (double?)json["KDR"] ?? 0,
                        BBLR = (double?)json["BBLR"] ?? 0,
                        Finals = (int?)json["Finals"] ?? 0,
                        FinalDeaths = (int?)json["FinalDeaths"] ?? 0,
                        Wins = (int?)json["Wins"] ?? 0,
                        Losses = (int?)json["Losses"] ?? 0,
                        Kills = (int?)json["Kills"] ?? 0,
                        Deaths = (int?)json["Deaths"] ?? 0,
                        Beds = (int?)json["Beds"] ?? 0,
                        BedsLost = (int?)json["BedsLost"] ?? 0,
                        FirstLogin = (long?)json["FirstLogin"] ?? 0,
                        PlayerTag = (string?)"-",
                        PlayerUUID = uuid,
                        NetworkExp = (long?)json["NetworkExp"] ?? 0,
                        BedwarsExperience = (int?)json["BedwarsExperience"] ?? 0,
                    };
                }
                else
                {
                    if ((bool?)json["success"] == false)
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

                    player = new Player
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
                }

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

                var url = $"https://jbw.fexei.at/leaderboards";
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    url = $"https://api.hypixel.net/leaderboards?key={_apiKey}";
                }

                DebugService.Instance.Log($"[HypixelApi] Requesting: {url}");

                var response = await _httpClient.GetStringAsync(url);
                _apiStopwatch.Restart();
                var json = JObject.Parse(response);

                if ((bool?)json["success"] == false)
                {
                    DebugService.Instance.Log($"[HypixelApi] API call failed: {json["cause"]}");
                    return new List<LeaderboardEntry>();
                }

                var leaders = new List<LeaderboardEntry>();
                if (json["leaderboards"]?["BEDWARS"] is JArray bedwarsLeaderboards)
                {
                    var board = bedwarsLeaderboards.FirstOrDefault(b => (string?)b["path"] == boardPath);
                    if (board?["leaders"]?.ToObject<List<string>>() is List<string> leaderUuids)
                    {
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

        public async Task GetNamesForLeaderboardEntries(List<LeaderboardEntry> entries, IProgress<double>? progress)
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
                    entry.Name = "-- Username Error --";
                }
            }
        }

        private async Task<Dictionary<string, string>> GetUsernamesFromUuids(List<string> uuids, IProgress<double>? progress)
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
                using var throttle = new SemaphoreSlim(MaxConcurrentMojangLookups);

                int completedCount = 0;
                int totalToFetch = uuidsToFetch.Count;
                object progressLock = new object();

                var fetchTasks = uuidsToFetch.Select(uuid => Task.Run(async () =>
                {
                    await throttle.WaitAsync();

                    try
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                var url = $"https://playerdb.co/api/player/minecraft/{uuid}";
                                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                                request.Headers.UserAgent.ParseAdd($"JustBedwars /v{Assembly.GetExecutingAssembly().GetName().Version}");
                                using var responseMessage = await _httpClient.SendAsync(request);
                                responseMessage.EnsureSuccessStatusCode();
                                var response = await responseMessage.Content.ReadAsStringAsync();

                                var json = JObject.Parse(response);
                                var name = (string?)json["data"]?["player"]?["username"];

                                if (name != null)
                                {
                                    lock (progressLock)
                                    {
                                        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
                                        _uuidCache.Add(uuid, name, policy);
                                        names[uuid] = name;
                                    }
                                    return;
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                DebugService.Instance.Log($"[HypixelApi] Timeout getting username for uuid {uuid}. Retry {i + 1}/5");
                                if (i < 2) await Task.Delay(1000);
                            }
                            catch (Exception ex)
                            {
                                // Log errors for failed UUIDs but allow others to succeed
                                DebugService.Instance.Log($"[HypixelApi] Couldn't get username for uuid {uuid}: {ex.Message}. Retry {i + 1}/5");
                                if (i < 2) await Task.Delay(1000);
                            }
                        }
                        DebugService.Instance.Log($"[HypixelApi] Failed to get username for uuid {uuid} after 5 retries.");
                    }
                    finally
                    {
                        throttle.Release(); // Release the slot so another task can run

                        // Safely report progress after each attempt (success or failure)
                        // This allows the progress bar to move, even with concurrent fetching.
                        lock (progressLock)
                        {
                            completedCount++;
                            progress?.Report((double)completedCount / totalToFetch * 100);
                        }
                    }
                })).ToList();

                // Wait for all concurrent requests to complete
                await Task.WhenAll(fetchTasks);
            }

            return names;
        }

        public async Task<string?> GetUuidFromUsername(string username)
        {
            var url = $"https://api.mojang.com/users/profiles/minecraft/{username}";
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(url);
                    var json = JObject.Parse(response);
                    return (string?)json["id"];
                }
                catch (HttpRequestException)
                {
                    // This could happen if the user doesn't exist, so no retry
                    return null;
                }
                catch (TaskCanceledException)
                {
                    // Timeout
                    DebugService.Instance.Log($"[HypixelApi] Timeout getting UUID for {username}. Retry {i + 1}/3");
                    await Task.Delay(1000); // Wait 1s before retrying
                }
                catch (Exception ex)
                {
                    DebugService.Instance.Log($"[HypixelApi] Error getting UUID for {username}: {ex.Message}. Retry {i + 1}/3");
                    await Task.Delay(1000); // Wait 1s before retrying
                }
            }
            DebugService.Instance.Log($"[HypixelApi] Failed to get UUID for {username} after 3 retries.");
            return null;
        }

        private double GetGuildLevel(double exp)
        {
            double[] expNeeded = {
                100000, 150000, 250000, 500000, 750000, 1000000, 1250000, 1500000, 2000000,
                2500000, 2500000, 2500000, 2500000, 2500000, 3000000
            };

            double level = 0;
            for (int i = 0; i < expNeeded.Length; i++)
            {
                if (exp < expNeeded[i])
                {
                    return level + exp / expNeeded[i];
                }
                exp -= expNeeded[i];
                level++;
            }
            return level + exp / 3000000;
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
