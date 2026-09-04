using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JustBedwars.Models;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class PlayerListView
{
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();
    private static readonly HttpClient _httpClient = new();
    private static LogReader? _logReader;
    private static readonly List<Player> _players = new();
    private static Gtk.ListBox? _listBox;
    private static Gtk.Box? _emptyStateBox;
    private static string _avatarCacheDir = "";

    private static void RunOnMainThread(Action action)
    {
        GLib.Functions.IdleAdd(0, () =>
        {
            action();
            return false;
        });
    }

    public static Gtk.Widget Create()
    {
        _avatarCacheDir = Path.Combine(Path.GetTempPath(), "JustBedwarsCache");
        Directory.CreateDirectory(_avatarCacheDir);

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

        // Empty State Container with Troubleshooting Tips
        _emptyStateBox = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        _emptyStateBox.Hexpand = true;
        _emptyStateBox.Vexpand = true;
        _emptyStateBox.Valign = Gtk.Align.Center;

        var emptyMsgLabel = Gtk.Label.New("Join a bedwars round and after it starts run '/who' to list players here!");
        emptyMsgLabel.Justify = Gtk.Justification.Center;
        emptyMsgLabel.UseMarkup = true;
        emptyMsgLabel.Label_ = "<span size='large'>Join a bedwars round and after it starts run '/who' to list players here!</span>";
        _emptyStateBox.Append(emptyMsgLabel);

        var expander = Gtk.Expander.New("Troubleshooting Tips");
        expander.Halign = Gtk.Align.Center;
        expander.WidthRequest = 450;

        var tipsBox = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        tipsBox.SetMarginStart(10);
        tipsBox.SetMarginEnd(10);
        tipsBox.SetMarginTop(10);
        tipsBox.SetMarginBottom(10);

        tipsBox.Append(Gtk.Label.New("• Check if the log file path in settings is set correctly"));
        tipsBox.Append(Gtk.Label.New("• Check if there are no mods/client that modify log format"));
        tipsBox.Append(Gtk.Label.New("• Ensure Minecraft language is set to English"));
        tipsBox.Append(Gtk.Label.New("• Works on Hypixel (and bedwarspractice.club)"));
        expander.Child = tipsBox;

        _emptyStateBox.Append(expander);
        mainBox.Append(_emptyStateBox);

        // Scrollable ListBox for Players
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;
        mainBox.Append(scrolledWindow);

        _listBox = Gtk.ListBox.New();
        scrolledWindow.Child = _listBox;

        _listBox.OnRowActivated += (sender, args) =>
        {
            if (args.Row is Gtk.ListBoxRow row && !string.IsNullOrEmpty(row.Name))
            {
                var player = _players.FirstOrDefault(p => p.Username == row.Name);
                if (player != null)
                {
                    player.IsExpanded = !player.IsExpanded;
                    RebuildList();
                }
            }
        };

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

        while (_listBox.GetFirstChild() is Gtk.Widget child)
        {
            _listBox.Remove(child);
        }

        foreach (var player in _players)
        {
            var row = Gtk.ListBoxRow.New();
            row.Name = player.Username;

            var actionRow = Adw.ActionRow.New();
            var starText = player.Star > 0 ? $"[★{player.Star}]" : "";
            actionRow.Title = $"{player.Username} {starText}";

            // Face image suffix
            var avatarImage = Gtk.Image.New();
            avatarImage.SetPixelSize(28);
            actionRow.AddPrefix(avatarImage);

            if (!string.IsNullOrEmpty(player.Username))
            {
                var username = player.Username;
                Task.Run(async () =>
                {
                    try
                    {
                        var avatarFile = Path.Combine(_avatarCacheDir, $"{username}_face.png");
                        if (!File.Exists(avatarFile))
                        {
                            var bytes = await _httpClient.GetByteArrayAsync($"https://skins.jbw.fexei.at/face/{username}");
                            await File.WriteAllBytesAsync(avatarFile, bytes);
                        }
                        RunOnMainThread(() =>
                        {
                            if (File.Exists(avatarFile))
                            {
                                avatarImage.SetFromFile(avatarFile);
                            }
                        });
                    }
                    catch
                    {
                        // Ignore avatar fetch error
                    }
                });
            }

            if (player.IsLoading)
            {
                actionRow.Subtitle = "Loading player metrics...";
                var spinner = Gtk.Spinner.New();
                spinner.Start();
                actionRow.AddSuffix(spinner);
            }
            else
            {
                if (player.IsExpanded)
                {
                    actionRow.Subtitle = $"FKDR: {player.FKDR:F2} | WLR: {player.WLR:F2} | BBLR: {player.BBLR:F2} | KDR: {player.KDR:F2}\n" +
                                         $"Finals: {player.Finals} | Wins: {player.Wins} | Beds: {player.Beds} | Kills: {player.Kills}";
                }
                else
                {
                    actionRow.Subtitle = $"FKDR: {player.FKDR:F2} | WLR: {player.WLR:F2} | BBLR: {player.BBLR:F2} | Score: {player.Score:F0}";
                }

                var tag = string.IsNullOrEmpty(player.PlayerTag) || player.PlayerTag == "-" ? "Score: " + player.Score.ToString("F0") : player.PlayerTag;
                var tagLabel = Gtk.Label.New(tag);
                actionRow.AddSuffix(tagLabel);
            }

            row.SetChild(actionRow);
            _listBox.Append(row);
        }

        UpdatePlaceholderVisibility();
    }

    private static void UpdatePlaceholderVisibility()
    {
        if (_emptyStateBox != null)
        {
            _emptyStateBox.Visible = _players.Count == 0;
        }
        if (_listBox != null)
        {
            _listBox.Visible = _players.Count > 0;
        }
    }
}