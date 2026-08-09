using System;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class SettingsView
{
    private static readonly SettingsService _settingsService = new();

    public static Gtk.Widget Create()
    {
        var prefPage = Adw.PreferencesPage.New();
        prefPage.Title = "Settings";

        // Group 1: API Configuration
        var apiGroup = Adw.PreferencesGroup.New();
        apiGroup.Title = "API Configuration";
        prefPage.Add(apiGroup);

        var apiKeyRow = Adw.ActionRow.New();
        apiKeyRow.Title = "Hypixel API Key";
        apiKeyRow.Subtitle = "Enter your personal Hypixel API Key";
        apiGroup.Add(apiKeyRow);

        var apiKeyEntry = Gtk.Entry.New();
        apiKeyEntry.Visibility = false; // Masked for password
        apiKeyEntry.WidthRequest = 250;
        var savedKey = _settingsService.GetValue("HypixelApiKey") as string ?? "";
        apiKeyEntry.Text_ = savedKey;

        // Handle text change
        apiKeyEntry.OnChanged += (sender, args) =>
        {
            _settingsService.SetValue("HypixelApiKey", apiKeyEntry.Text_);
        };
        apiKeyRow.AddSuffix(apiKeyEntry);

        // Group 2: Minecraft Integration
        var gameGroup = Adw.PreferencesGroup.New();
        gameGroup.Title = "Minecraft Integration";
        prefPage.Add(gameGroup);

        // Log File Row
        var logRow = Adw.ActionRow.New();
        logRow.Title = "Log File Path";
        logRow.Subtitle = "Location of latest.log (usually ~/.minecraft/logs/latest.log)";
        gameGroup.Add(logRow);

        var logBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        var logEntry = Gtk.Entry.New();
        logEntry.WidthRequest = 350;
        var savedLog = _settingsService.GetValue("LogFilePath") as string ?? "";
        if (string.IsNullOrEmpty(savedLog))
        {
            savedLog = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft", "logs", "latest.log"
            );
        }
        logEntry.Text_ = savedLog;
        logEntry.OnChanged += (sender, args) =>
        {
            _settingsService.SetValue("LogFilePath", logEntry.Text_);
        };
        logBox.Append(logEntry);

        // Simple button to select standard/default location
        var defaultBtn = Gtk.Button.NewWithLabel("Reset Default");
        defaultBtn.OnClicked += (sender, args) =>
        {
            var defaultPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft", "logs", "latest.log"
            );
            logEntry.Text_ = defaultPath;
            _settingsService.SetValue("LogFilePath", defaultPath);
        };
        logBox.Append(defaultBtn);
        logRow.AddSuffix(logBox);

        // Group 3: Sorting Configuration
        var sortGroup = Adw.PreferencesGroup.New();
        sortGroup.Title = "Sorting Configuration";
        prefPage.Add(sortGroup);

        var sortRow = Adw.ActionRow.New();
        sortRow.Title = "Player Sorting Mode";
        sortRow.Subtitle = "Determine how player list is sorted";
        sortGroup.Add(sortRow);

        var sortCombo = Gtk.ComboBoxText.New();
        sortCombo.AppendText("JustBedwars Score");
        sortCombo.AppendText("Skill Index");
        sortCombo.AppendText("FKDR");
        sortCombo.AppendText("WLR");
        sortCombo.AppendText("Stars");

        var savedSorting = _settingsService.GetValue("PlayerSorting") as string ?? "JustBedwars Score";
        switch (savedSorting)
        {
            case "Skill Index": sortCombo.SetActive(1); break;
            case "FKDR": sortCombo.SetActive(2); break;
            case "WLR": sortCombo.SetActive(3); break;
            case "Stars": sortCombo.SetActive(4); break;
            default: sortCombo.SetActive(0); break;
        }

        sortCombo.OnChanged += (sender, args) =>
        {
            var activeText = sortCombo.GetActiveText();
            if (!string.IsNullOrEmpty(activeText))
            {
                _settingsService.SetValue("PlayerSorting", activeText);
            }
        };
        sortRow.AddSuffix(sortCombo);

        return prefPage;
    }
}