using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using CommunityToolkit.WinUI.Controls;
using Newtonsoft.Json;

namespace JustBedwars.Views
{
    public sealed partial class GuildLookupPage : Page
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public GuildLookupPage()
        {
            InitializeComponent();
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
                    endpoint = $"http://185.194.216.210:3000/autocomplete?query={query}&limit=10";
                    break;
                case "Guild Name":
                    endpoint = $"http://185.194.216.210:3000/guildautocomplete?query={query}&limit=10";
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
            var selectedItem = (SegmentedItem)SearchTypeSegmented.SelectedItem;
            if (selectedItem == null) return;

            var query = args.QueryText;
            var type = selectedItem.Content.ToString();

            var window = new GuildWindow(query, type);
            window.Activate();
            
            
        }
    }
}