using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Wpf.ViewModels;

namespace StegoForge.Wpf;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = ConfigureServices();

        var window = _serviceProvider.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        global::StegoForge.Application.ApplicationServiceCollectionExtensions.AddStegoForgeApplicationServices(services);

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<EmbedViewModel>();
        services.AddTransient<ExtractViewModel>();

        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
