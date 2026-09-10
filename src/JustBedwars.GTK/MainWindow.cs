using System;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class MainWindow
{
    private static Adw.NavigationSplitView? _splitView;
    private static Gtk.Stack? _contentStack;
    private static Gtk.ListBox? _sidebarListBox;
    private static Adw.HeaderBar? _contentHeaderBar;
    private static Gtk.Label? _titleLabel;
    private static readonly SettingsService _settingsService = new();

    public static Adw.ApplicationWindow New(Adw.Application application)
    {
        var window = Adw.ApplicationWindow.New(application);
        window.Title = "JustBedwars";
        window.SetDefaultSize(950, 650);
        window.WidthRequest = 820;
        window.HeightRequest = 400;

        _splitView = Adw.NavigationSplitView.New();

        // Create Content Stack / Stack Pages
        _contentStack = Gtk.Stack.New();
        _contentStack.SetTransitionType(Gtk.StackTransitionType.Crossfade);
        _contentStack.SetTransitionDuration(150);

        var playerListView = PlayerListView.Create();
        var statsView = StatsView.Create();
        var leaderboardsView = LeaderboardsView.Create();
        var guildLookupView = GuildLookupView.Create();

        _contentStack.AddTitled(playerListView, "player_list", "Current Game");
        _contentStack.AddTitled(statsView, "stats_page", "Player Lookup");
        _contentStack.AddTitled(leaderboardsView, "leaderboards", "Leaderboards");
        _contentStack.AddTitled(guildLookupView, "guilds", "Guilds");

        // Create Sidebar / Sidebar List
        var sidebarPage = Adw.NavigationPage.New(CreateSidebarWidget(), "Navigation");
        sidebarPage.Title = "JustBedwars";

        // Content Outer ToolbarView
        var contentToolbar = Adw.ToolbarView.New();
        _contentHeaderBar = Adw.HeaderBar.New();

        _titleLabel = Gtk.Label.New("Current Game");
        _titleLabel.AddCssClass("title");
        _contentHeaderBar.TitleWidget = _titleLabel;

        // Settings Button in HeaderBar
        var settingsBtn = Gtk.Button.NewFromIconName("emblem-system-symbolic");
        settingsBtn.TooltipText = "Settings";
        settingsBtn.OnClicked += (sender, args) =>
        {
            SettingsWindow.Show(window);
        };
        _contentHeaderBar.PackEnd(settingsBtn);

        contentToolbar.AddTopBar(_contentHeaderBar);
        contentToolbar.SetContent(_contentStack);

        var contentPage = Adw.NavigationPage.New(contentToolbar, "ContentPage");

        _splitView.Sidebar = sidebarPage;
        _splitView.Content = contentPage;

        window.SetContent(_splitView);

        // Show welcome dialog on first launch after window presents
        GLib.Functions.IdleAdd(0, () =>
        {
            WelcomeDialog.ShowIfFirstLaunch(window, _settingsService);
            return false;
        });

        return window;
    }

    private static Gtk.Widget CreateSidebarWidget()
    {
        var toolbarView = Adw.ToolbarView.New();
        var headerBar = Adw.HeaderBar.New();
        toolbarView.AddTopBar(headerBar);

        _sidebarListBox = Gtk.ListBox.New();
        _sidebarListBox.AddCssClass("navigation-sidebar");
        _sidebarListBox.SetMarginStart(6);
        _sidebarListBox.SetMarginEnd(6);
        _sidebarListBox.SetMarginTop(6);
        _sidebarListBox.SetMarginBottom(6);

        var items = new (string Title, string IconName, string Tag)[]
        {
            ("Current Game", "user-bookmarks-symbolic", "player_list"),
            ("Player Lookup", "system-search-symbolic", "stats_page"),
            ("Leaderboards", "emblem-favorite-symbolic", "leaderboards"),
            ("Guilds", "system-users-symbolic", "guilds")
        };

        foreach (var item in items)
        {
            var row = Adw.ActionRow.New();
            row.Title = item.Title;
            row.Name = item.Tag;
            
            var icon = Gtk.Image.NewFromIconName(item.IconName);
            row.AddPrefix(icon);

            _sidebarListBox.Append(row);
        }

        _sidebarListBox.OnRowSelected += (sender, args) =>
        {
            if (args.Row is Adw.ActionRow actionRow && !string.IsNullOrEmpty(actionRow.Name) && _contentStack != null)
            {
                _contentStack.VisibleChildName = actionRow.Name;
                if (_titleLabel != null)
                {
                    _titleLabel.Label_ = actionRow.Title;
                }
            }
        };

        _sidebarListBox.SelectRow(_sidebarListBox.GetRowAtIndex(0));

        toolbarView.SetContent(_sidebarListBox);
        return toolbarView;
    }

    public static void NavigateToStatsPage(string username)
    {
        if (_sidebarListBox != null && _contentStack != null)
        {
            _sidebarListBox.SelectRow(_sidebarListBox.GetRowAtIndex(1));
            _contentStack.VisibleChildName = "stats_page";
            if (_titleLabel != null)
            {
                _titleLabel.Label_ = "Player Lookup";
            }
            StatsView.LoadPlayer(username);
        }
    }
}