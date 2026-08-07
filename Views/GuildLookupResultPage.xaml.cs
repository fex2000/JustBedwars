using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Controls;
using JustBedwars.Models;
using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Navigation;

namespace JustBedwars.Views;

public class GuildLookupParameter
{
    public string Query { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed partial class GuildLookupResultPage : Page
{
    private readonly AptabaseClient _aptabaseClient;
    private readonly HypixelApi _hypixelApi;
    private readonly SettingsService _settingsService;
    private int _currentTab;
    private bool _navigationAllowed;
    private string _query = string.Empty;
    private string _type = string.Empty;
    private Guild? guild;

    public GuildLookupResultPage()
    {
        InitializeComponent();
        _hypixelApi = new HypixelApi();
        _settingsService = new SettingsService();
        var instance = (App)Application.Current;
        _aptabaseClient = instance.AptabaseClient;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GuildLookupParameter args)
        {
            _query = args.Query;
            _type = args.Type;
            _ = LoadGuildDataAsync();
        }
    }

    private async Task LoadGuildDataAsync()
    {
        LoadingOverlay.Visibility = Visibility.Visible;

        guild = await _hypixelApi.GetGuildAsync(_query, _type);

        // Composition gives up if this isn't here
        await Task.Delay(300);

        if (guild != null)
        {
            _settingsService.SetValue("LastSearchedName", guild.Name);
            LoadingProgressRing.Visibility = Visibility.Collapsed;
            LoadingDetails.Visibility = Visibility.Visible;

            var progress = new Progress<double>(p =>
            {
                LoadingProgressBar.Value = p;
                if (LoadingPercentage != null)
                    LoadingPercentage.Text = $"{p:0}%";
            });

            await _hypixelApi.GetNamesForGuildMembers(guild.Members, progress);

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
            RankFilter.Items.Add(new SegmentedItem { Content = "Guild Master" });
            foreach (var rank in sortedRanks) RankFilter.Items.Add(new SegmentedItem { Content = rank.Name });
            RankFilter.SelectedIndex = 0;

            DescriptionTextBlock.Text = string.IsNullOrEmpty(guild.Description)
                ? "No description available."
                : guild.Description;
            PreferredGamesTextBlock.Text =
                guild.PreferredGames.Any() ? string.Join(", ", guild.PreferredGames) : "Not set.";
            LevelTextBlock.Text = guild.Level.ToString("F2");
            ExpTextBlock.Text = guild.Exp.ToString("N0");
            CreatedAtTextBlock.Text = DateTimeOffset.FromUnixTimeMilliseconds(guild.Created).ToString("D");
            GuildIdTextBlock.Text = guild.Id;
            ExpByGameTypeListView.ItemsSource = guild.ExpByGameType;
            GuildTagGrid.Visibility = string.IsNullOrEmpty(guild.Tag) ? Visibility.Collapsed : Visibility.Visible;

            LoadingOverlay.Visibility = Visibility.Collapsed;

            PageBar.Visibility = Visibility.Visible;
            TopBar.Visibility = Visibility.Visible;
            InfoView.Visibility = Visibility.Visible;
            MainContentGrid.Visibility = Visibility.Visible;

            GoBackDocked.Visibility = Visibility.Visible;
            GoBackFloating.Visibility = Visibility.Collapsed;

            var trackProps = new
            {
                guild.Name
            };

            _ = _aptabaseClient.TrackEvent("GuildLookup", trackProps);

            await Task.Delay(10);
            _navigationAllowed = true;
        }
        else
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            await Task.Delay(100);
            ErrorGrid.Visibility = Visibility.Visible;
        }
    }

    private void RankFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (guild == null) return;

        if (RankFilter.SelectedItem is SegmentedItem selectedItem)
        {
            var selectedRank = selectedItem.Content?.ToString() ?? string.Empty;
            if (selectedRank == "All")
                MembersListView.ItemsSource = guild.Members;
            else
                MembersListView.ItemsSource = guild.Members.Where(m => m.Rank == selectedRank).ToList();
        }
    }

    private void GoBackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void RanksListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GuildRank clickedRank)
        {
            var rankName = clickedRank.Name;
            var filterItem = RankFilter.Items.Cast<SegmentedItem>()
                .FirstOrDefault(i => i.Content?.ToString() == rankName);
            if (filterItem != null) RankFilter.SelectedItem = filterItem;
            PageBar.SelectedItem = PlayersSelector;
        }
    }

    private void MembersListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GuildMember clickedMember)
        {
            var mainWindow = App.Window as MainWindow;
            mainWindow?.OpenStatsPage(clickedMember.Name);
        }
    }

    private void PageBar_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageBar.SelectedIndex != _currentTab)
            _ = NavigateTab(PageBar.SelectedIndex);
    }

    private async Task NavigateTab(int tabIndex)
    {
        if (!_navigationAllowed)
            return;

        TabIndexToElement(tabIndex).Visibility = Visibility.Visible;

        var currentVisual = ElementCompositionPreview.GetElementVisual(TabIndexToElement(_currentTab));
        var currentCompositor = currentVisual.Compositor;
        var nextVisual = ElementCompositionPreview.GetElementVisual(TabIndexToElement(tabIndex));
        var nextCompositor = nextVisual.Compositor;

        ElementCompositionPreview.SetIsTranslationEnabled(TabIndexToElement(tabIndex), true);
        ElementCompositionPreview.SetIsTranslationEnabled(TabIndexToElement(_currentTab), true);

        var currentTranslationAnimation = currentCompositor.CreateVector3KeyFrameAnimation();
        var currentOpacityAnimation = currentCompositor.CreateScalarKeyFrameAnimation();
        var nextTranslationAnimation = nextCompositor.CreateVector3KeyFrameAnimation();
        var nextOpacityAnimation = nextCompositor.CreateScalarKeyFrameAnimation();

        currentTranslationAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);
        currentOpacityAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);
        nextTranslationAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);
        nextOpacityAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);

        currentOpacityAnimation.InsertKeyFrame(0, 1);
        currentOpacityAnimation.InsertKeyFrame(1, 0);
        nextOpacityAnimation.InsertKeyFrame(0, 0);
        nextOpacityAnimation.InsertKeyFrame(1, 1);

        if (tabIndex > _currentTab)
        {
            currentTranslationAnimation.InsertKeyFrame(0, new Vector3(0, 0, 0));
            currentTranslationAnimation.InsertKeyFrame(1, new Vector3(-50, 0, 0));
            nextTranslationAnimation.InsertKeyFrame(0, new Vector3(100, 0, 0));
            nextTranslationAnimation.InsertKeyFrame(1, new Vector3(0, 0, 0));
        }
        else
        {
            currentTranslationAnimation.InsertKeyFrame(0, new Vector3(0, 0, 0));
            currentTranslationAnimation.InsertKeyFrame(1, new Vector3(50, 0, 0));
            nextTranslationAnimation.InsertKeyFrame(0, new Vector3(-100, 0, 0));
            nextTranslationAnimation.InsertKeyFrame(1, new Vector3(0, 0, 0));
        }

        currentVisual.StartAnimation("Translation", currentTranslationAnimation);
        currentVisual.StartAnimation("Opacity", currentOpacityAnimation);
        nextVisual.StartAnimation("Translation", nextTranslationAnimation);
        nextVisual.StartAnimation("Opacity", nextOpacityAnimation);

        await Task.Delay(300);
        TabIndexToElement(_currentTab).Visibility = Visibility.Collapsed;
        _currentTab = tabIndex;
    }

    private Grid TabIndexToElement(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0: return InfoView;
            case 1: return PlayersView;
            case 2: return RanksView;
            default: return InfoView;
        }
    }

    private void MainScrollView_OnViewChanged(ScrollView sender, object args)
    {
        if (sender.VerticalOffset == 0)
        {
            GoBackDocked.Visibility = Visibility.Visible;
            GoBackFloating.Visibility = Visibility.Collapsed;
        }
        else
        {
            GoBackDocked.Visibility = Visibility.Collapsed;
            GoBackFloating.Visibility = Visibility.Visible;
        }
    }
}