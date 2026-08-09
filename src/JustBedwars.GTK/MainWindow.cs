using System;

namespace JustBedwars.GTK;

public static class MainWindow
{
    public static Adw.ApplicationWindow New(Adw.Application application)
    {
        var window = Adw.ApplicationWindow.New(application);
        window.Title = "JustBedwars (Linux)";
        window.SetDefaultSize(800, 600);

        // Layout vertical box
        var mainBox = Gtk.Box.New(Gtk.Orientation.Vertical, 0);

        // Header bar
        var headerBar = Adw.HeaderBar.New();
        mainBox.Append(headerBar);

        // Sidebar and Content Box (Horizontal)
        var contentBox = Gtk.Box.New(Gtk.Orientation.Horizontal, 0);
        mainBox.Append(contentBox);

        // Gtk Stack
        var stack = Gtk.Stack.New();
        stack.SetTransitionType(Gtk.StackTransitionType.SlideLeftRight);
        stack.SetTransitionDuration(250);

        // Sidebar
        var sidebar = Gtk.StackSidebar.New();
        sidebar.Stack = stack;
        sidebar.WidthRequest = 200;

        // Add separator between sidebar and content
        var separator = Gtk.Separator.New(Gtk.Orientation.Vertical);

        contentBox.Append(sidebar);
        contentBox.Append(separator);
        contentBox.Append(stack);

        // Expand the stack to fill the remaining horizontal and vertical space
        stack.Hexpand = true;
        stack.Vexpand = true;
        contentBox.Vexpand = true;

        // Implement views
        var playerListView = PlayerListView.Create();
        var statsView = StatsView.Create();
        var settingsView = SettingsView.Create();

        // Add views to stack with title and name
        stack.AddTitled(playerListView, "player_list", "Player List");
        stack.AddTitled(statsView, "stats_page", "Stats Page");
        stack.AddTitled(settingsView, "settings", "Settings");

        window.Child = mainBox;
        return window;
    }
}