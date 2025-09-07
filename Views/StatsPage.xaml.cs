using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace JustBedwars.Views
{
    public sealed partial class StatsPage : Page
    {
        private readonly HypixelApi _hypixelApi;
        private readonly SettingsService _settingsService;
        private readonly HttpClient _httpClient;
        private const string ApiKeySettingName = "HypixelApiKey";

        public StatsPage()
        {
            this.InitializeComponent();
            _hypixelApi = new HypixelApi();
            _settingsService = new SettingsService();
            _httpClient = new HttpClient();
            LoadApiKey();
        }

        private void LoadApiKey()
        {
            var apiKey = _settingsService.GetValue(ApiKeySettingName);
            if (apiKey != null)
            {
                _hypixelApi.SetApiKey((string)apiKey);
            }
        }

        private async void SearchPlayer()
        {
            var username = UsernameAutoSuggestBox.Text;
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            ProgressBars.Visibility = Visibility.Collapsed;
            SetLoaderVisibility(Visibility.Visible);
            ImageLoader.Visibility = Visibility.Visible;
            ImageError.Visibility = Visibility.Collapsed;
            SetStatsVisibility(Visibility.Collapsed);
            ContentBorder.Visibility = Visibility.Visible;
            PlayerImage.Source = null;

            var player = await _hypixelApi.GetPlayerStats(username);

            if (player != null)
            {
                if (player.PlayerTag != "NICK" && player.PlayerTag != "ERROR")
                {
                    HypixelLevelText.Text = $"Hypixel Level: {player.HypixelLevel}";
                    HypixelLevelProgress.Value = player.HypixelLevelProgress;
                    BedwarsLevelText.Text = $"Bedwars Stars: {player.Star}";
                    BedwarsLevelProgress.Value = player.BedwarsLevelProgress;
                    ProgressBars.Visibility = Visibility.Visible;
                }
                UsernameTextBlock.Text = player.Username;
                StarTextBlock.Text = player.Star.ToString();
                FkdrTextBlock.Text = player.FKDR.ToString();
                WlrTextBlock.Text = player.WLR.ToString();
                BblrTextBlock.Text = player.BBLR.ToString();
                FinalsTextBlock.Text = player.Finals.ToString();
                WinsTextBlock.Text = player.Wins.ToString();
                KdrTextBlock.Text = player.KDR.ToString();
                FinalDeathsTextBlock.Text = player.FinalDeaths.ToString();
                LossesTextBlock.Text = player.Losses.ToString();
                KillsTextBlock.Text = player.Kills.ToString();
                DeathsTextBlock.Text = player.Deaths.ToString();
                BedsLostTextBlock.Text = player.BedsLost.ToString();
                BedsTextBlock.Text = player.Beds.ToString();
                TagTextBlock.Text = player.PlayerTag;
                FirstLoginTextBlock.Text = player.FirstLoginDate;
                SetStatsVisibility(Visibility.Visible);
                SetLoaderVisibility(Visibility.Collapsed);

                if (!string.IsNullOrEmpty(player.PlayerUUID))
                {
                    PlayerImage.Source = new BitmapImage(new Uri($"https://starlightskins.lunareclipse.studio/render/default/{player.PlayerUUID}/full"));
                }
                else
                {
                    ImageError.Visibility = Visibility.Visible;
                    ImageLoader.Visibility = Visibility.Collapsed;
                    PlayerImage.Source = null;
                    StarTextBlock.Text = "Player not found!";
                    FkdrTextBlock.Text = "Error";
                    WlrTextBlock.Text = "Error";
                    BblrTextBlock.Text = "Error";
                    FinalsTextBlock.Text = "Error";
                    WinsTextBlock.Text = "Error";
                    KdrTextBlock.Text = "Error";
                    FirstLoginTextBlock.Text = "Error";
                    FinalDeathsTextBlock.Text = "Error";
                    LossesTextBlock.Text = "Error";
                    KillsTextBlock.Text = "Error";
                    DeathsTextBlock.Text = "Error";
                    TagTextBlock.Text = "Error";
                    BedsTextBlock.Text = "Error";
                    BedsLostTextBlock.Text = "Error";
                }
            }
            else
            {
                PlayerImage.Source = null;
                UsernameTextBlock.Text = "Player not found";
                StarTextBlock.Text = string.Empty;
                FkdrTextBlock.Text = string.Empty;
                WlrTextBlock.Text = string.Empty;
                BblrTextBlock.Text = string.Empty;
                FinalsTextBlock.Text = string.Empty;
                WinsTextBlock.Text = string.Empty;
                KdrTextBlock.Text = string.Empty;
                FirstLoginTextBlock.Text = string.Empty;
                FinalDeathsTextBlock.Text = string.Empty;
                LossesTextBlock.Text = string.Empty;
                KillsTextBlock.Text = string.Empty;
                DeathsTextBlock.Text = string.Empty;
                TagTextBlock.Text = string.Empty;
                BedsTextBlock.Text = string.Empty;
                BedsLostTextBlock.Text = string.Empty;
                SetLoaderVisibility(Visibility.Visible);
            }
        }

        private async void UsernameAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text;
                if (string.IsNullOrWhiteSpace(query))
                {
                    return;
                }

                try
                {
                    var response = await _httpClient.GetStringAsync($"http://185.194.216.210:3000/autocomplete?query={query}&limit=10");
                    var suggestions = JsonConvert.DeserializeObject<List<string>>(response);
                    sender.ItemsSource = suggestions;
                }
                catch (HttpRequestException)
                {
                    // Handle API errors gracefully
                }
            }
        }

        private void UsernameAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
            SearchPlayer();
        }

        private void PlayerImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            ImageLoader.Visibility = Visibility.Collapsed;
        }

        private void PlayerImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ImageLoader.Visibility = Visibility.Collapsed;
            ImageError.Visibility = Visibility.Visible;
        }

        private void SetStatsVisibility(Visibility visibility)
        {
            UsernameLabel.Visibility = visibility;
            UsernameTextBlock.Visibility = visibility;
            StarLabel.Visibility = visibility;
            StarTextBlock.Visibility = visibility;
            Stars.Visibility = visibility;
            FkdrLabel.Visibility = visibility;
            FkdrTextBlock.Visibility = visibility;
            FKDR.Visibility = visibility;
            WlrLabel.Visibility = visibility;
            WlrTextBlock.Visibility = visibility;
            WLR.Visibility = visibility;
            FinalsLabel.Visibility = visibility;
            FinalsTextBlock.Visibility = visibility;
            Finals.Visibility = visibility;
            WinsLabel.Visibility = visibility;
            WinsTextBlock.Visibility = visibility;
            Wins.Visibility = visibility;
            KdrLabel.Visibility = visibility;
            KdrTextBlock.Visibility = visibility;
            KDR.Visibility = visibility;
            FirstLoginLabel.Visibility = visibility;
            FirstLoginTextBlock.Visibility = visibility;
            FinalDeathsLabel.Visibility = visibility;
            FinalDeathsTextBlock.Visibility = visibility;
            FinalDeaths.Visibility = visibility;
            LossesLabel.Visibility = visibility;
            LossesTextBlock.Visibility = visibility;
            Losses.Visibility = visibility;
            KillsLabel.Visibility = visibility;
            KillsTextBlock.Visibility = visibility;
            Kills.Visibility = visibility;
            DeathsLabel.Visibility = visibility;
            DeathsTextBlock.Visibility = visibility;
            Deaths.Visibility = visibility;
            TagLabel.Visibility = visibility;
            TagTextBlock.Visibility = visibility;
            BblrLabel.Visibility = visibility;
            BblrTextBlock.Visibility = visibility;
            BBLR.Visibility = visibility;
            BedsLabel.Visibility = visibility;
            BedsTextBlock.Visibility = visibility;
            Beds.Visibility = visibility;
            BedsLostLabel.Visibility = visibility;
            BedsLostTextBlock.Visibility = visibility;
            BedsLost.Visibility = visibility;
        }

        private void SetLoaderVisibility(Visibility visibility)
        {
            StarsLoader.Visibility = visibility;
            FkdrLoader.Visibility = visibility;
            WlrLoader.Visibility = visibility;
            FinalsLoader.Visibility = visibility;
            FinalDeathsLoader.Visibility = visibility;
            WinsLoader.Visibility = visibility;
            LossesLoader.Visibility = visibility;
            KdrLoader.Visibility = visibility;
            KillsLoader.Visibility = visibility;
            DeathsLoader.Visibility = visibility;
            BblrLoader.Visibility = visibility;
            BedsLoader.Visibility = visibility;
            BedsLostLoader.Visibility = visibility;
            HypixelLevelProgressLoad.Visibility = visibility;
            HypixelLevelTextLoad.Visibility = visibility;
            BedwarsLevelProgressLoad.Visibility = visibility;
            BedwarsLevelTextLoad.Visibility = visibility;
            ProgressBarsLoader.Visibility = visibility;
        }

        private void UsernameAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            SearchPlayer();
        }
    }
}