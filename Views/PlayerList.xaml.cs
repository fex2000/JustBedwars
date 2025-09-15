using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace JustBedwars.Views
{
    public sealed partial class PlayerList : Page
    {
        private LogReader _logReader;
        private readonly HypixelApi _hypixelApi;
        private readonly SettingsService _settingsService;
        private readonly ObservableCollection<Player> _players = new ObservableCollection<Player>();
        private const string ApiKeySettingName = "HypixelApiKey";
        private const string LogFileSettingName = "LogFilePath";

        public PlayerList()
        {
            InitializeComponent();
            _hypixelApi = new HypixelApi();
            _settingsService = new SettingsService();

            LoadApiKey();
            LoadLogReader();

            PlayersListView.ItemsSource = _players;
        }

        private async void AddPlayer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var inputTextBox = new TextBox
            {
                PlaceholderText = "Enter player name"
            };
            var dialog = new ContentDialog
            {
                Title = "Add Player",
                Content = inputTextBox,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var playerName = inputTextBox.Text;
                if (!string.IsNullOrWhiteSpace(playerName))
                {
                    OnPlayerJoined(playerName);
                }
            }
        }

        private void LoadApiKey()
        {
            var apiKey = _settingsService.GetValue(ApiKeySettingName);
            if (apiKey != null)
            {
                _hypixelApi.SetApiKey((string)apiKey);
            }
        }

        private void LoadLogReader()
        {
            var logFilePath = _settingsService.GetValue(LogFileSettingName);
            if (logFilePath != null)
            {
                _logReader = new LogReader((string)logFilePath);
            }
            else
            {
                _logReader = new LogReader();
            }
            _logReader.PlayerJoined += OnPlayerJoined;
            _logReader.PlayerLeft += OnPlayerLeft;
            _logReader.WhoResult += OnWhoResult;
            _logReader.Start();
            DebugService.Instance.EmulatePlayerJoined += OnPlayerJoined;
            DebugService.Instance.EmulatePlayerLeft += OnPlayerLeft;
        }

        public async void OnPlayerJoined(string username)
        {
            var player = new Player { Username = username, IsLoading = true };
            DispatcherQueue.TryEnqueue(() =>
            {
                _players.Add(player);
            });

            var stats = await _hypixelApi.GetPlayerStats(username);

            DispatcherQueue.TryEnqueue(() =>
            {
                var existingPlayer = _players.FirstOrDefault(p => p.Username == username);
                if (existingPlayer != null)
                {
                    if (stats != null)
                    {
                        // Create a new Player object with updated stats
                        var updatedPlayer = new Player
                        {
                            Username = stats.Username,
                            Star = stats.Star,
                            FKDR = stats.FKDR,
                            WLR = stats.WLR,
                            BBLR = stats.BBLR,
                            Finals = stats.Finals,
                            FinalDeaths = stats.FinalDeaths,
                            Kills = stats.Kills,
                            Deaths = stats.Deaths,
                            KDR = stats.KDR,
                            Beds = stats.Beds,
                            BedsLost = stats.BedsLost,
                            Wins = stats.Wins,
                            Losses = stats.Losses,
                            PlayerTag = stats.PlayerTag,
                            FirstLogin = stats.FirstLogin,
                            IsLoading = false // Set to false as stats are loaded

                        };

                        // Find the index of the existing player and replace it
                        var index = _players.IndexOf(existingPlayer);
                        if (index != -1)
                        {
                            _players[index] = updatedPlayer;
                            DebugService.Instance.Log($"[PlayerList] Replaced player {username} with updated stats.");
                        }
                    }
                    else
                    {
                        // If stats are null, it could be a nick or an API error.
                        // Let's retry.
                        DebugService.Instance.Log($"[PlayerList] No stats received for {username}. Initiating retry logic.");
                        RetryPlayerFetch(username, 1);
                    }
                }
                SortPlayers();
            });
        }

        private async void RetryPlayerFetch(string username, int attempt)
        {
            int delay;
            switch (attempt)
            {
                case 1:
                    delay = 3000; // 3 seconds
                    break;
                case 2:
                    delay = 5000; // 5 seconds
                    break;
                case 3:
                    delay = 10000; // 10 seconds
                    break;
                default:
                    // Max retries reached
                    var player = _players.FirstOrDefault(p => p.Username == username);
                    if (player != null)
                    {
                        player.IsLoading = false; // Give up
                        player.PlayerTag = "ERROR"; // Indicate error
                    }
                    return;
            }

            await Task.Delay(delay);

            // Check if player is still in the list
            var existingPlayer = _players.FirstOrDefault(p => p.Username == username);
            if (existingPlayer == null)
            {
                // Player left, no need to retry
                return;
            }

            // It's good practice to set IsLoading to true before fetching
            existingPlayer.IsLoading = true;

            var stats = await _hypixelApi.GetPlayerStats(username);

            DispatcherQueue.TryEnqueue(() =>
            {
                var playerToUpdate = _players.FirstOrDefault(p => p.Username == username);
                if (playerToUpdate != null)
                {
                    if (stats != null)
                    {
                        // Update player with stats, same logic as in OnPlayerJoined
                        var updatedPlayer = new Player
                        {
                            Username = stats.Username,
                            Star = stats.Star,
                            FKDR = stats.FKDR,
                            WLR = stats.WLR,
                            BBLR = stats.BBLR,
                            Finals = stats.Finals,
                            FinalDeaths = stats.FinalDeaths,
                            Kills = stats.Kills,
                            Deaths = stats.Deaths,
                            KDR = stats.KDR,
                            Beds = stats.Beds,
                            BedsLost = stats.BedsLost,
                            Wins = stats.Wins,
                            Losses = stats.Losses,
                            PlayerTag = stats.PlayerTag,
                            FirstLogin = stats.FirstLogin,
                            IsLoading = false
                        };

                        var index = _players.IndexOf(playerToUpdate);
                        if (index != -1)
                        {
                            _players[index] = updatedPlayer;
                            DebugService.Instance.Log($"[PlayerList] Replaced player {username} with updated stats on retry attempt {attempt}.");
                        }
                        SortPlayers();
                    }
                    else
                    {
                        // Retry again
                        RetryPlayerFetch(username, attempt + 1);
                    }
                }
            });
        }

        public void OnPlayerLeft(string username)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var playerToRemove = _players.FirstOrDefault(p => p.Username == username);
                if (playerToRemove != null)
                {
                    _players.Remove(playerToRemove);
                }
            });
        }

        private void OnWhoResult(List<string> players)
        { 
            DispatcherQueue.TryEnqueue(() =>
            {
                _players.Clear();
            });

            foreach (var player in players)
            {
                OnPlayerJoined(player);
            }
        }

        private void SortPlayers()
        {
            var sortingMode = _settingsService.GetValue("PlayerSorting") as string ?? "JustBedwars Score";
            
            IOrderedEnumerable<Player> sortedPlayers;

            switch (sortingMode)
            {
                case "Abyss Index":
                    sortedPlayers = _players
                        .OrderBy(p => string.IsNullOrEmpty(p.PlayerTag))
                        .ThenByDescending(p => p.Star * p.FKDR * p.FKDR);
                    break;
                case "FKDR":
                    sortedPlayers = _players
                        .OrderBy(p => string.IsNullOrEmpty(p.PlayerTag))
                        .ThenByDescending(p => p.FKDR);
                    break;
                case "WLR":
                    sortedPlayers = _players
                        .OrderBy(p => string.IsNullOrEmpty(p.PlayerTag))
                        .ThenByDescending(p => p.WLR);
                    break;
                case "Stars":
                    sortedPlayers = _players
                        .OrderBy(p => string.IsNullOrEmpty(p.PlayerTag))
                        .ThenByDescending(p => p.Star);
                    break;
                case "JustBedwars Score":
                default:
                    sortedPlayers = _players
                        .OrderBy(p => string.IsNullOrEmpty(p.PlayerTag))
                        .ThenByDescending(p => p.Star * Math.Pow(p.FKDR, 2) * Math.Pow(p.WLR, 1.2) * Math.Pow(p.BBLR, 1.1) * (1 + p.Finals / 1000.0 + p.Kills / 2000.0 + p.Beds / 500.0 + p.Wins / 1000.0));
                    break;
            }

            var sortedList = sortedPlayers.ToList();
            _players.Clear();
            foreach (var p in sortedList)
            {
                _players.Add(p);
            }
        }

        private void Player_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Player player)
            {
                var existingPlayer = _players.FirstOrDefault(p => p.Username == player.Username);
                if (existingPlayer != null)
                {
                    var index = _players.IndexOf(existingPlayer);
                    var updatedPlayer = existingPlayer;
                    updatedPlayer.IsExpanded = !existingPlayer.IsExpanded;
                    if (index != -1)
                    {
                        _players[index] = updatedPlayer;
                    }
                }
            }
        }
    }
}
