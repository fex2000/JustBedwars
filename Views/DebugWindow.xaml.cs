using JustBedwars.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;

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
    }
}
