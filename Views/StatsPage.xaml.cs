using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace JustBedwars.Views
{
    public sealed partial class StatsPage : Page
    {
        private readonly HypixelApi _hypixelApi;
        private const string ApiKeySettingName = "HypixelApiKey";

        public StatsPage()
        {
            this.InitializeComponent();
            _hypixelApi = new HypixelApi();
            LoadApiKey();
        }

        private void LoadApiKey()
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ApiKeySettingName, out object? apiKey))
            {
                _hypixelApi.SetApiKey((string)apiKey!);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            LoadingIndicator.IsActive = true;
            SetStatsVisibility(Visibility.Collapsed);

            var player = await _hypixelApi.GetPlayerStats(username);

            LoadingIndicator.IsActive = false;
            SetStatsVisibility(Visibility.Visible);

            if (player != null)
            {
                UsernameTextBlock.Text = player.Username;
                StarTextBlock.Text = player.Star.ToString();
                FkdrTextBlock.Text = player.FKDR.ToString();
                WlrTextBlock.Text = player.WLR.ToString();
                FinalsTextBlock.Text = player.Finals.ToString();
                WinsTextBlock.Text = player.Wins.ToString();
                KdrTextBlock.Text = player.KDR.ToString();
                FirstLoginTextBlock.Text = player.FirstLoginDate;
            }
            else
            {
                UsernameTextBlock.Text = "Player not found";
                StarTextBlock.Text = string.Empty;
                FkdrTextBlock.Text = string.Empty;
                WlrTextBlock.Text = string.Empty;
                FinalsTextBlock.Text = string.Empty;
                WinsTextBlock.Text = string.Empty;
                KdrTextBlock.Text = string.Empty;
                FirstLoginTextBlock.Text = string.Empty;
            }
        }

        private void SetStatsVisibility(Visibility visibility)
        {
            UsernameLabel.Visibility = visibility;
            UsernameTextBlock.Visibility = visibility;
            StarLabel.Visibility = visibility;
            StarTextBlock.Visibility = visibility;
            FkdrLabel.Visibility = visibility;
            FkdrTextBlock.Visibility = visibility;
            WlrLabel.Visibility = visibility;
            WlrTextBlock.Visibility = visibility;
            FinalsLabel.Visibility = visibility;
            FinalsTextBlock.Visibility = visibility;
            WinsLabel.Visibility = visibility;
            WinsTextBlock.Visibility = visibility;
            KdrLabel.Visibility = visibility;
            KdrTextBlock.Visibility = visibility;
            FirstLoginLabel.Visibility = visibility;
            FirstLoginTextBlock.Visibility = visibility;
        }
    }
}
