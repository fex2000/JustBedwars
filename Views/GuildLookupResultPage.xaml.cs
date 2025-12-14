using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Controls;

namespace JustBedwars.Views
{
    public class GuildLookupParameter
    {
        public string Query { get; set; }
        public string Type { get; set; }
    }

    public sealed partial class GuildLookupResultPage : Page
    {
        private string _query;
        private string _type;
        private readonly HypixelApi _hypixelApi;
        private readonly SettingsService _settingsService;
        private Guild guild;

        public GuildLookupResultPage()
        {
            InitializeComponent();
            _hypixelApi = new HypixelApi();
            _settingsService = new SettingsService();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is GuildLookupParameter args)
            {
                _query = args.Query;
                _type = args.Type;
                LoadGuildDataAsync();
            }
        }

        private async Task LoadGuildDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            guild = await _hypixelApi.GetGuildAsync(_query, _type);

            if (guild != null)
            {
                _settingsService.SetValue("LastSearchedName", guild.Name);
                LoadingProgressRing.Visibility = Visibility.Collapsed;
                LoadingDetails.Visibility = Visibility.Visible;

                var progress = new Progress<double>(p =>
                {
                    LoadingProgressBar.Value = p;
                    if(LoadingPercentage != null)
                        LoadingPercentage.Text = $"{p:0}%";
                });

                await _hypixelApi.GetNamesForGuildMembers(guild.Members, progress);

                // Set data before showing controls to avoid animation conflicts
                GuildNameTextBlock.Text = guild.Name;
                GuildTagTextBlock.Text = guild.Tag;

                // Sort ranks by priority
                var sortedRanks = guild.Ranks.OrderByDescending(r => r.Priority).ToList();
                RanksListView.ItemsSource = sortedRanks;

                // Sort players by rank priority
                var rankPriority = sortedRanks.Select((rank, index) => new { rank.Name, Priority = index })
                    .ToDictionary(r => r.Name, r => r.Priority);
                var sortedMembers = guild.Members
                    .OrderBy(m => rankPriority.ContainsKey(m.Rank) ? rankPriority[m.Rank] : int.MaxValue).ToList();
                MembersListView.ItemsSource = sortedMembers;

                // Populate rank filter
                RankFilter.Items.Clear();
                RankFilter.Items.Add(new SegmentedItem { Content = "All" });
                foreach (var rank in sortedRanks)
                {
                    RankFilter.Items.Add(new SegmentedItem { Content = rank.Name });
                }
                RankFilter.SelectedIndex = 0;

                DescriptionTextBlock.Text = string.IsNullOrEmpty(guild.Description) ? "No description available." : guild.Description;
                PreferredGamesTextBlock.Text = guild.PreferredGames.Any() ? string.Join(", ", guild.PreferredGames) : "Not set.";
                LevelTextBlock.Text = guild.Level.ToString("F2");
                ExpTextBlock.Text = guild.Exp.ToString("N0");
                OnlinePlayersTextBlock.Text = guild.OnlinePlayers.ToString();
                CreatedAtTextBlock.Text = DateTimeOffset.FromUnixTimeMilliseconds(guild.Created).ToString("D");
                GuildIdTextBlock.Text = guild.Id;
                ExpByGameTypeListView.ItemsSource = guild.ExpByGameType;

                LoadingOverlay.Visibility = Visibility.Collapsed;

                SelectorBar.Visibility = Visibility.Visible;
                TopBar.Visibility = Visibility.Visible;
                InfoView.Visibility = Visibility.Visible;
            }
            else
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                await Task.Delay(100);
                ErrorGrid.Visibility = Visibility.Visible;
            }
        }

        private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (InfoSelector != null && PlayersSelector != null && RanksSelector != null)
            {
                if (SelectorBar.SelectedItem == InfoSelector)
                {
                    InfoView.Visibility = Visibility.Visible;
                    PlayersView.Visibility = Visibility.Collapsed;
                    RanksView.Visibility = Visibility.Collapsed;
                }
                else if (SelectorBar.SelectedItem == PlayersSelector)
                {
                    InfoView.Visibility = Visibility.Collapsed;
                    PlayersView.Visibility = Visibility.Visible;
                    RanksView.Visibility = Visibility.Collapsed;
                }
                else if (SelectorBar.SelectedItem == RanksSelector)
                {
                    InfoView.Visibility = Visibility.Collapsed;
                    PlayersView.Visibility = Visibility.Collapsed;
                    RanksView.Visibility = Visibility.Visible;
                }
            }
        }

        private void RankFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (guild == null) return;

            if (RankFilter.SelectedItem is SegmentedItem selectedItem)
            {
                string selectedRank = selectedItem.Content.ToString();
                if (selectedRank == "All")
                {
                    MembersListView.ItemsSource = guild.Members;
                }
                else
                {
                    MembersListView.ItemsSource = guild.Members.Where(m => m.Rank == selectedRank).ToList();
                }
            }
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void RanksListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GuildRank clickedRank)
            {
                var rankName = clickedRank.Name;
                var filterItem = RankFilter.Items.Cast<SegmentedItem>().FirstOrDefault(i => i.Content.ToString() == rankName);
                if (filterItem != null)
                {
                    RankFilter.SelectedItem = filterItem;
                }
                SelectorBar.SelectedItem = PlayersSelector;
            }
        }

        private void MembersListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GuildMember clickedMember)
            {
                // This might need adjustment depending on the main navigation structure.
                var mainWindow = App.Window as MainWindow;
                mainWindow?.OpenStatsPage(clickedMember.Name);
            }
        }
    }
}
