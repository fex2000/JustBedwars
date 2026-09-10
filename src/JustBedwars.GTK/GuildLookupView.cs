using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JustBedwars.Models;
using JustBedwars.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JustBedwars.GTK;

public static class GuildLookupView
{
    private const string HistorySettingsKey = "GuildSearchHistory";
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();
    private static readonly HttpClient _httpClient = new();
    private static List<string> _history = new();

    private static Gtk.Entry? _searchEntry;
    private static Gtk.ComboBoxText? _modeCombo;
    private static Gtk.Spinner? _spinner;
    private static Gtk.Box? _resultBox;
    private static Gtk.ListBox? _historyListBox;

    // Guild Header labels
    private static Gtk.Label? _guildNameLabel;
    private static Gtk.Label? _guildTagLabel;
    private static Gtk.Label? _guildLevelLabel;

    // Info Tab labels
    private static Gtk.Label? _descLabel;
    private static Gtk.Label? _prefGamesLabel;
    private static Gtk.Label? _levelInfoLabel;
    private static Gtk.Label? _expInfoLabel;
    private static Gtk.Label? _createdAtLabel;
    private static Gtk.Label? _guildIdLabel;
    private static Gtk.ListBox? _expByGameListBox;

    // Players Tab
    private static Gtk.ListBox? _membersListBox;
    private static Gtk.ComboBoxText? _rankFilterCombo;
    private static List<GuildMember> _currentMembers = new();

    // Ranks Tab
    private static Gtk.ListBox? _ranksListBox;

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
        LoadHistory();

        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        mainBox.SetMarginStart(15);
        mainBox.SetMarginEnd(15);
        mainBox.SetMarginTop(15);
        mainBox.SetMarginBottom(15);

        // Search Header
        var searchBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        searchBox.Valign = Gtk.Align.Center;

        _modeCombo = Gtk.ComboBoxText.New();
        _modeCombo.Valign = Gtk.Align.Center;
        _modeCombo.AppendText("Guild Name");
        _modeCombo.AppendText("Guild ID");
        _modeCombo.AppendText("Member Name");
        _modeCombo.AppendText("Member UUID");
        _modeCombo.SetActive(0);
        searchBox.Append(_modeCombo);

        _searchEntry = Gtk.Entry.New();
        _searchEntry.PlaceholderText = "Search Guild...";
        _searchEntry.Hexpand = true;
        _searchEntry.Valign = Gtk.Align.Center;
        searchBox.Append(_searchEntry);

        var searchBtn = Gtk.Button.NewWithLabel("Search");
        searchBtn.Valign = Gtk.Align.Center;
        searchBox.Append(searchBtn);
        mainBox.Append(searchBox);

        // Spinner
        _spinner = Gtk.Spinner.New();
        _spinner.HeightRequest = 30;
        _spinner.Visible = false;
        mainBox.Append(_spinner);

        // Main Scrolled Content
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;

        var container = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        scrolledWindow.Child = container;
        mainBox.Append(scrolledWindow);

        // History Section
        var historyGroup = Adw.PreferencesGroup.New();
        historyGroup.Title = "Recent Search History";
        container.Append(historyGroup);

        _historyListBox = Gtk.ListBox.New();
        historyGroup.Add(_historyListBox);
        RebuildHistoryList();

        _historyListBox.OnRowActivated += (sender, args) =>
        {
            if (args.Row is Gtk.ListBoxRow row && !string.IsNullOrEmpty(row.Name))
            {
                if (_searchEntry != null) _searchEntry.Text_ = row.Name;
                PerformSearch(row.Name, "Guild Name");
            }
        };

        // Result Container (Initially hidden)
        _resultBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        _resultBox.Visible = false;
        container.Append(_resultBox);

        // Guild Banner Header
        var bannerGroup = Adw.PreferencesGroup.New();
        _resultBox.Append(bannerGroup);

        var bannerRow = Adw.ActionRow.New();
        _guildNameLabel = Gtk.Label.New("Guild Name");
        _guildNameLabel.UseMarkup = true;
        bannerRow.Title = "<b>Guild Name</b>";
        _guildTagLabel = Gtk.Label.New("[TAG]");
        bannerRow.AddSuffix(_guildTagLabel);
        _guildLevelLabel = Gtk.Label.New("Level: 0");
        bannerRow.AddSuffix(_guildLevelLabel);
        bannerGroup.Add(bannerRow);

        // Stack & StackSwitcher for Tabs (Info, Players, Ranks)
        var stack = Gtk.Stack.New();
        stack.SetTransitionType(Gtk.StackTransitionType.SlideLeftRight);

        var switcher = Gtk.StackSwitcher.New();
        switcher.Stack = stack;
        switcher.Halign = Gtk.Align.Center;
        _resultBox.Append(switcher);
        _resultBox.Append(stack);

        // TAB 1: INFO
        var infoBox = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        infoBox.SetMarginStart(10);
        infoBox.SetMarginEnd(10);
        infoBox.SetMarginTop(10);

        var infoGroup = Adw.PreferencesGroup.New();
        infoGroup.Title = "Guild Details";
        infoBox.Append(infoGroup);

        var descRow = Adw.ActionRow.New();
        descRow.Title = "Description";
        _descLabel = Gtk.Label.New("-");
        descRow.AddSuffix(_descLabel);
        infoGroup.Add(descRow);

        var prefRow = Adw.ActionRow.New();
        prefRow.Title = "Preferred Games";
        _prefGamesLabel = Gtk.Label.New("-");
        prefRow.AddSuffix(_prefGamesLabel);
        infoGroup.Add(prefRow);

        var levelRow = Adw.ActionRow.New();
        levelRow.Title = "Level";
        _levelInfoLabel = Gtk.Label.New("-");
        levelRow.AddSuffix(_levelInfoLabel);
        infoGroup.Add(levelRow);

        var expRow = Adw.ActionRow.New();
        expRow.Title = "Experience";
        _expInfoLabel = Gtk.Label.New("-");
        expRow.AddSuffix(_expInfoLabel);
        infoGroup.Add(expRow);

        var createdRow = Adw.ActionRow.New();
        createdRow.Title = "Created At";
        _createdAtLabel = Gtk.Label.New("-");
        createdRow.AddSuffix(_createdAtLabel);
        infoGroup.Add(createdRow);

        var idRow = Adw.ActionRow.New();
        idRow.Title = "Guild ID";
        _guildIdLabel = Gtk.Label.New("-");
        idRow.AddSuffix(_guildIdLabel);
        infoGroup.Add(idRow);

        // Expander for Exp by Game Type
        var expExpander = Gtk.Expander.New("Experience by Game");
        _expByGameListBox = Gtk.ListBox.New();
        expExpander.Child = _expByGameListBox;
        infoBox.Append(expExpander);

        stack.AddTitled(infoBox, "info", "Info");

        // TAB 2: PLAYERS
        var playersBox = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        playersBox.SetMarginStart(10);
        playersBox.SetMarginEnd(10);
        playersBox.SetMarginTop(10);

        var filterBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        var filterLabel = Gtk.Label.New("Filter Rank:");
        filterBox.Append(filterLabel);

        _rankFilterCombo = Gtk.ComboBoxText.New();
        _rankFilterCombo.AppendText("All");
        _rankFilterCombo.SetActive(0);
        _rankFilterCombo.OnChanged += (s, e) => FilterMembers();
        filterBox.Append(_rankFilterCombo);
        playersBox.Append(filterBox);

        _membersListBox = Gtk.ListBox.New();
        playersBox.Append(_membersListBox);

        _membersListBox.OnRowActivated += (sender, args) =>
        {
            if (args.Row is Gtk.ListBoxRow row && !string.IsNullOrEmpty(row.Name))
            {
                MainWindow.NavigateToStatsPage(row.Name);
            }
        };

        stack.AddTitled(playersBox, "players", "Players");

        // TAB 3: RANKS
        var ranksBox = Gtk.Box.New(Gtk.Orientation.Vertical, 10);
        ranksBox.SetMarginStart(10);
        ranksBox.SetMarginEnd(10);
        ranksBox.SetMarginTop(10);

        _ranksListBox = Gtk.ListBox.New();
        ranksBox.Append(_ranksListBox);

        stack.AddTitled(ranksBox, "ranks", "Ranks");

        // Event Listeners for Search
        searchBtn.OnClicked += (s, e) => TriggerSearch();
        _searchEntry.OnActivate += (s, e) => TriggerSearch();

        return mainBox;
    }

    private static void TriggerSearch()
    {
        if (_searchEntry == null || _modeCombo == null) return;
        var query = _searchEntry.Text_;
        var mode = _modeCombo.GetActiveText() ?? "Guild Name";

        if (!string.IsNullOrWhiteSpace(query))
        {
            PerformSearch(query, mode);
        }
    }

    private static async void PerformSearch(string query, string mode)
    {
        if (_spinner == null || _resultBox == null) return;

        AddToHistory(query);

        _spinner.Visible = true;
        _spinner.Start();
        _resultBox.Visible = false;

        var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
        _hypixelApi.SetApiKey(apiKey ?? "");

        try
        {
            var guild = await _hypixelApi.GetGuildAsync(query, mode);
            if (guild != null)
            {
                DisplayGuild(guild);
            }
            else
            {
                if (_guildNameLabel != null) _guildNameLabel.Label_ = "<b>Guild Not Found</b>";
                _resultBox.Visible = true;
            }
        }
        catch (Exception ex)
        {
            DebugService.Instance.Log($"[GuildLookupView] Error looking up guild: {ex.Message}");
        }
        finally
        {
            _spinner.Stop();
            _spinner.Visible = false;
        }
    }

    private static void DisplayGuild(Guild guild)
    {
        if (_resultBox == null) return;

        // Banner
        if (_guildNameLabel != null) _guildNameLabel.Label_ = $"<b>{guild.Name}</b>";
        if (_guildTagLabel != null) _guildTagLabel.Label_ = string.IsNullOrEmpty(guild.Tag) ? "" : $"[{guild.Tag}]";
        if (_guildLevelLabel != null) _guildLevelLabel.Label_ = $"Level: {guild.Level:F1}";

        // Info Tab
        if (_descLabel != null) _descLabel.Label_ = string.IsNullOrEmpty(guild.Description) ? "No description available." : guild.Description;
        if (_prefGamesLabel != null) _prefGamesLabel.Label_ = guild.PreferredGames.Count > 0 ? string.Join(", ", guild.PreferredGames) : "Not set.";
        if (_levelInfoLabel != null) _levelInfoLabel.Label_ = guild.Level.ToString("F2");
        if (_expInfoLabel != null) _expInfoLabel.Label_ = guild.Exp.ToString("N0");
        if (_createdAtLabel != null)
        {
            var createdDate = DateTimeOffset.FromUnixTimeMilliseconds(guild.Created).ToString("dd.MM.yyyy");
            _createdAtLabel.Label_ = createdDate;
        }
        if (_guildIdLabel != null) _guildIdLabel.Label_ = guild.Id;

        // Populate Exp by Game
        if (_expByGameListBox != null)
        {
            while (_expByGameListBox.GetFirstChild() is Gtk.Widget c) _expByGameListBox.Remove(c);
            foreach (var kvp in guild.ExpByGameType)
            {
                var row = Adw.ActionRow.New();
                row.Title = kvp.Key;
                var valLabel = Gtk.Label.New(kvp.Value.ToString("N0"));
                row.AddSuffix(valLabel);
                _expByGameListBox.Append(row);
            }
        }

        // Players Tab
        _currentMembers = guild.Members;
        if (_rankFilterCombo != null)
        {
            _rankFilterCombo.RemoveAll();
            _rankFilterCombo.AppendText("All");
            var ranks = guild.Members.Select(m => m.Rank).Distinct().Where(r => !string.IsNullOrEmpty(r)).ToList();
            foreach (var r in ranks) _rankFilterCombo.AppendText(r);
            _rankFilterCombo.SetActive(0);
        }
        FilterMembers();

        // Ranks Tab
        if (_ranksListBox != null)
        {
            while (_ranksListBox.GetFirstChild() is Gtk.Widget c) _ranksListBox.Remove(c);
            foreach (var rank in guild.Ranks)
            {
                var row = Adw.ActionRow.New();
                row.Title = rank.Name;
                if (!string.IsNullOrEmpty(rank.Tag))
                {
                    var tagLabel = Gtk.Label.New($"[{rank.Tag}]");
                    row.AddSuffix(tagLabel);
                }
                var prioLabel = Gtk.Label.New($"Priority: {rank.Priority}");
                row.AddSuffix(prioLabel);
                _ranksListBox.Append(row);
            }
        }

        _resultBox.Visible = true;
    }

    private static void FilterMembers()
    {
        if (_membersListBox == null || _rankFilterCombo == null) return;
        while (_membersListBox.GetFirstChild() is Gtk.Widget c) _membersListBox.Remove(c);

        var selectedRank = _rankFilterCombo.GetActiveText() ?? "All";
        var filtered = selectedRank == "All"
            ? _currentMembers
            : _currentMembers.Where(m => m.Rank == selectedRank).ToList();

        foreach (var member in filtered)
        {
            var row = Gtk.ListBoxRow.New();
            row.Name = member.Name;

            var actionRow = Adw.ActionRow.New();
            actionRow.Title = string.IsNullOrEmpty(member.Name) ? member.Uuid : member.Name;
            actionRow.Subtitle = $"Rank: {member.Rank}";

            if (member.Joined > 0)
            {
                var joinDate = DateTimeOffset.FromUnixTimeMilliseconds(member.Joined).ToString("dd.MM.yyyy");
                var joinLabel = Gtk.Label.New($"Joined: {joinDate}");
                actionRow.AddSuffix(joinLabel);
            }

            row.SetChild(actionRow);
            _membersListBox.Append(row);
        }
    }

    private static void LoadHistory()
    {
        var historyObj = _settingsService.GetValue(HistorySettingsKey);
        if (historyObj is JArray jArray)
        {
            _history = jArray.ToObject<List<string>>() ?? new List<string>();
        }
    }

    private static void SaveHistory()
    {
        _settingsService.SetValue(HistorySettingsKey, _history.ToArray());
    }

    private static void AddToHistory(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        _history.RemoveAll(h => h.Equals(query, StringComparison.OrdinalIgnoreCase));
        _history.Insert(0, query);

        if (_history.Count > 10) _history = _history.Take(10).ToList();

        SaveHistory();
        RebuildHistoryList();
    }

    private static void RebuildHistoryList()
    {
        if (_historyListBox == null) return;
        while (_historyListBox.GetFirstChild() is Gtk.Widget c) _historyListBox.Remove(c);

        foreach (var item in _history)
        {
            var row = Gtk.ListBoxRow.New();
            row.Name = item;

            var box = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
            box.SetMarginStart(10);
            box.SetMarginEnd(10);
            box.SetMarginTop(5);
            box.SetMarginBottom(5);

            var label = Gtk.Label.New(item);
            label.Hexpand = true;
            label.Halign = Gtk.Align.Start;
            box.Append(label);

            var deleteBtn = Gtk.Button.NewWithLabel("✕");
            var historyItem = item;
            deleteBtn.OnClicked += (s, e) =>
            {
                _history.Remove(historyItem);
                SaveHistory();
                RebuildHistoryList();
            };
            box.Append(deleteBtn);

            row.SetChild(box);
            _historyListBox.Append(row);
        }
    }
}
