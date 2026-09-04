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

        // 1. Player List Group (Sorting)
        var playerListGroup = Adw.PreferencesGroup.New();
        playerListGroup.Title = "Player List";
        prefPage.Add(playerListGroup);

        var sortRow = Adw.ActionRow.New();
        sortRow.Title = "Player Sorting";
        sortRow.Subtitle = "The order that players appear in";
        playerListGroup.Add(sortRow);

        var sortCombo = Gtk.ComboBoxText.New();
        sortCombo.Valign = Gtk.Align.Center;
        sortCombo.AppendText("JustBedwars Score");
        sortCombo.AppendText("Skill Index");
        sortCombo.AppendText("Stars");
        sortCombo.AppendText("FKDR");
        sortCombo.AppendText("WLR");

        var savedSorting = _settingsService.GetValue("PlayerSorting") as string ?? "JustBedwars Score";
        switch (savedSorting)
        {
            case "Skill Index": sortCombo.SetActive(1); break;
            case "Stars": sortCombo.SetActive(2); break;
            case "FKDR": sortCombo.SetActive(3); break;
            case "WLR": sortCombo.SetActive(4); break;
            default: sortCombo.SetActive(0); break;
        }

        sortCombo.OnChanged += (sender, args) =>
        {
            var activeText = sortCombo.GetActiveText();
            if (!string.IsNullOrEmpty(activeText))
            {
                _settingsService.SetValue("PlayerSorting", activeText.Trim());
            }
        };
        sortRow.AddSuffix(sortCombo);

        // Expander inside Player List group explaining formulas
        var scoreExpander = Gtk.Expander.New("Sorting Mode Descriptions & Formulas");
        scoreExpander.SetMarginStart(10);
        scoreExpander.SetMarginEnd(10);
        scoreExpander.SetMarginTop(5);
        scoreExpander.SetMarginBottom(10);

        var scoreBox = Gtk.Box.New(Gtk.Orientation.Vertical, 8);
        var jbwScoreTitle = Gtk.Label.New("<b>JustBedwars Score</b>");
        jbwScoreTitle.UseMarkup = true;
        jbwScoreTitle.Halign = Gtk.Align.Start;
        var jbwScoreDesc = Gtk.Label.New("A balanced way of telling a player's skill, using almost every stat.\nCalculation: Level ⋅ FKDR² ⋅ WLR¹˙² ⋅ BBLR¹˙¹ ⋅ (1 + Finals/1000 + Kills/2000 + Beds/500 + Wins/1000)");
        jbwScoreDesc.Halign = Gtk.Align.Start;
        jbwScoreDesc.Wrap = true;

        var skillIndexTitle = Gtk.Label.New("<b>Skill Index</b>");
        skillIndexTitle.UseMarkup = true;
        skillIndexTitle.Halign = Gtk.Align.Start;
        var skillIndexDesc = Gtk.Label.New("The simpler way of scoring players used in most other Bedwars skill trackers.\nCalculation: Level ⋅ FKDR²");
        skillIndexDesc.Halign = Gtk.Align.Start;
        skillIndexDesc.Wrap = true;

        scoreBox.Append(jbwScoreTitle);
        scoreBox.Append(jbwScoreDesc);
        scoreBox.Append(skillIndexTitle);
        scoreBox.Append(skillIndexDesc);
        scoreExpander.Child = scoreBox;
        playerListGroup.Add(scoreExpander);

        // 2. Log Location Group
        var logGroup = Adw.PreferencesGroup.New();
        logGroup.Title = "Log Location";
        prefPage.Add(logGroup);

        var logRow = Adw.ActionRow.New();
        logRow.Title = "Log File Path";
        logRow.Subtitle = "Set to your .minecraft/logs/latest.log location to use";
        logGroup.Add(logRow);

        var logBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        logBox.Valign = Gtk.Align.Center;
        var logEntry = Gtk.Entry.New();
        logEntry.WidthRequest = 280;
        logEntry.Valign = Gtk.Align.Center;
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

        var browseBtn = Gtk.Button.NewFromIconName("folder-open-symbolic");
        browseBtn.TooltipText = "Choose Log File";
        browseBtn.Valign = Gtk.Align.Center;
        browseBtn.OnClicked += (sender, args) =>
        {
            var dialog = Gtk.FileChooserNative.New(
                "Select Minecraft Log File",
                null,
                Gtk.FileChooserAction.Open,
                "Open",
                "Cancel"
            );

            var filter = Gtk.FileFilter.New();
            filter.Name = "Log Files (*.log)";
            filter.AddPattern("*.log");
            dialog.AddFilter(filter);

            try
            {
                if (System.IO.File.Exists(logEntry.Text_))
                {
                    var file = Gio.FileHelper.NewForPath(logEntry.Text_);
                    dialog.SetFile(file);
                }
            }
            catch { }

            dialog.OnResponse += (dlg, respArgs) =>
            {
                if (respArgs.ResponseId == (int)Gtk.ResponseType.Accept)
                {
                    var file = dialog.GetFile();
                    if (file != null)
                    {
                        var path = file.GetPath();
                        if (!string.IsNullOrEmpty(path))
                        {
                            logEntry.Text_ = path;
                            _settingsService.SetValue("LogFilePath", path);
                        }
                    }
                }
                dialog.Destroy();
            };
            dialog.Show();
        };
        logBox.Append(browseBtn);
        logRow.AddSuffix(logBox);

        // 3. Usage Statistics Group (with Expander details)
        var usageGroup = Adw.PreferencesGroup.New();
        usageGroup.Title = "Usage Statistics";
        prefPage.Add(usageGroup);

        var statsRow = Adw.ActionRow.New();
        statsRow.Title = "Send Usage Statistics";
        statsRow.Subtitle = "Send usage info for diagnostics & crash reports";
        usageGroup.Add(statsRow);

        var statsSwitch = Gtk.Switch.New();
        statsSwitch.Valign = Gtk.Align.Center;
        statsSwitch.Active = _settingsService.GetValue("SendUsageStats") as bool? ?? true;
        statsSwitch.OnNotify += (sender, args) =>
        {
            _settingsService.SetValue("SendUsageStats", statsSwitch.Active);
        };
        statsRow.AddSuffix(statsSwitch);

        var statsExpander = Gtk.Expander.New("Details on collected telemetry & events");
        statsExpander.SetMarginStart(10);
        statsExpander.SetMarginEnd(10);
        statsExpander.SetMarginTop(5);
        statsExpander.SetMarginBottom(10);

        var statsDetailsBox = Gtk.Box.New(Gtk.Orientation.Vertical, 10);

        var dataCollectedLabel = Gtk.Label.New("<b>What data is collected?</b>\n• OS Version & App Version / Build Type\n• Timestamp & Session Duration\n• Source Country/City (IP address deleted immediately after lookup)\n• Sent Events (App launch/close, searches, errors)");
        dataCollectedLabel.UseMarkup = true;
        dataCollectedLabel.Halign = Gtk.Align.Start;
        dataCollectedLabel.Wrap = true;

        var eventsLabel = Gtk.Label.New("<b>Which events trigger a message?</b>\n• App launch & close\n• Unhandled exceptions (exception type & basic crash info)\n• Setup completed / Player lookup / Guild search\n• Leaderboard tab switches & updates");
        eventsLabel.UseMarkup = true;
        eventsLabel.Halign = Gtk.Align.Start;
        eventsLabel.Wrap = true;

        var howUsedLabel = Gtk.Label.New("<b>How is this data used?</b>\n• To see which areas need improvement & track app usage\n• To identify supported OS versions and active languages");
        howUsedLabel.UseMarkup = true;
        howUsedLabel.Halign = Gtk.Align.Start;
        howUsedLabel.Wrap = true;

        var goodToKnowLabel = Gtk.Label.New("<b>Good to know</b>\n• There is no way to track you across sessions (nothing is permanent)\n• Each session gets a temporary session ID");
        goodToKnowLabel.UseMarkup = true;
        goodToKnowLabel.Halign = Gtk.Align.Start;
        goodToKnowLabel.Wrap = true;

        statsDetailsBox.Append(dataCollectedLabel);
        statsDetailsBox.Append(eventsLabel);
        statsDetailsBox.Append(howUsedLabel);
        statsDetailsBox.Append(goodToKnowLabel);
        statsExpander.Child = statsDetailsBox;
        usageGroup.Add(statsExpander);

        // 4. Developer Settings Group
        var devGroup = Adw.PreferencesGroup.New();
        devGroup.Title = "Developer Settings";
        prefPage.Add(devGroup);

        var apiKeyRow = Adw.ActionRow.New();
        apiKeyRow.Title = "Hypixel API Key";
        apiKeyRow.Subtitle = "For development, not recommended for regular users";
        devGroup.Add(apiKeyRow);

        var apiKeyEntry = Gtk.Entry.New();
        apiKeyEntry.Visibility = false; // Masked
        apiKeyEntry.WidthRequest = 250;
        apiKeyEntry.Valign = Gtk.Align.Center;
        var savedKey = _settingsService.GetValue("HypixelApiKey") as string ?? "";
        apiKeyEntry.Text_ = savedKey;

        apiKeyEntry.OnChanged += (sender, args) =>
        {
            _settingsService.SetValue("HypixelApiKey", apiKeyEntry.Text_);
        };
        apiKeyRow.AddSuffix(apiKeyEntry);

        // 5. Save Debug Logs Group
        var debugGroup = Adw.PreferencesGroup.New();
        debugGroup.Title = "Save Logs";
        prefPage.Add(debugGroup);

        var debugLogsRow = Adw.ActionRow.New();
        debugLogsRow.Title = "Save Debug Logs";
        debugLogsRow.Subtitle = "Save the latest application log to disk";
        debugGroup.Add(debugLogsRow);

        var debugSwitch = Gtk.Switch.New();
        debugSwitch.Valign = Gtk.Align.Center;
        debugSwitch.Active = _settingsService.GetValue("SaveDebugLogs") as bool? ?? true;
        debugSwitch.OnNotify += (sender, args) =>
        {
            _settingsService.SetValue("SaveDebugLogs", debugSwitch.Active);
        };
        debugLogsRow.AddSuffix(debugSwitch);

        return prefPage;
    }
}