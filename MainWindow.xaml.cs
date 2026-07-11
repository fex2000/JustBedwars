using JustBedwars.Services;
using JustBedwars.Views;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI.ApplicationSettings;
using DevWinUI;
using Microsoft.UI;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars
{
    public sealed partial class MainWindow : Window
    {
        OverlappedPresenter presenter = OverlappedPresenter.Create();
        private readonly SettingsService _settingsService;

        private AptabaseClient _aptabaseClient;

        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        DesktopAcrylicController m_acrylicController;
        SystemBackdropConfiguration m_configurationSource;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public MainWindow(SettingsService settingsService, AptabaseClient aptabaseClient)
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);
            presenter.PreferredMinimumWidth = 900;
            presenter.PreferredMinimumHeight = 620;
            AppWindow.SetPresenter(presenter);
            _ = UpdateService.CheckForUpdates();

            _aptabaseClient = aptabaseClient;
            _settingsService = settingsService;

            AppWindow.Closing += AppWindowOnClosing;
        }

        private void AppWindowOnClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            _ = _aptabaseClient.TrackEvent("AppClosing");
            args.Cancel = false;
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
        private async void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isOnTop == false)
            {
                WindowHelper.HideWindow(this);
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
                AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 500));
                AppWindow.Move(new PointInt32(DisplayArea.Primary.WorkArea.Width - 812, 12));

                Thickness otThickness = new Thickness();
                otThickness.Right = AppWindow.TitleBar.RightInset;
                AlwaysOnTopButton.Margin = otThickness;
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
                margin.Bottom = -32;
                NavView.Margin = margin;
                NavView.Header = null;
                margin.Top = 24;
                margin.Bottom = 0;
                ContentFrame.Margin = margin;
                TitleBar.Subtitle = "Overlay";
                TitleBar.IsPaneToggleButtonVisible = false;
                SetAlwaysOnTopTitleBarVisible(false);

                await Task.Delay(150);
                WindowHelper.ShowWindow(this);
                WindowHelper.SetForegroundWindow(this);
            }
            else
            {
                isOnTop = false;
                AppWindow.SetPresenter(presenter);
                presenter.Restore();
                NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
                Thickness margin = NavView.Margin;
                margin.Top = 32;
                margin.Bottom = 0;
                NavView.Margin = margin;
                Thickness padding = ContentFrame.Margin;
                margin.Top = 0;
                ContentFrame.Margin = margin;
                TitleBar.Subtitle = "App";
                TitleBar.IsPaneToggleButtonVisible = true;
                AlwaysOnTopButton.Content = "\uE8A7";
                if (m_acrylicController != null)
                {
                    m_acrylicController.Dispose();
                    m_acrylicController = null;
                }
                SystemBackdrop = new MicaBackdrop();
                if(preTopPage == typeof(SettingsView))
                    NavView_Navigate(preTopPage, new DrillInNavigationTransitionInfo(), _settingsService);
                else
                    NavView_Navigate(preTopPage, new DrillInNavigationTransitionInfo());
                NavView.IsPaneOpen = false;
                Thickness otThickness = new Thickness();
                otThickness.Right = AppWindow.TitleBar.RightInset;
                AlwaysOnTopButton.Margin = otThickness;
                SetAlwaysOnTopTitleBarVisible(true);
            }
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

                m_acrylicController.TintColor = Microsoft.UI.Colors.Black;
                m_acrylicController.TintOpacity = 0.4f;
                m_acrylicController.LuminosityOpacity = 0f;

                m_acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);

                return true;
            }

            return false;
        }

        private void TitleBar_OnPaneToggleRequested(TitleBar? sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        public bool IsCursorInsideWindow()
        {
            if (!GetCursorPos(out POINT pointerPosition))
                return false;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var winX = appWindow.Position.X;
            var winY = appWindow.Position.Y;
            var winWidth = appWindow.Size.Width;
            var winHeight = appWindow.Size.Height;

            return pointerPosition.X >= winX && pointerPosition.X <= (winX + winWidth) &&
                   pointerPosition.Y >= winY && pointerPosition.Y <= (winY + winHeight);
        }

        private async void CheckCursorInWindow()
        {
            await Task.Delay(5);
            SetAlwaysOnTopTitleBarVisible(IsCursorInsideWindow());
            
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (isOnTop)
                CheckCursorInWindow();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (isOnTop)
                CheckCursorInWindow();
        }

        private void SetAlwaysOnTopTitleBarVisible(bool visible)
        {
            var tempPresenter = AppWindow.Presenter as OverlappedPresenter;
            var titleVisual = ElementCompositionPreview.GetElementVisual(TitleBar);
            var titleCompositor = titleVisual.Compositor;

            var contentVisual = ElementCompositionPreview.GetElementVisual(NavView);
            var contentCompositor = contentVisual.Compositor;

            var buttonVisual = ElementCompositionPreview.GetElementVisual(AlwaysOnTopButton);
            var buttonCompositor = buttonVisual.Compositor;

            var titleAnimation = titleCompositor.CreateVector3KeyFrameAnimation();
            var contentAnimation = contentCompositor.CreateVector3KeyFrameAnimation();
            var buttonAnimation = contentCompositor.CreateVector3KeyFrameAnimation();
            titleAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);
            contentAnimation.Duration = new TimeSpan(0, 0, 0, 0, 300);
            if (visible)
            {
                titleAnimation.InsertKeyFrame(1, new Vector3(0f, 0f, 0f));
                contentAnimation.InsertKeyFrame(1, new Vector3(0f, 0f, 0f));
                buttonAnimation.InsertKeyFrame(1, new Vector3(0f, 0f, 0f));
                tempPresenter.SetBorderAndTitleBar(true, true);
            }
            else
            {
                titleAnimation.InsertKeyFrame(1, new Vector3(0f, -32f, 0f));
                contentAnimation.InsertKeyFrame(1, new Vector3(0f, -28f, 0f));
                buttonAnimation.InsertKeyFrame(1, new Vector3(0f, -32f, 0f));
                tempPresenter.SetBorderAndTitleBar(true, false);
            }

            AppWindow.SetPresenter(tempPresenter);

            titleVisual.StartAnimation("Translation", titleAnimation);
            contentVisual.StartAnimation("Translation", contentAnimation);
            buttonVisual.StartAnimation("Translation", buttonAnimation);
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
