using Microsoft.Extensions.DependencyInjection;
using StegoForge.Core.Abstractions;
using StegoForge.Formats.Bmp;
using StegoForge.Formats.Png;

namespace StegoForge.Formats;

public static class FormatServiceCollectionExtensions
{
    public static IServiceCollection AddStegoForgeFormatHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICarrierFormatHandler, PngLsbFormatHandler>();
        services.AddSingleton<ICarrierFormatHandler, BmpLsbFormatHandler>();

        return services;
    }
}
