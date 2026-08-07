using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JustBedwars.Views;

public sealed partial class LeaderboardsPage : Page
{
    private readonly AptabaseClient _aptabaseClient;
    private readonly Dictionary<string, List<LeaderboardEntry>> _fullLeaderboardCache = new();
    private readonly Dictionary<string, ObservableCollection<LeaderboardEntry>> _leaderboardDataCache = new();
    private string _selectedLeaderboard = "Stars";
    private string _selectedTimeFilter = "Weekly";

    public LeaderboardsPage()
    {
        InitializeComponent();
        Loaded += LeaderboardsPage_Loaded;

        var instance = (App)Application.Current;
        _aptabaseClient = instance.AptabaseClient;
    }

    private void LeaderboardsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SwitchLeaderboard();
    }

    private void LeaderboardSelector_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        var newSelection = "";

        if (LeaderboardSelector.SelectedIndex == 0)
            newSelection = "Stars";
        else if (LeaderboardSelector.SelectedIndex == 1)
            newSelection = "Wins";
        else
            newSelection = "Finals";


        if (newSelection != _selectedLeaderboard)
        {
            _selectedLeaderboard = newSelection;
            if (_selectedLeaderboard == "Wins" || _selectedLeaderboard == "Finals")
                TimeFilterSelector.Visibility = Visibility.Visible;
            else
                TimeFilterSelector.Visibility = Visibility.Collapsed;
            SwitchLeaderboard();
        }
    }

    private void TimeFilterSelector_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        var newSelection = "";

        if (TimeFilterSelector.SelectedIndex == 0)
            newSelection = "Weekly";
        else
            newSelection = "Lifetime";
        if (LeaderboardSelector != null)
            if (LeaderboardSelector.SelectedIndex == 0)
                newSelection = "Weekly";


        if (newSelection != _selectedTimeFilter)
        {
            _selectedTimeFilter = newSelection;
            SwitchLeaderboard();
        }
    }

    private void SwitchLeaderboard()
    {
        var props = new
        {
            Board = _selectedLeaderboard,
            Filter = _selectedTimeFilter
        };
        _ = _aptabaseClient.TrackEvent("OpenLeaderbaord", props);

        var cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";
        if (_leaderboardDataCache.ContainsKey(cacheKey))
            LeaderboardList.ItemsSource = _leaderboardDataCache[cacheKey];
        else
            LoadLeaderboard();
    }

    private async void LoadLeaderboard()
    {
        if (LeaderboardList == null) return;

        var cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";

        LoadingIndicator.IsActive = true;
        LoadingIndicator.IsIndeterminate = true;
        LeaderboardList.ItemsSource = null;

        var api = new HypixelApi();
        var fullLeaderboard = await api.GetLeaderboard(_selectedLeaderboard, _selectedTimeFilter);
        _fullLeaderboardCache[cacheKey] = fullLeaderboard;

        var collection = new ObservableCollection<LeaderboardEntry>();
        LeaderboardList.ItemsSource = collection;
        _leaderboardDataCache[cacheKey] = collection;

        IProgress<double> progress = new Progress<double>(value =>
        {
            LoadingIndicator.IsIndeterminate = false;
            LoadingIndicator.Value = value;
        });

        await api.GetNamesForLeaderboardEntries(fullLeaderboard, progress);

        foreach (var item in fullLeaderboard) collection.Add(item);

        LoadingIndicator.IsActive = false;
    }

    private void LeaderboardList_ItemClick(object sender, ItemClickEventArgs e)
    {
        var clickedEntry = e.ClickedItem as LeaderboardEntry;
        if (clickedEntry != null && Frame != null) Frame.Navigate(typeof(StatsPage), clickedEntry.Name);
    }
}