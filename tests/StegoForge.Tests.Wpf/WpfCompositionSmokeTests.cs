using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Wpf;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class WpfCompositionSmokeTests
{
    [Fact]
    public void CompositionContainer_ResolvesMainWindowAndOperationViewModels()
    {
        using var provider = BuildWpfServiceProvider();

        var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();
        var embedViewModel = provider.GetRequiredService<EmbedViewModel>();
        var extractViewModel = provider.GetRequiredService<ExtractViewModel>();

        Assert.NotNull(mainWindowViewModel);
        Assert.NotNull(embedViewModel);
        Assert.NotNull(extractViewModel);
    }

    [Fact]
    public void CommandBindings_InitializeWithoutExceptions()
    {
        using var provider = BuildWpfServiceProvider();
        var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();

        var exception = Record.Exception(() =>
        {
            _ = mainWindowViewModel.Embed.CheckCapacityCommand.CanExecute(null);
            _ = mainWindowViewModel.Embed.GetInfoCommand.CanExecute(null);
            _ = mainWindowViewModel.Embed.EmbedCommand.CanExecute(null);
            _ = mainWindowViewModel.Embed.BrowseCarrierCommand.CanExecute(null);
            _ = mainWindowViewModel.Embed.BrowsePayloadCommand.CanExecute(null);
            _ = mainWindowViewModel.Embed.BrowseOutputCommand.CanExecute(null);

            _ = mainWindowViewModel.Extract.ExtractCommand.CanExecute(null);
            _ = mainWindowViewModel.Extract.BrowseCarrierCommand.CanExecute(null);
            _ = mainWindowViewModel.Extract.BrowseOutputCommand.CanExecute(null);
        });

        Assert.Null(exception);
    }

    [WpfFact]
    public void StartupCompositionPath_ResolvesMainWindow()
    {
        using var provider = BuildWpfServiceProvider();
        var mainWindow = provider.GetRequiredService<MainWindow>();

        Assert.NotNull(mainWindow);
        Assert.IsType<MainWindowViewModel>(mainWindow.DataContext);
        mainWindow.Close();
    }

    private static ServiceProvider BuildWpfServiceProvider()
    {
        var configureServicesMethod = typeof(App).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(configureServicesMethod);

        var result = configureServicesMethod.Invoke(null, null);
        return Assert.IsType<ServiceProvider>(result);
    }
}
