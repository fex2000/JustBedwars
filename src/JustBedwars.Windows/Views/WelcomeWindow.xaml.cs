using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using DevWinUI;
using JustBedwars.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace JustBedwars.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomeWindow : Window
{
    private readonly SettingsService _settingsService;

    public WelcomeWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            AppWindow.SetPresenter(presenter);
        }

        AppWindow.Resize(new SizeInt32(1000, 700));

        ContentGrid.Lights.Add(new HoverLight());
        ContentGrid.Lights.Add(new AmbLight());

        WindowHelper.RemoveWindowBorderAndTitleBar(this);
        WindowHelper.CenterOnScreen(this);
        WindowHelper.SetWindowCornerRadius(this, NativeValues.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND);

        _ = LoadValues();

        FinishButton.BubbleForeground = new SolidColorBrush(App.Current.RequestedTheme == ApplicationTheme.Light
            ? new UISettings().GetColorValue(UIColorType.Accent)
            : new UISettings().GetColorValue(UIColorType.AccentLight2));
    }

    private async Task LoadValues()
    {
        if (_settingsService.GetValue("LogFilePath") is string logFilePath)
            LogFilePathTextBox.Text = logFilePath;
        else
            LogFilePathTextBox.Text =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft",
                    "logs", "latest.log");

        if (_settingsService.GetValue("SendUsageStats") is bool sendUsageStats)
            UsageStatisticsSwitch.IsOn = sendUsageStats;
        else
            UsageStatisticsSwitch.IsOn = true;
    }

    public async Task RunSetup()
    {
        Activate();

        await Task.Delay(100);

        LogFilePathTextBox.Width = ContentGrid.ActualWidth - 108;

        var tcs = new TaskCompletionSource<bool>();

        RoutedEventHandler handler = null!;
        handler = (sender, e) =>
        {
            FinishButton.Click -= handler;
            tcs.SetResult(true);
        };

        FinishButton.Click += handler;
        await tcs.Task;

        ScrollView.Visibility = Visibility.Collapsed;

        await Task.Delay(100);

        BackdropElement.Visibility = Visibility.Collapsed;
        ContentGrid.Visibility = Visibility.Collapsed;

        _settingsService.SetValue("FirstLaunch", false);
        _settingsService.SetValue("LogFilePath", LogFilePathTextBox.Text);
        _settingsService.SetValue("SendUsageStats", UsageStatisticsSwitch.IsOn);
        _settingsService.Save();

        await Task.Delay(550);
    }

    private async void SelectLogFileButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".log");

        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null) LogFilePathTextBox.Text = file.Path;
    }

    private void SetCenter(object sender, RoutedEventArgs e)
    {
        var visual = ElementCompositionPreview.GetElementVisual(sender as UIElement);
        var compositor = visual.Compositor;

        var centerPointAnimation =
            compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");

        visual.StartAnimation("CenterPoint", centerPointAnimation);
    }
}