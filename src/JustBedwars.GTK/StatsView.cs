using System;
using JustBedwars.Services;
using JustBedwars.Models;

namespace JustBedwars.GTK;

public static class StatsView
{
    private static readonly HypixelApi _hypixelApi = new();
    private static readonly SettingsService _settingsService = new();

    public static Gtk.Widget Create()
    {
        // Set API key if saved
        var apiKey = _settingsService.GetValue("HypixelApiKey") as string;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _hypixelApi.SetApiKey(apiKey);
        }

        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        mainBox.SetMarginStart(15);
        mainBox.SetMarginEnd(15);
        mainBox.SetMarginTop(15);
        mainBox.SetMarginBottom(15);

        // Search Section
        var searchBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        var searchEntry = Gtk.Entry.New();
        searchEntry.PlaceholderText = "Search Player Username...";
        searchEntry.Hexpand = true;
        searchBox.Append(searchEntry);

        var searchButton = Gtk.Button.NewWithLabel("Search");
        searchBox.Append(searchButton);
        mainBox.Append(searchBox);

        // Loader Widget
        var loaderLabel = Gtk.Label.New("Loading player statistics...");
        loaderLabel.Visible = false;
        mainBox.Append(loaderLabel);

        // Scrolled content for stats
        var scrolledWindow = Gtk.ScrolledWindow.New();
        scrolledWindow.Hexpand = true;
        scrolledWindow.Vexpand = true;
        mainBox.Append(scrolledWindow);

        var statsContainer = Gtk.Box.New(Gtk.Orientation.Vertical, 15);
        scrolledWindow.Child = statsContainer;

        // Group 1: General Info
        var generalGroup = Adw.PreferencesGroup.New();
        generalGroup.Title = "General Information";
        statsContainer.Append(generalGroup);

        var usernameRow = Adw.ActionRow.New();
        usernameRow.Title = "Username";
        var usernameLabel = Gtk.Label.New("-");
        usernameRow.AddSuffix(usernameLabel);
        generalGroup.Add(usernameRow);

        var starRow = Adw.ActionRow.New();
        starRow.Title = "Bedwars Level (Stars)";
        var starLabel = Gtk.Label.New("-");
        starRow.AddSuffix(starLabel);
        generalGroup.Add(starRow);

        var tagRow = Adw.ActionRow.New();
        tagRow.Title = "Player Tag";
        var tagLabel = Gtk.Label.New("-");
        tagRow.AddSuffix(tagLabel);
        generalGroup.Add(tagRow);

        var firstLoginRow = Adw.ActionRow.New();
        firstLoginRow.Title = "First Login";
        var firstLoginLabel = Gtk.Label.New("-");
        firstLoginRow.AddSuffix(firstLoginLabel);
        generalGroup.Add(firstLoginRow);

        // Group 2: Combat Stats
        var combatGroup = Adw.PreferencesGroup.New();
        combatGroup.Title = "Combat &amp; Performance Stats";
        statsContainer.Append(combatGroup);

        var fkdrRow = Adw.ActionRow.New();
        fkdrRow.Title = "Final Kills / Final Deaths / FKDR";
        var fkdrLabel = Gtk.Label.New("-");
        fkdrRow.AddSuffix(fkdrLabel);
        combatGroup.Add(fkdrRow);

        var kdrRow = Adw.ActionRow.New();
        kdrRow.Title = "Kills / Deaths / KDR";
        var kdrLabel = Gtk.Label.New("-");
        kdrRow.AddSuffix(kdrLabel);
        combatGroup.Add(kdrRow);

        var wlrRow = Adw.ActionRow.New();
        wlrRow.Title = "Wins / Losses / WLR";
        var wlrLabel = Gtk.Label.New("-");
        wlrRow.AddSuffix(wlrLabel);
        combatGroup.Add(wlrRow);

        // Group 3: Objective Stats
        var objGroup = Adw.PreferencesGroup.New();
        objGroup.Title = "Objective Stats";
        statsContainer.Append(objGroup);

        var bblrRow = Adw.ActionRow.New();
        bblrRow.Title = "Beds Broken / Beds Lost / BBLR";
        var bblrLabel = Gtk.Label.New("-");
        bblrRow.AddSuffix(bblrLabel);
        objGroup.Add(bblrRow);

        // Search Action Helper
        async void PerformSearch()
        {
            var query = searchEntry.Text_;
            if (string.IsNullOrWhiteSpace(query)) return;

            // Update API Key in case it was changed in Settings
            var currentKey = _settingsService.GetValue("HypixelApiKey") as string;
            _hypixelApi.SetApiKey(currentKey ?? "");

            searchEntry.Sensitive = false;
            searchButton.Sensitive = false;
            statsContainer.Visible = false;
            loaderLabel.Visible = true;

            try
            {
                var player = await _hypixelApi.GetPlayerStats(query);
                if (player != null)
                {
                    usernameLabel.Label_ = player.Username ?? "-";
                    starLabel.Label_ = player.Star.ToString();
                    tagLabel.Label_ = string.IsNullOrEmpty(player.PlayerTag) ? "-" : player.PlayerTag;
                    firstLoginLabel.Label_ = string.IsNullOrEmpty(player.FirstLoginDate) ? "-" : player.FirstLoginDate;

                    fkdrLabel.Label_ = $"{player.Finals} / {player.FinalDeaths} ({player.FKDR:F2})";
                    kdrLabel.Label_ = $"{player.Kills} / {player.Deaths} ({player.KDR:F2})";
                    wlrLabel.Label_ = $"{player.Wins} / {player.Losses} ({player.WLR:F2})";

                    bblrLabel.Label_ = $"{player.Beds} / {player.BedsLost} ({player.BBLR:F2})";

                    statsContainer.Visible = true;
                }
                else
                {
                    usernameLabel.Label_ = "Player not found";
                    starLabel.Label_ = "-";
                    tagLabel.Label_ = "-";
                    firstLoginLabel.Label_ = "-";
                    fkdrLabel.Label_ = "-";
                    kdrLabel.Label_ = "-";
                    wlrLabel.Label_ = "-";
                    bblrLabel.Label_ = "-";
                    statsContainer.Visible = true;
                }
            }
            catch (Exception ex)
            {
                usernameLabel.Label_ = $"Error: {ex.Message}";
                statsContainer.Visible = true;
            }
            finally
            {
                searchEntry.Sensitive = true;
                searchButton.Sensitive = true;
                loaderLabel.Visible = false;
            }
        }

        // Trigger search on button click or Enter key
        searchButton.OnClicked += (sender, args) => PerformSearch();
        searchEntry.OnActivate += (sender, args) => PerformSearch();

        return mainBox;
    }
}