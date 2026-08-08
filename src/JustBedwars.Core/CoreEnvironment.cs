using Microsoft.UI.Xaml;
using JustBedwars.Services;

namespace JustBedwars;

public static class CoreEnvironment
{
    public static Window? MainWindow { get; set; }
    public static AptabaseClient? AptabaseClient { get; set; }
}
