using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JustBedwars.GTK;

public static class LibraryResolver
{
    [ModuleInitializer]
    public static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(LibraryResolver).Assembly, ResolveDll);
    }

    private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string? targetLib = libraryName switch
            {
                "GLib" => "libglib-2.0.so.0",
                "GObject" => "libgobject-2.0.so.0",
                "Gio" => "libgio-2.0.so.0",
                "Gtk" => "libgtk-4.0.so.0",
                "Adw" => "libadwaita-1.so.0",
                _ => null
            };

            if (targetLib != null && NativeLibrary.TryLoad(targetLib, assembly, searchPath, out IntPtr handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }
}