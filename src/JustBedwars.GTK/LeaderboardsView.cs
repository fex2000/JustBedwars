using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JustBedwars.Models;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class LeaderboardsView
{
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();
    private static readonly HttpClient _httpClient = new();

    private static readonly Dictionary<string, List<LeaderboardEntry>> _fullLeaderboardCache = new();
    private static string _selectedLeaderboard = "Stars";
    private static string _selectedTimeFilter = "Weekly";

    private static Gtk.ListBox? _listBox;
    private static Gtk.Spinner? _spinner;
    private static Gtk.Box? _timeFilterBox;
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

        // Header controls (Selector + TimeFilter + Hint)
        var headerBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);

        // Leaderboard Category Buttons
        var categoryBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);

        var starsBtn = Gtk.Button.NewWithLabel("Level");
        var winsBtn = Gtk.Button.NewWithLabel("Wins");
        var finalsBtn = Gtk.Button.NewWithLabel("Finals");

        categoryBox.Append(starsBtn);
        categoryBox.Append(winsBtn);
        categoryBox.Append(finalsBtn);
        headerBox.Append(categoryBox);

        // Time Filter (Weekly / Lifetime)
        _timeFilterBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        _timeFilterBox.Visible = false; // Hidden for Stars by default

        var weeklyBtn = Gtk.Button.NewWithLabel("Weekly");
        var lifetimeBtn = Gtk.Button.NewWithLabel("Lifetime");
        _timeFilterBox.Append(weeklyBtn);
        _timeFilterBox.Append(lifetimeBtn);
        headerBox.Append(_timeFilterBox);

        var hintLabel = Gtk.Label.New("Click players for their Stats!");
        hintLabel.Hexpand = true;
        hintLabel.Halign = Gtk.Align.End;
        hintLabel.SetMarginEnd(10);
        headerBox.Append(hintLabel);

        mainBox.Append(headerBox);

        // Loading Spinner
        _spinner = Gtk.Spinner.New();
        _spinner.HeightRequest = 40;
        _spinner.WidthRequest = 40;
        _spinner.Halign = Gtk.Align.Center;
        _spinner.Visible = false;
        mainBox.Append(_spinner);

        // Leaderboard List inside ScrolledWindow
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;

        _listBox = Gtk.ListBox.New();
        scrolledWindow.Child = _listBox;
        mainBox.Append(scrolledWindow);

        _listBox.OnRowActivated += (sender, args) =>
        {
            if (args.Row is Gtk.ListBoxRow row)
            {
                var name = row.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    MainWindow.NavigateToStatsPage(name);
                }
            }
        };

        // Event Handlers
        starsBtn.OnClicked += (s, e) =>
        {
            _selectedLeaderboard = "Stars";
            if (_timeFilterBox != null) _timeFilterBox.Visible = false;
            SwitchLeaderboard();
        };

        winsBtn.OnClicked += (s, e) =>
        {
            _selectedLeaderboard = "Wins";
            if (_timeFilterBox != null) _timeFilterBox.Visible = true;
            SwitchLeaderboard();
        };

        finalsBtn.OnClicked += (s, e) =>
        {
            _selectedLeaderboard = "Finals";
            if (_timeFilterBox != null) _timeFilterBox.Visible = true;
            SwitchLeaderboard();
        };

        weeklyBtn.OnClicked += (s, e) =>
        {
            _selectedTimeFilter = "Weekly";
            SwitchLeaderboard();
        };

        lifetimeBtn.OnClicked += (s, e) =>
        {
            _selectedTimeFilter = "Lifetime";
            SwitchLeaderboard();
        };

        // Load default leaderboard
        SwitchLeaderboard();

        return mainBox;
    }

    private static void SwitchLeaderboard()
    {
        var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
        _hypixelApi.SetApiKey(apiKey ?? "");

        var cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";
        if (_fullLeaderboardCache.ContainsKey(cacheKey))
        {
            PopulateList(_fullLeaderboardCache[cacheKey]);
        }
        else
        {
            LoadLeaderboard();
        }
    }

    private static async void LoadLeaderboard()
    {
        if (_listBox == null || _spinner == null) return;

        var cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";

        _spinner.Visible = true;
        _spinner.Start();
        ClearListBox();

        try
        {
            var entries = await _hypixelApi.GetLeaderboard(_selectedLeaderboard, _selectedTimeFilter);
            await _hypixelApi.GetNamesForLeaderboardEntries(entries, null);

            _fullLeaderboardCache[cacheKey] = entries;
            PopulateList(entries);
        }
        catch (Exception ex)
        {
            DebugService.Instance.Log($"[LeaderboardsView] Error loading leaderboard: {ex.Message}");
        }
        finally
        {
            _spinner.Stop();
            _spinner.Visible = false;
        }
    }

    private static void ClearListBox()
    {
        if (_listBox == null) return;
        while (_listBox.GetFirstChild() is Gtk.Widget child)
        {
            _listBox.Remove(child);
        }
    }

    private static void PopulateList(List<LeaderboardEntry> entries)
    {
        if (_listBox == null) return;
        ClearListBox();

        foreach (var entry in entries)
        {
            var row = Gtk.ListBoxRow.New();
            row.Name = entry.Name;

            var box = Gtk.Box.New(Gtk.Orientation.Horizontal, 15);
            box.SetMarginStart(10);
            box.SetMarginEnd(10);
            box.SetMarginTop(8);
            box.SetMarginBottom(8);

            // Rank Label
            var rankLabel = Gtk.Label.New($"#{entry.Rank}");
            rankLabel.WidthRequest = 45;
            rankLabel.Halign = Gtk.Align.Start;
            rankLabel.UseMarkup = true;
            rankLabel.Label_ = $"<b>#{entry.Rank}</b>";
            box.Append(rankLabel);

            // Avatar Image
            var image = Gtk.Image.New();
            image.SetPixelSize(36);
            box.Append(image);

            // Fetch avatar asynchronously
            if (!string.IsNullOrEmpty(entry.Uuid))
            {
                var uuid = entry.Uuid;
                Task.Run(async () =>
                {
                    try
                    {
                        var avatarFile = Path.Combine(_avatarCacheDir, $"{uuid}_face.png");
                        if (!File.Exists(avatarFile))
                        {
                            var bytes = await _httpClient.GetByteArrayAsync($"https://skins.jbw.fexei.at/face/{uuid}");
                            await File.WriteAllBytesAsync(avatarFile, bytes);
                        }
                        RunOnMainThread(() =>
                        {
                            if (File.Exists(avatarFile))
                            {
                                image.SetFromFile(avatarFile);
                            }
                        });
                    }
                    catch
                    {
                        // Ignore image fetch error
                    }
                });
            }

            // Player Name Label
            var nameLabel = Gtk.Label.New(entry.Name);
            nameLabel.Halign = Gtk.Align.Start;
            nameLabel.Hexpand = true;
            box.Append(nameLabel);

            // Value Label
            if (!string.IsNullOrEmpty(entry.Value))
            {
                var valLabel = Gtk.Label.New(entry.Value);
                valLabel.Halign = Gtk.Align.End;
                box.Append(valLabel);
            }

            row.SetChild(box);
            _listBox.Append(row);
        }
    }
}
