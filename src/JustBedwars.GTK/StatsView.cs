using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JustBedwars.Models;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class StatsView
{
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();
    private static readonly HttpClient _httpClient = new();
    private static string _avatarCacheDir = "";

    private static Gtk.Entry? _searchEntry;
    private static Gtk.Button? _searchButton;
    private static Gtk.Spinner? _spinner;
    private static Gtk.Box? _contentBox;

    // Progress Bars
    private static Gtk.Label? _hypixelLevelLabel;
    private static Gtk.ProgressBar? _hypixelLevelBar;
    private static Gtk.Label? _bedwarsLevelLabel;
    private static Gtk.ProgressBar? _bedwarsLevelBar;

    // Grid Image & Card Labels
    private static Gtk.Image? _playerAvatarImage;
    private static Gtk.Label? _usernameValLabel;
    private static Gtk.Label? _starValLabel;
    private static Gtk.Label? _tagValLabel;
    private static Gtk.Label? _firstLoginValLabel;

    private static Gtk.Label? _fkdrValLabel;
    private static Gtk.Label? _wlrValLabel;
    private static Gtk.Label? _kdrValLabel;
    private static Gtk.Label? _bblrValLabel;

    private static Gtk.Label? _finalsValLabel;
    private static Gtk.Label? _finalDeathsValLabel;
    private static Gtk.Label? _winsValLabel;
    private static Gtk.Label? _lossesValLabel;
    private static Gtk.Label? _killsValLabel;
    private static Gtk.Label? _deathsValLabel;
    private static Gtk.Label? _bedsValLabel;
    private static Gtk.Label? _bedsLostValLabel;

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

        // Search Section
        var searchBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        searchBox.Valign = Gtk.Align.Center;
        _searchEntry = Gtk.Entry.New();
        _searchEntry.PlaceholderText = "Search Player Username...";
        _searchEntry.Hexpand = true;
        _searchEntry.Valign = Gtk.Align.Center;
        searchBox.Append(_searchEntry);

        _searchButton = Gtk.Button.NewWithLabel("Search");
        _searchButton.Valign = Gtk.Align.Center;
        searchBox.Append(_searchButton);
        mainBox.Append(searchBox);

        // Loading Spinner
        _spinner = Gtk.Spinner.New();
        _spinner.HeightRequest = 35;
        _spinner.Visible = false;
        mainBox.Append(_spinner);

        // Scrolled Window for Content
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;
        mainBox.Append(scrolledWindow);

        _contentBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        _contentBox.Visible = false;
        scrolledWindow.Child = _contentBox;

        // Header Section: Progress Bars
        var progressGroup = Adw.PreferencesGroup.New();
        progressGroup.Title = "Player Level Progress";
        _contentBox.Append(progressGroup);

        var hypixelRow = Adw.ActionRow.New();
        hypixelRow.Title = "Hypixel Network Level";
        _hypixelLevelLabel = Gtk.Label.New("Level 0");
        hypixelRow.AddSuffix(_hypixelLevelLabel);
        _hypixelLevelBar = Gtk.ProgressBar.New();
        _hypixelLevelBar.WidthRequest = 200;
        _hypixelLevelBar.Valign = Gtk.Align.Center;
        hypixelRow.AddSuffix(_hypixelLevelBar);
        progressGroup.Add(hypixelRow);

        var bwRow = Adw.ActionRow.New();
        bwRow.Title = "Bedwars Level";
        _bedwarsLevelLabel = Gtk.Label.New("0 Stars");
        bwRow.AddSuffix(_bedwarsLevelLabel);
        _bedwarsLevelBar = Gtk.ProgressBar.New();
        _bedwarsLevelBar.WidthRequest = 200;
        _bedwarsLevelBar.Valign = Gtk.Align.Center;
        bwRow.AddSuffix(_bedwarsLevelBar);
        progressGroup.Add(bwRow);

        // GRID LAYOUT FOR STATS CARDS (4 Columns x 5 Rows)
        var grid = Gtk.Grid.New();
        grid.SetColumnSpacing(12);
        grid.SetRowSpacing(12);
        grid.Hexpand = true;
        grid.Vexpand = true;
        _contentBox.Append(grid);

        // Row 0: Avatar Bust (Col 0, Row 0, Span 1x4), Stars (Col 1-2, Row 0), Tag (Col 3, Row 0)
        _playerAvatarImage = Gtk.Image.New();
        _playerAvatarImage.SetPixelSize(120);
        var avatarFrame = Gtk.Frame.New(null);
        avatarFrame.Child = _playerAvatarImage;
        grid.Attach(avatarFrame, 0, 0, 1, 3);

        grid.Attach(CreateCard("Bedwars Level", out _starValLabel), 1, 0, 2, 1);
        grid.Attach(CreateCard("Player Tag", out _tagValLabel), 3, 0, 1, 1);

        // Row 1: FKDR, Finals, Final Deaths
        grid.Attach(CreateCard("FKDR", out _fkdrValLabel), 1, 1, 1, 1);
        grid.Attach(CreateCard("Final Kills", out _finalsValLabel), 2, 1, 1, 1);
        grid.Attach(CreateCard("Final Deaths", out _finalDeathsValLabel), 3, 1, 1, 1);

        // Row 2: WLR, Wins, Losses
        grid.Attach(CreateCard("WLR", out _wlrValLabel), 1, 2, 1, 1);
        grid.Attach(CreateCard("Wins", out _winsValLabel), 2, 2, 1, 1);
        grid.Attach(CreateCard("Losses", out _lossesValLabel), 3, 2, 1, 1);

        // Row 3: Username, KDR, Kills, Deaths
        grid.Attach(CreateCard("Username", out _usernameValLabel), 0, 3, 1, 1);
        grid.Attach(CreateCard("KDR", out _kdrValLabel), 1, 3, 1, 1);
        grid.Attach(CreateCard("Kills", out _killsValLabel), 2, 3, 1, 1);
        grid.Attach(CreateCard("Deaths", out _deathsValLabel), 3, 3, 1, 1);

        // Row 4: First Login, BBLR, Beds Broken, Beds Lost
        grid.Attach(CreateCard("First Login", out _firstLoginValLabel), 0, 4, 1, 1);
        grid.Attach(CreateCard("BBLR", out _bblrValLabel), 1, 4, 1, 1);
        grid.Attach(CreateCard("Beds Broken", out _bedsValLabel), 2, 4, 1, 1);
        grid.Attach(CreateCard("Beds Lost", out _bedsLostValLabel), 3, 4, 1, 1);

        // Event Trigger
        _searchButton.OnClicked += (s, e) => PerformSearch();
        _searchEntry.OnActivate += (s, e) => PerformSearch();

        return mainBox;
    }

    private static Gtk.Widget CreateCard(string title, out Gtk.Label valueLabel)
    {
        var box = Gtk.Box.New(Gtk.Orientation.Vertical, 6);
        box.SetMarginStart(10);
        box.SetMarginEnd(10);
        box.SetMarginTop(10);
        box.SetMarginBottom(10);

        var titleLabel = Gtk.Label.New(title);
        titleLabel.Halign = Gtk.Align.Start;
        titleLabel.UseMarkup = true;
        titleLabel.Label_ = $"<small><b>{title}</b></small>";
        box.Append(titleLabel);

        valueLabel = Gtk.Label.New("-");
        valueLabel.Halign = Gtk.Align.Start;
        valueLabel.UseMarkup = true;
        valueLabel.Label_ = "<span size='large'>-</span>";
        box.Append(valueLabel);

        var frame = Gtk.Frame.New(null);
        frame.Child = box;
        frame.Hexpand = true;
        return frame;
    }

    public static void LoadPlayer(string username)
    {
        if (_searchEntry != null)
        {
            _searchEntry.Text_ = username;
        }
        PerformSearch();
    }

    private static async void PerformSearch()
    {
        if (_searchEntry == null || _spinner == null || _contentBox == null) return;
        var query = _searchEntry.Text_;
        if (string.IsNullOrWhiteSpace(query)) return;

        var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
        _hypixelApi.SetApiKey(apiKey ?? "");

        _searchEntry.Sensitive = false;
        if (_searchButton != null) _searchButton.Sensitive = false;
        _contentBox.Visible = false;
        _spinner.Visible = true;
        _spinner.Start();

        try
        {
            var player = await _hypixelApi.GetPlayerStats(query);
            if (player != null)
            {
                DisplayPlayerStats(player);
            }
            else
            {
                if (_usernameValLabel != null) _usernameValLabel.Label_ = "Player Not Found";
                _contentBox.Visible = true;
            }
        }
        catch (Exception ex)
        {
            DebugService.Instance.Log($"[StatsView] Error searching player stats: {ex.Message}");
        }
        finally
        {
            _searchEntry.Sensitive = true;
            if (_searchButton != null) _searchButton.Sensitive = true;
            _spinner.Stop();
            _spinner.Visible = false;
        }
    }

    private static void DisplayPlayerStats(Player player)
    {
        if (_contentBox == null) return;

        // Progress Bars
        if (_hypixelLevelLabel != null) _hypixelLevelLabel.Label_ = $"Level {player.HypixelLevel} ({player.HypixelLevelProgress:F1}%)";
        if (_hypixelLevelBar != null) _hypixelLevelBar.Fraction = Math.Clamp(player.HypixelLevelProgress / 100.0, 0.0, 1.0);

        if (_bedwarsLevelLabel != null) _bedwarsLevelLabel.Label_ = $"{player.Star} ★ ({player.BedwarsLevelProgress:F1}%)";
        if (_bedwarsLevelBar != null) _bedwarsLevelBar.Fraction = Math.Clamp(player.BedwarsLevelProgress / 100.0, 0.0, 1.0);

        // General Info
        if (_usernameValLabel != null) _usernameValLabel.Label_ = $"<b>{player.Username ?? "-"}</b>";
        if (_starValLabel != null) _starValLabel.Label_ = $"<span size='large' foreground='#FFAA00'><b>{player.Star} ★</b></span>";
        if (_tagValLabel != null) _tagValLabel.Label_ = string.IsNullOrEmpty(player.PlayerTag) ? "-" : $"<b>{player.PlayerTag}</b>";
        if (_firstLoginValLabel != null) _firstLoginValLabel.Label_ = string.IsNullOrEmpty(player.FirstLoginDate) ? "-" : player.FirstLoginDate;

        // Ratios
        if (_fkdrValLabel != null) _fkdrValLabel.Label_ = $"<span size='large' foreground='#FF5555'><b>{player.FKDR:F2}</b></span>";
        if (_wlrValLabel != null) _wlrValLabel.Label_ = $"<span size='large' foreground='#55FF55'><b>{player.WLR:F2}</b></span>";
        if (_kdrValLabel != null) _kdrValLabel.Label_ = $"<span size='large'><b>{player.KDR:F2}</b></span>";
        if (_bblrValLabel != null) _bblrValLabel.Label_ = $"<span size='large'><b>{player.BBLR:F2}</b></span>";

        // Counts
        if (_finalsValLabel != null) _finalsValLabel.Label_ = player.Finals.ToString("N0");
        if (_finalDeathsValLabel != null) _finalDeathsValLabel.Label_ = player.FinalDeaths.ToString("N0");
        if (_winsValLabel != null) _winsValLabel.Label_ = player.Wins.ToString("N0");
        if (_lossesValLabel != null) _lossesValLabel.Label_ = player.Losses.ToString("N0");
        if (_killsValLabel != null) _killsValLabel.Label_ = player.Kills.ToString("N0");
        if (_deathsValLabel != null) _deathsValLabel.Label_ = player.Deaths.ToString("N0");
        if (_bedsValLabel != null) _bedsValLabel.Label_ = player.Beds.ToString("N0");
        if (_bedsLostValLabel != null) _bedsLostValLabel.Label_ = player.BedsLost.ToString("N0");

        // Load Avatar Bust Image
        if (_playerAvatarImage != null && !string.IsNullOrEmpty(player.Username))
        {
            var username = player.Username;
            Task.Run(async () =>
            {
                try
                {
                    var avatarFile = Path.Combine(_avatarCacheDir, $"{username}_bust.png");
                    if (!File.Exists(avatarFile))
                    {
                        var bytes = await _httpClient.GetByteArrayAsync($"https://skins.jbw.fexei.at/bust/{username}");
                        await File.WriteAllBytesAsync(avatarFile, bytes);
                    }
                    RunOnMainThread(() =>
                    {
                        if (File.Exists(avatarFile) && _playerAvatarImage != null)
                        {
                            _playerAvatarImage.SetFromFile(avatarFile);
                        }
                    });
                }
                catch
                {
                    // Ignore image error
                }
            });
        }

        _contentBox.Visible = true;
    }
}