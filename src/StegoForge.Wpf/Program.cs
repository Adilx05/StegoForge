using System;
using StegoForge.Application;

namespace StegoForge.Wpf;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var marker = new ApplicationMarker();
        Console.WriteLine($"StegoForge WPF baseline ready: {marker.GetType().Name}");
    }
}
