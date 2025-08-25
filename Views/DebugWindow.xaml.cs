using JustBedwars.Services;
using JustBedwars.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;

namespace JustBedwars.Views
{
    public sealed partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            this.InitializeComponent();
            DebugService.Instance.LogAdded += OnLogAdded;
            ExtendsContentIntoTitleBar = true;
            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 500;
            presenter.PreferredMinimumHeight = 400;
            AppWindow.SetPresenter(presenter);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        private void OnLogAdded(string log)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                DebugTextBox.Text += log + "\n";
            });
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
}
