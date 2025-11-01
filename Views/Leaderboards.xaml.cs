using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using JustBedwars.Services;
using JustBedwars.Models;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace JustBedwars.Views
{
    public sealed partial class LeaderboardsPage : Page
    {
        private string _selectedLeaderboard = "Stars";
        private string _selectedTimeFilter = "Weekly";
        private Dictionary<string, List<LeaderboardEntry>> _fullLeaderboardCache = new Dictionary<string, List<LeaderboardEntry>>();
        private Dictionary<string, ObservableCollection<LeaderboardEntry>> _leaderboardDataCache = new Dictionary<string, ObservableCollection<LeaderboardEntry>>();

        public LeaderboardsPage()
        {
            this.InitializeComponent();
            this.Loaded += LeaderboardsPage_Loaded;
        }

        private void LeaderboardsPage_Loaded(object sender, RoutedEventArgs e)
        {
            SwitchLeaderboard();
        }

        private void LeaderboardSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            if (selectedItem != null)
            {
                var newSelection = selectedItem.Text;
                if (newSelection != _selectedLeaderboard)
                {
                    _selectedLeaderboard = newSelection;
                    if (_selectedLeaderboard == "Wins" || _selectedLeaderboard == "Finals")
                    {
                        TimeFilterSelector.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        TimeFilterSelector.Visibility = Visibility.Collapsed;
                    }
                    SwitchLeaderboard();
                }
            }
        }

        private void TimeFilterSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            if (selectedItem != null)
            {
                var newSelection = selectedItem.Text;
                if (newSelection != _selectedTimeFilter)
                {
                    _selectedTimeFilter = newSelection;
                    SwitchLeaderboard();
                }
            }
        }

        private void SwitchLeaderboard()
        {
            string cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";
            if (_leaderboardDataCache.ContainsKey(cacheKey))
            {
                LeaderboardList.ItemsSource = _leaderboardDataCache[cacheKey];
            }
            else
            {
                LoadLeaderboard();
            }
        }

        private async void LoadLeaderboard()
        {
            if (LeaderboardList == null) return;

            string cacheKey = $"{_selectedLeaderboard}_{_selectedTimeFilter}";

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

            foreach (var item in fullLeaderboard)
            {
                collection.Add(item);
            }

            LoadingIndicator.IsActive = false;
        }

        private void LeaderboardList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedEntry = e.ClickedItem as LeaderboardEntry;
            if (clickedEntry != null && this.Frame != null)
            {
                this.Frame.Navigate(typeof(JustBedwars.Views.StatsPage), clickedEntry.Name);
            }
        }
    }
}