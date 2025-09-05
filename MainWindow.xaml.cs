using JustBedwars.Services;
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


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        OverlappedPresenter presenter = OverlappedPresenter.Create();

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            presenter.PreferredMinimumWidth = 900;
            presenter.PreferredMinimumHeight = 620;
            AppWindow.SetPresenter(presenter);
            _ = UpdateService.CheckForUpdates();
        }

        private double NavViewCompactModeThresholdWidth { get { return NavView.CompactModeThresholdWidth; } }

        private bool isOnTop;
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
            NavView_Navigate(typeof(Views.PlayerList), new EntranceNavigationTransitionInfo());
        }

        private void NavView_ItemInvoked(NavigationView sender,
                                         NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
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
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.SelectedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.SelectedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(
            Type navPageType,
            NavigationTransitionInfo transitionInfo)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, null, transitionInfo);
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
                // Select the nav view item that corresponds to the page being navigated to.
                NavView.SelectedItem = NavView.MenuItems
                            .OfType<NavigationViewItem>()
                            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));

                NavView.Header =
                    ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();

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
                SystemBackdrop = new DesktopAcrylicBackdrop();

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
                SystemBackdrop = new MicaBackdrop();
                NavView_Navigate(preTopPage, new DrillInNavigationTransitionInfo());
            }
        }
    }
}

