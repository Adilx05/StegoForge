using System;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application;
using StegoForge.Core.Abstractions;

namespace StegoForge.Wpf;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var services = new ServiceCollection()
            .AddStegoForgeApplicationServices()
            .BuildServiceProvider();

        _ = services.GetRequiredService<IEmbedService>();
        _ = services.GetRequiredService<IExtractService>();
        _ = services.GetRequiredService<IInfoService>();
        _ = services.GetRequiredService<ICapacityService>();

        Console.WriteLine("StegoForge WPF baseline ready.");
    }
}
