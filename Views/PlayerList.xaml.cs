using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace JustBedwars.Views
{
    public sealed partial class PlayerList : Page
    {
        private LogReader _logReader;
        private readonly HypixelApi _hypixelApi;
        private readonly ObservableCollection<Player> _players = new ObservableCollection<Player>();
        private const string ApiKeySettingName = "HypixelApiKey";
        private const string LogFileSettingName = "LogFilePath";

        public PlayerList()
        {
            InitializeComponent();
            _hypixelApi = new HypixelApi();

            LoadApiKey();
            LoadLogReader();

            PlayersListView.ItemsSource = _players;
        }

        private void LoadApiKey()
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ApiKeySettingName, out object? apiKey))
            {
                _hypixelApi.SetApiKey((string)apiKey!);
            }
        }

        private void LoadLogReader()
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(LogFileSettingName, out object? logFilePath))
            {
                _logReader = new LogReader((string)logFilePath!);
            }
            else
            {
                _logReader = new LogReader();
            }
            _logReader.PlayerJoined += OnPlayerJoined;
            _logReader.PlayerLeft += OnPlayerLeft;
            _logReader.WhoResult += OnWhoResult;
            _logReader.Start();
        }

        private async void OnPlayerJoined(string username)
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
                            Finals = stats.Finals,
                            Wins = stats.Wins,
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
                        // If stats are null, just set IsLoading to false on the existing player
                        existingPlayer.IsLoading = false;
                        DebugService.Instance.Log($"[PlayerList] No stats received for {username}. Set IsLoading to false.");
                    }
                }
            });
        }

        private void OnPlayerLeft(string username)
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
    }
}