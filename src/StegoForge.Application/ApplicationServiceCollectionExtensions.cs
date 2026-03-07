using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application.Capacity;
using StegoForge.Application.Formats;
using StegoForge.Core.Abstractions;

namespace StegoForge.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStegoForgeApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CarrierFormatResolver>();
        services.AddSingleton<ICapacityService, CapacityService>();

        return services;
    }
}
