using System;

namespace JustBedwars.GTK;

public static class Program
{
    public static int Main(string[] args)
    {
#if DEBUG
        if (OperatingSystem.IsLinux())
        {
            Environment.SetEnvironmentVariable("GSK_RENDERER", "cairo");
        }
#endif

        var app = Adw.Application.New("at.fexei.jbw.gtk", Gio.ApplicationFlags.FlagsNone);

        app.OnActivate += (sender, e) =>
        {
            // Splash
            Console.WriteLine("\n     ██╗██╗   ██╗███████╗████████╗██████╗ ███████╗██████╗ ██╗    ██╗ █████╗ ██████╗ ███████╗\r\n     ██║██║   ██║██╔════╝╚══██╔══╝██╔══██╗██╔════╝██╔══██╗██║    ██║██╔══██╗██╔══██╗██╔════╝\r\n     ██║██║   ██║███████╗   ██║   ██████╔╝█████╗  ██║  ██║██║ █╗ ██║███████║██████╔╝███████╗\r\n██   ██║██║   ██║╚════██║   ██║   ██╔══██╗██╔══╝  ██║  ██║██║███╗██║██╔══██║██╔══██╗╚════██║\r\n╚█████╔╝╚██████╔╝███████║   ██║   ██████╔╝███████╗██████╔╝╚███╔███╔╝██║  ██║██║  ██║███████║\r\n ╚════╝  ╚═════╝ ╚══════╝   ╚═╝   ╚═════╝ ╚══════╝╚═════╝  ╚══╝╚══╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝\r\n                                                                                            ");

            var window = MainWindow.New(app);
            window.Present();
        };

        return app.Run(args);
    }
}