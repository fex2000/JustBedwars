using System;

namespace JustBedwars.GTK;

public static class Program
{
    public static int Main(string[] args)
    {
        // Test compilation of GLib.Functions.IdleAdd with a SourceFunc delegate
        GLib.Functions.IdleAdd(0, () =>
        {
            Console.WriteLine("Hello from idle");
            return false;
        });
        return 0;
    }
}