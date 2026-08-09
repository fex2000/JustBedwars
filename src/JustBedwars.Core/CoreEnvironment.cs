using JustBedwars.Services;

namespace JustBedwars;

public static class CoreEnvironment
{
#if WINDOWS || MICROSOFT_UI_XAML
    public static Microsoft.UI.Xaml.Window? MainWindow { get; set; }
#else
    public static object? MainWindow { get; set; }
#endif
    public static AptabaseClient? AptabaseClient { get; set; }
}
