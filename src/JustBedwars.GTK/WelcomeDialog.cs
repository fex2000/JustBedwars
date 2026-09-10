using System;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class WelcomeDialog
{
    public static void ShowIfFirstLaunch(Gtk.Window parentWindow, SettingsService settingsService)
    {
        var isFirstLaunch = settingsService.GetValue("FirstLaunch") as bool? ?? true;
        if (!isFirstLaunch) return;

        var welcomeDialog = Gtk.Dialog.New();
        welcomeDialog.Title = "Welcome to JustBedwars!";
        welcomeDialog.TransientFor = parentWindow;
        welcomeDialog.Modal = true;
        welcomeDialog.SetDefaultSize(550, 420);
        welcomeDialog.Resizable = false;

        var contentArea = (Gtk.Box)welcomeDialog.GetContentArea();
        contentArea.MarginStart = 20;
        contentArea.MarginEnd = 20;
        contentArea.MarginTop = 20;
        contentArea.MarginBottom = 20;
        contentArea.Spacing = 15;

        // Title and Intro
        var titleLabel = Gtk.Label.New("<b>Welcome to JustBedwars!</b>");
        titleLabel.UseMarkup = true;
        titleLabel.Halign = Gtk.Align.Start;
        contentArea.Append(titleLabel);

        var subtitleLabel = Gtk.Label.New("Let's configure a few settings before starting. You can always change these later in Settings.");
        subtitleLabel.Wrap = true;
        subtitleLabel.Halign = Gtk.Align.Start;
        contentArea.Append(subtitleLabel);

        // Preferences Group
        var prefGroup = Adw.PreferencesGroup.New();
        prefGroup.Title = "Initial Configuration";
        contentArea.Append(prefGroup);

        // Log File Row
        var logRow = Adw.ActionRow.New();
        logRow.Title = "Log File Path";
        logRow.Subtitle = "Location of latest.log";
        prefGroup.Add(logRow);

        var logBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 5);
        logBox.Valign = Gtk.Align.Center;
        var logEntry = Gtk.Entry.New();
        logEntry.WidthRequest = 220;
        logEntry.Valign = Gtk.Align.Center;
        var defaultLogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".minecraft", "logs", "latest.log"
        );
        var currentLog = settingsService.GetValue("LogFilePath") as string;
        logEntry.Text_ = string.IsNullOrEmpty(currentLog) ? defaultLogPath : currentLog;
        logBox.Append(logEntry);

        var browseBtn = Gtk.Button.NewFromIconName("folder-open-symbolic");
        browseBtn.TooltipText = "Choose Log File";
        browseBtn.Valign = Gtk.Align.Center;
        browseBtn.OnClicked += (sender, args) =>
        {
            var dialog = Gtk.FileChooserNative.New(
                "Select Minecraft Log File",
                welcomeDialog,
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
                        }
                    }
                }
                dialog.Destroy();
            };
            dialog.Show();
        };
        logBox.Append(browseBtn);
        logRow.AddSuffix(logBox);

        // Usage Stats Row
        var statsRow = Adw.ActionRow.New();
        statsRow.Title = "Send usage statistics?";
        statsRow.Subtitle = "Send anonymous usage data & crash reports";
        prefGroup.Add(statsRow);

        var statsSwitch = Gtk.Switch.New();
        var sendStats = settingsService.GetValue("SendUsageStats") as bool? ?? true;
        statsSwitch.Active = sendStats;
        statsSwitch.Valign = Gtk.Align.Center;
        statsRow.AddSuffix(statsSwitch);

        // Action Box (Finish Button)
        var actionBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 10);
        actionBox.Halign = Gtk.Align.End;
        actionBox.MarginTop = 10;

        var finishBtn = Gtk.Button.NewWithLabel("Finish");
        finishBtn.OnClicked += (sender, args) =>
        {
            settingsService.SetValue("FirstLaunch", false);
            settingsService.SetValue("LogFilePath", logEntry.Text_);
            settingsService.SetValue("SendUsageStats", statsSwitch.Active);
            welcomeDialog.Destroy();
        };
        actionBox.Append(finishBtn);
        contentArea.Append(actionBox);

        welcomeDialog.Present();
    }
}
