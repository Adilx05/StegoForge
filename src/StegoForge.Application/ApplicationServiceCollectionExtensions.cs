using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StegoForge.Application.Capacity;
using StegoForge.Application.Embed;
using StegoForge.Application.Extract;
using StegoForge.Application.Formats;
using StegoForge.Application.Info;
using StegoForge.Application.Payload;
using StegoForge.Application.Validation;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Abstractions;
using StegoForge.Crypto.AesGcm;
using StegoForge.Formats;

namespace StegoForge.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStegoForgeApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddStegoForgeFormatHandlers();

        services.TryAddSingleton<ICompressionProvider, DeflateCompressionProvider>();
        services.TryAddSingleton<ICryptoProvider, AesGcmCryptoProvider>();

        services.TryAddSingleton<CarrierFormatResolver>();
        services.TryAddSingleton<OperationPolicyValidator>();
        services.TryAddSingleton<PayloadOrchestrationService>();
        services.TryAddSingleton<IPayloadEnvelopeSerializer, PayloadEnvelopeSerializer>();

        services.TryAddSingleton<IEmbedService, EmbedService>();
        services.TryAddSingleton<IExtractService, ExtractService>();
        services.TryAddSingleton<IInfoService, InfoService>();
        services.TryAddSingleton<ICapacityService, CapacityService>();

        return services;
    }
}
