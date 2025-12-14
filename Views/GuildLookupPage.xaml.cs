using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Navigation;

namespace JustBedwars.Views
{
    public sealed partial class GuildLookupPage : Page
    {
        private static readonly HttpClient _httpClient = new();
        private readonly SettingsService _settingsService;
        private List<string> _history = new();
        private const string HistorySettingsKey = "GuildSearchHistory";

        public GuildLookupPage()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            LoadHistory();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var lastSearchedName = _settingsService.GetValue("LastSearchedName") as string;
            if (!string.IsNullOrWhiteSpace(lastSearchedName))
            {
                AddToHistory(lastSearchedName);
                _settingsService.SetValue("LastSearchedName", null);
            }
        }

        private void LoadHistory()
        {
            var historyObject = _settingsService.GetValue(HistorySettingsKey);
            if (historyObject is JArray historyJArray)
            {
                _history = historyJArray.ToObject<List<string>>() ?? new List<string>();
            }
            HistoryListView.ItemsSource = _history;
        }

        private void SaveHistory()
        {
            _settingsService.SetValue(HistorySettingsKey, _history.ToArray());
        }

        private void AddToHistory(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            _history.RemoveAll(h => h.Equals(query, StringComparison.OrdinalIgnoreCase));
            _history.Insert(0, query);

            const int maxHistorySize = 10;
            if (_history.Count > maxHistorySize)
            {
                _history = _history.Take(maxHistorySize).ToList();
            }

            HistoryListView.ItemsSource = null;
            HistoryListView.ItemsSource = _history;
            SaveHistory();
        }

        private void SearchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchAutoSuggestBox != null)
            {
                SearchAutoSuggestBox.Text = string.Empty;
                SearchAutoSuggestBox.ItemsSource = null;
            }
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            var query = sender.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            var selectedItem = (SegmentedItem)SearchTypeSegmented.SelectedItem;
            if (selectedItem == null) return;

            string endpoint = null;
            switch (selectedItem.Content.ToString())
            {
                case "Member Name":
                    endpoint = $"http://185.194.216.210:3000/api/justbedwars/v2/autocomplete?query={query}&limit=10&mode=name";
                    break;
                case "Guild Name":
                    endpoint = $"http://185.194.216.210:3000/api/justbedwars/v2/autocomplete?query={query}&limit=10&mode=guild";
                    break;
            }

            if (endpoint != null)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(endpoint);
                    var suggestions = JsonConvert.DeserializeObject<List<string>>(response);
                    sender.ItemsSource = suggestions;
                }
                catch (HttpRequestException)
                {
                    // Handle API errors gracefully
                }
            }
            else
            {
                sender.ItemsSource = null;
            }
        }

        private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var query = args.QueryText;
            if (string.IsNullOrWhiteSpace(query)) return;

            var selectedItem = (SegmentedItem)SearchTypeSegmented.SelectedItem;
            if (selectedItem == null) return;

            var type = selectedItem.Content.ToString();

            Frame.Navigate(typeof(GuildLookupResultPage), new GuildLookupParameter { Query = query, Type = type }, new DrillInNavigationTransitionInfo());
        }
        
        private void HistoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not string clickedItem) return;

            SearchAutoSuggestBox.Text = clickedItem;
            SearchTypeSegmented.SelectedIndex = 0;
            
            var type = "Guild Name";
            
            Frame.Navigate(typeof(GuildLookupResultPage), new GuildLookupParameter { Query = clickedItem, Type = type }, new DrillInNavigationTransitionInfo());
        }

        private void DeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: string itemToRemove }) return;
            _history.Remove(itemToRemove);
            HistoryListView.ItemsSource = null;
            HistoryListView.ItemsSource = _history;
            SaveHistory();
        }
    }
}
