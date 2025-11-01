using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ApplicationSettings;
using Microsoft.UI.Windowing;
using System.Threading.Tasks;
using Windows.Media.Control;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using System.Runtime.InteropServices;
using WinRT;
using JustBedwars.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars
{
    public sealed partial class MainWindow : Window
    {
        OverlappedPresenter presenter = OverlappedPresenter.Create();
        private readonly SettingsService _settingsService;
        private GlobalSystemMediaTransportControlsSessionManager _mediaManager;
        private GlobalSystemMediaTransportControlsSession _currentSession;

        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        DesktopAcrylicController m_acrylicController;
        SystemBackdropConfiguration m_configurationSource;

        public MainWindow(Services.SettingsService settingsService)
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            presenter.PreferredMinimumWidth = 900;
            presenter.PreferredMinimumHeight = 620;
            AppWindow.SetPresenter(presenter);
            _ = UpdateService.CheckForUpdates();

            _settingsService = settingsService;
            _settingsService.SettingChanged += SettingsService_SettingChanged;
            LoadMediaPlayerSetting();
            InitializeMedia();
        }

        public void OpenStatsPage(string username)
        {
            if (ContentFrame.CurrentSourcePageType == typeof(StatsPage))
            {
                (ContentFrame.Content as StatsPage).LoadPlayerStats(username);
            }
            else
            {
                NavView_Navigate(typeof(StatsPage), new EntranceNavigationTransitionInfo(), username);
            }
            this.Activate();
        }

        private async void InitializeMedia()
        {
            _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _mediaManager.CurrentSessionChanged += MediaManager_CurrentSessionChanged;
            UpdateCurrentSession();
        }

        private void MediaManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentSession();
        }

        private void UpdateCurrentSession()
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
            }

            _currentSession = _mediaManager.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
                DispatcherQueue.TryEnqueue(async () => await UpdateMediaProperties());
                DispatcherQueue.TryEnqueue(() => UpdatePlaybackInfo());
                DispatcherQueue.TryEnqueue(() => UpdateTimeline());
            }
        }

        private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await UpdateMediaProperties();
            });
        }

        private async Task UpdateMediaProperties()
        {
            if (_currentSession == null) return;

            var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();

            MediaTitle.Text = mediaProperties.Title ?? "Unknown Title";
            MediaAuthor.Text = mediaProperties.Artist ?? "Unknown Artist";

            var thumbnail = mediaProperties.Thumbnail;
            if (thumbnail != null)
            {
                using (var stream = await thumbnail.OpenReadAsync())
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    MediaImage.Source = bitmap;
                    MediaImageBG.Source = bitmap;
                }
            }
            else
            {
                MediaImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icon.ico"));
                MediaImageBG.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icon.ico"));
            }
        }

        private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdatePlaybackInfo();
            });
        }

        private void UpdatePlaybackInfo()
        {
            if (_currentSession == null) return;

            var playbackInfo = _currentSession.GetPlaybackInfo();
            if (PlayPauseButton != null)
            {
                PlayPauseButton.Content = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ? "\uE769" : "\uE768";
            }
        }

        private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateTimeline();
            });
        }

        private void UpdateTimeline()
        {
            if (_currentSession == null) return;

            var timelineProperties = _currentSession.GetTimelineProperties();
            if (timelineProperties.EndTime > TimeSpan.Zero)
            {
                MediaProgressBar.Maximum = timelineProperties.EndTime.TotalSeconds;
                MediaProgressBar.Value = timelineProperties.Position.TotalSeconds;
            }
        }

        private void SettingsService_SettingChanged(object sender, string key)
        {
            if (key == "ShowMediaPlayer")
            {
                    LoadMediaPlayerSetting();
            }
        }

        private void LoadMediaPlayerSetting()
        {
            var showMediaPlayer = _settingsService.GetValue("ShowMediaPlayer");
            if (showMediaPlayer != null && (bool)showMediaPlayer)
            {
                if ((bool)showMediaPlayer == true) {
                    if (NavView.IsPaneOpen == true)
                        MediaPlayerGrid.Visibility = Visibility.Visible;
                    else
                        MediaPlayerGrid.Visibility = Visibility.Collapsed;
                }
                else MediaPlayerGrid.Visibility = Visibility.Collapsed;
            }
            else MediaPlayerGrid.Visibility = Visibility.Collapsed;
        }

        private double NavViewCompactModeThresholdWidth { get { return NavView.CompactModeThresholdWidth; } }

        public bool isOnTop;
        private Type preTopPage;

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            

            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_Navigated;

            // NavView doesn't load any page by default, so load home page.
            NavView.SelectedItem = NavView.MenuItems[0];
            // If navigation occurs on SelectionChanged, this isn't needed.
            // Because we use ItemInvoked to navigate, we need to call Navigate
            // here to load the home page.
            NavView_Navigate(typeof(Views.PlayerList), new EntranceNavigationTransitionInfo(), null);
        }

        private void NavView_ItemInvoked(NavigationView sender,
                                         NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo, _settingsService);
            }
            else if (args.InvokedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo, null);
            }
        }

        // NavView_SelectionChanged is not used in this example, but is shown for completeness.
        // You will typically handle either ItemInvoked or SelectionChanged to perform navigation,
        // but not both.
        private void NavView_SelectionChanged(NavigationView sender,
                                              NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo, _settingsService);
            }
            else if (args.SelectedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.SelectedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo, null);
            }
        }

        private void NavView_Navigate(
            Type navPageType,
            NavigationTransitionInfo transitionInfo, object parameter = null)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, parameter, transitionInfo);
            }
        }

        private void NavView_BackRequested(NavigationView sender,
                                           NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Don't go back if the nav pane is overlayed.
            if (NavView.IsPaneOpen &&
                (NavView.DisplayMode == NavigationViewDisplayMode.Compact ||
                 NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            BackButton.IsEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(Views.SettingsView))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                NavView.Header = "Settings";
            }
            else if (ContentFrame.SourcePageType != null)
            {
                var selectedItem = NavView.MenuItems
                    .OfType<NavigationViewItem>()
                    .FirstOrDefault(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));
                if (selectedItem != null)
                {
                    NavView.SelectedItem = selectedItem;
                    NavView.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TryGoBack();
        }

        private void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isOnTop == false)
            {
                isOnTop = true;
                OverlappedPresenter alwaysontop = OverlappedPresenter.Create();
                alwaysontop.IsAlwaysOnTop = true;
                alwaysontop.IsMaximizable = false;
                alwaysontop.IsMinimizable = false;
                alwaysontop.PreferredMaximumHeight = 700;
                alwaysontop.PreferredMinimumHeight = 300;
                alwaysontop.PreferredMaximumWidth = 1200;
                alwaysontop.PreferredMinimumWidth = 550;
                alwaysontop.SetBorderAndTitleBar(true, true);
                AppWindow.SetPresenter(alwaysontop);
                alwaysontop.Restore();
                AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));

                AlwaysOnTopButton.HorizontalAlignment = HorizontalAlignment.Left;
                AlwaysOnTopButton.Content = "\uE944";
                SystemBackdrop = null;
                TrySetAcrylicBackdrop();

                NavView.IsPaneToggleButtonVisible = false;
                NavView.SelectedItem = "JustBedwars.Views.PlayerList";
                Type preNavPageType = ContentFrame.CurrentSourcePageType;
                preTopPage = ContentFrame.CurrentSourcePageType;
                if (preTopPage != typeof(Views.PlayerList))
                {
                    NavView_Navigate(typeof(Views.PlayerList), new DrillInNavigationTransitionInfo());
                }
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                Thickness margin = NavView.Margin;
                margin.Top = 0;
                NavView.Margin = margin;
                NavView.Header = null;
                Thickness padding = ContentFrame.Margin;
                margin.Top = 24;
                ContentFrame.Margin = margin;
                AppTitle.Text = "JustBedwars Overlay";
            }
            else
            {
                AlwaysOnTopButton.HorizontalAlignment = HorizontalAlignment.Right;
                isOnTop = false;
                AppWindow.SetPresenter(presenter);
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                presenter.Restore();
                Thickness margin = NavView.Margin;
                margin.Top = 32;
                NavView.Margin = margin;
                Thickness padding = ContentFrame.Margin;
                margin.Top = 0;
                ContentFrame.Margin = margin;
                NavView.IsPaneToggleButtonVisible = true;
                AppTitle.Text = "JustBedwars";
                AlwaysOnTopButton.Content = "\uE8A7";
                if (m_acrylicController != null)
                {
                    m_acrylicController.Dispose();
                    m_acrylicController = null;
                }
                SystemBackdrop = new MicaBackdrop();
                NavView_Navigate(preTopPage, new DrillInNavigationTransitionInfo());
                NavView.IsPaneOpen = false;
            }
        }

        private async void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
            {
                await _currentSession.TrySkipPreviousAsync();
            }
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession != null)
            {
                await _currentSession.TrySkipNextAsync();
            }
        }

        private void NavView_PaneOpening(NavigationView sender, object args)
        {
            LoadMediaPlayerSetting();
        }

        private void NavView_PaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            LoadMediaPlayerSetting();
        }

        bool TrySetAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                m_configurationSource = new SystemBackdropConfiguration();

                m_configurationSource.IsInputActive = true;

                switch (((FrameworkElement)this.Content).ActualTheme)
                {
                    case ElementTheme.Dark:
                        m_configurationSource.Theme = SystemBackdropTheme.Dark;
                        break;
                    case ElementTheme.Light:
                        m_configurationSource.Theme = SystemBackdropTheme.Light;
                        break;
                    case ElementTheme.Default:
                        m_configurationSource.Theme = SystemBackdropTheme.Default;
                        break;
                }

                m_acrylicController = new DesktopAcrylicController();

                m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);

                return true;
            }

            return false;
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;
                options.apartmentType = 2;

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
