using JustBedwars.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace JustBedwars.Views;

public sealed partial class DebugWindow : Window
{
    public DebugWindow()
    {
        InitializeComponent();
        DebugService.Instance.LogAdded += OnLogAdded;
        var logHistory = DebugService.Instance.GetLogHistory();
        foreach (var log in logHistory) DebugTextBox.Text += log + "\n";
        ExtendsContentIntoTitleBar = true;
        var presenter = OverlappedPresenter.Create();
        presenter.PreferredMinimumWidth = 900;
        presenter.PreferredMinimumHeight = 500;
        AppWindow.SetPresenter(presenter);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private void OnLogAdded(string log)
    {
        DispatcherQueue.TryEnqueue(() => { DebugTextBox.Text += log + "\n"; });
    }

    private void ButtonJoined_Click(object sender, RoutedEventArgs e)
    {
        DebugService.Instance.OnEmulatePlayerJoined(EmulateUsername.Text);
    }

    private void ButtonLeft_Click(object sender, RoutedEventArgs e)
    {
        DebugService.Instance.OnEmulatePlayerLeft(EmulateUsername.Text);
    }
}