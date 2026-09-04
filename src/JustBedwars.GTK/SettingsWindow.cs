using System;
using JustBedwars.Services;

namespace JustBedwars.GTK;

public static class SettingsWindow
{
    private static Adw.PreferencesDialog? _currentDialog;

    public static void Show(Gtk.Window parent)
    {
        if (_currentDialog != null)
        {
            _currentDialog.Present(parent);
            return;
        }

        var dialog = Adw.PreferencesDialog.New();
        var prefPage = (Adw.PreferencesPage)SettingsView.Create();

        dialog.Add(prefPage);
        _currentDialog = dialog;

        dialog.OnClosed += (sender, args) =>
        {
            _currentDialog = null;
        };

        dialog.Present(parent);
    }
}
