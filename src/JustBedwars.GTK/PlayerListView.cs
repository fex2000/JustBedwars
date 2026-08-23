using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustBedwars.Models;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class PlayerListView
{
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();
    private static LogReader? _logReader;
    private static readonly List<Player> _players = new();
    private static Gtk.ListBox? _listBox;
    private static Gtk.Label? _placeholderLabel;

    private static void RunOnMainThread(Action action)
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            action();
            return false; // Run once
        });
    }

    public static Gtk.Widget Create()
    {
        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        mainBox.SetMarginStart(15);
        mainBox.SetMarginEnd(15);
        mainBox.SetMarginTop(15);
        mainBox.SetMarginBottom(15);

        // Header controls (Title + Actions)
        var headerBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        var titleLabel = Gtk.Label.New("Active Players");
        titleLabel.Halign = Gtk.Align.Start;
        titleLabel.Hexpand = true;
        titleLabel.UseMarkup = true;
        titleLabel.Label_ = "<b>Active Players</b>";
        headerBox.Append(titleLabel);

        var clearBtn = Gtk.Button.NewWithLabel("Clear List");
        clearBtn.OnClicked += (sender, args) => ClearPlayers();
        headerBox.Append(clearBtn);
        mainBox.Append(headerBox);

        // Manual Add Player Box
        var addBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        var addEntry = Gtk.Entry.New();
        addEntry.PlaceholderText = "Add player manually...";
        addEntry.Hexpand = true;
        addBox.Append(addEntry);

        var addBtn = Gtk.Button.NewWithLabel("Add");
        addBtn.OnClicked += (sender, args) =>
        {
            var name = addEntry.Text_;
            if (!string.IsNullOrWhiteSpace(name))
            {
                AddPlayer(name);
                addEntry.Text_ = "";
            }
        };
        addEntry.OnActivate += (sender, args) =>
        {
            var name = addEntry.Text_;
            if (!string.IsNullOrWhiteSpace(name))
            {
                AddPlayer(name);
                addEntry.Text_ = "";
            }
        };
        addBox.Append(addBtn);
        mainBox.Append(addBox);

        // Placeholder for Empty State
        _placeholderLabel = Gtk.Label.New("No players in current round.\nRun /who in-game to populate, or add a player manually.");
        _placeholderLabel.Hexpand = true;
        _placeholderLabel.Vexpand = true;
        _placeholderLabel.Justify = Gtk.Justification.Center;
        mainBox.Append(_placeholderLabel);

        // Scrollable ListBox for Players
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;
        mainBox.Append(scrolledWindow);

        _listBox = Gtk.ListBox.New();
        scrolledWindow.Child = _listBox;

        // Load API and LogReader
        LoadApiKey();
        StartLogReader();

        UpdatePlaceholderVisibility();

        return mainBox;
    }

    private static void LoadApiKey()
    {
        var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _hypixelApi.SetApiKey(apiKey);
        }
    }

    private static void StartLogReader()
    {
        try
        {
            var logFilePath = _settingsService.GetValue("LogFilePath") as string;
            if (!string.IsNullOrEmpty(logFilePath))
            {
                _logReader = new LogReader(logFilePath);
            }
            else
            {
                _logReader = new LogReader();
            }

            _logReader.PlayerJoined += OnPlayerJoined;
            _logReader.PlayerLeft += OnPlayerLeft;
            _logReader.WhoResult += OnWhoResult;
            _logReader.ClearList += OnClearList;

            _logReader.Start();
        }
        catch (Exception ex)
        {
            DebugService.Instance.Log($"[GTK PlayerList] Error starting LogReader: {ex.Message}");
        }
    }

    private static void OnPlayerJoined(string username)
    {
        RunOnMainThread(() => AddPlayer(username));
    }

    private static void OnPlayerLeft(string username)
    {
        RunOnMainThread(() => RemovePlayer(username));
    }

    private static void OnWhoResult(List<string> players)
    {
        RunOnMainThread(() =>
        {
            ClearPlayers();
            foreach (var p in players)
            {
                AddPlayer(p);
            }
        });
    }

    private static void OnClearList()
    {
        RunOnMainThread(() => ClearPlayers());
    }

    private static void AddPlayer(string username)
    {
        if (_players.Any(p => p.Username == username)) return;

        var player = new Player { Username = username, IsLoading = true };
        _players.Add(player);
        RebuildList();

        // Asynchronously fetch stats
        Task.Run(async () =>
        {
            var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
            _hypixelApi.SetApiKey(apiKey ?? "");

            var stats = await _hypixelApi.GetPlayerStats(username);

            RunOnMainThread(() =>
            {
                var existing = _players.FirstOrDefault(p => p.Username == username);
                if (existing != null)
                {
                    if (stats != null)
                    {
                        existing.Username = stats.Username;
                        existing.Star = stats.Star;
                        existing.FKDR = stats.FKDR;
                        existing.WLR = stats.WLR;
                        existing.BBLR = stats.BBLR;
                        existing.Finals = stats.Finals;
                        existing.FinalDeaths = stats.FinalDeaths;
                        existing.Kills = stats.Kills;
                        existing.Deaths = stats.Deaths;
                        existing.KDR = stats.KDR;
                        existing.Beds = stats.Beds;
                        existing.BedsLost = stats.BedsLost;
                        existing.Wins = stats.Wins;
                        existing.Losses = stats.Losses;
                        existing.PlayerTag = stats.PlayerTag;
                        existing.FirstLogin = stats.FirstLogin;
                        existing.IsLoading = false;
                    }
                    else
                    {
                        existing.IsLoading = false;
                        existing.PlayerTag = "ERROR";
                    }

                    SortPlayers();
                    RebuildList();
                }
            });
        });
    }

    private static void RemovePlayer(string username)
    {
        var player = _players.FirstOrDefault(p => p.Username == username);
        if (player != null)
        {
            _players.Remove(player);
            RebuildList();
        }
    }

    private static void ClearPlayers()
    {
        _players.Clear();
        RebuildList();
    }

    private static void SortPlayers()
    {
        var sortingMode = _settingsService.GetValue("PlayerSorting") as string ?? "JustBedwars Score";
        IOrderedEnumerable<Player> sorted;

        switch (sortingMode)
        {
            case "Skill Index":
                sorted = _players
                    .OrderBy(p => string.IsNullOrWhiteSpace(p.PlayerTag) || p.PlayerTag == "-")
                    .ThenByDescending(p => p.Star * p.FKDR * p.FKDR);
                break;
            case "FKDR":
                sorted = _players
                    .OrderBy(p => string.IsNullOrWhiteSpace(p.PlayerTag) || p.PlayerTag == "-")
                    .ThenByDescending(p => p.FKDR);
                break;
            case "WLR":
                sorted = _players
                    .OrderBy(p => string.IsNullOrWhiteSpace(p.PlayerTag) || p.PlayerTag == "-")
                    .ThenByDescending(p => p.WLR);
                break;
            case "Stars":
                sorted = _players
                    .OrderBy(p => string.IsNullOrWhiteSpace(p.PlayerTag) || p.PlayerTag == "-")
                    .ThenByDescending(p => p.Star);
                break;
            case "JustBedwars Score":
            default:
                sorted = _players
                    .OrderBy(p => string.IsNullOrWhiteSpace(p.PlayerTag) || p.PlayerTag == "-")
                    .ThenByDescending(p => p.Score);
                break;
        }

        var sortedList = sorted.ToList();
        _players.Clear();
        _players.AddRange(sortedList);
    }

    private static void RebuildList()
    {
        if (_listBox == null) return;

        // Clear existing children from ListBox
        while (_listBox.GetFirstChild() is Gtk.Widget child)
        {
            _listBox.Remove(child);
        }

        foreach (var player in _players)
        {
            var row = Adw.ActionRow.New();
            var starText = player.Star > 0 ? $"[★{player.Star}]" : "";
            row.Title = $"{player.Username} {starText}";

            if (player.IsLoading)
            {
                row.Subtitle = "Loading player metrics...";
                var spinnerLabel = Gtk.Label.New("Loading...");
                row.AddSuffix(spinnerLabel);
            }
            else
            {
                row.Subtitle = $"FKDR: {player.FKDR:F2} | WLR: {player.WLR:F2} | BBLR: {player.BBLR:F2} | Score: {player.Score:F0}";
                var tag = string.IsNullOrEmpty(player.PlayerTag) || player.PlayerTag == "-" ? "Score: " + player.Score.ToString("F0") : player.PlayerTag;
                var tagLabel = Gtk.Label.New(tag);
                row.AddSuffix(tagLabel);
            }

            _listBox.Append(row);
        }

        UpdatePlaceholderVisibility();
    }

    private static void UpdatePlaceholderVisibility()
    {
        if (_placeholderLabel != null)
        {
            _placeholderLabel.Visible = _players.Count == 0;
        }
        if (_listBox != null)
        {
            _listBox.Visible = _players.Count > 0;
        }
    }
}