using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Application;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class ApplicationServiceOrchestrationIntegrationTests
{
    [Fact]
    public void AddStegoForgeApplicationServices_ResolvesAllApplicationServices()
    {
        var services = new ServiceCollection()
            .AddStegoForgeApplicationServices()
            .BuildServiceProvider();

        Assert.NotNull(services.GetService<IEmbedService>());
        Assert.NotNull(services.GetService<IExtractService>());
        Assert.NotNull(services.GetService<IInfoService>());
        Assert.NotNull(services.GetService<ICapacityService>());
    }

    [Fact]
    public async Task EmbeddedRoundTrip_UsesApplicationServicesWithSharedOrchestration()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"stegoforge-services-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var carrierPath = Path.Combine(tempRoot, "carrier.png");
            var stegoPath = Path.Combine(tempRoot, "carrier.stego.png");
            var extractDirectory = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(extractDirectory);

            await CreateCarrierFixtureAsync(carrierPath, 220, 220);

            var payload = Enumerable.Repeat((byte)'A', 2048).ToArray();

            using var services = new ServiceCollection()
                .AddStegoForgeApplicationServices()
                .BuildServiceProvider();

            var embedService = services.GetRequiredService<IEmbedService>();
            var extractService = services.GetRequiredService<IExtractService>();
            var infoService = services.GetRequiredService<IInfoService>();

            var embedResponse = await embedService.EmbedAsync(new EmbedRequest(
                carrierPath,
                stegoPath,
                payload,
                processingOptions: new ProcessingOptions(
                    compressionMode: CompressionMode.Enabled,
                    compressionLevel: 6,
                    encryptionMode: EncryptionMode.None,
                    overwriteBehavior: OverwriteBehavior.Allow)));

            var infoResponse = await infoService.GetInfoAsync(new InfoRequest(stegoPath));
            var extractResponse = await extractService.ExtractAsync(new ExtractRequest(
                stegoPath,
                Path.Combine(extractDirectory, "payload.bin"),
                processingOptions: new ProcessingOptions(
                    compressionMode: CompressionMode.Automatic,
                    encryptionMode: EncryptionMode.Optional,
                    overwriteBehavior: OverwriteBehavior.Allow)));

            Assert.Equal(payload, extractResponse.Payload);
            Assert.Equal(embedResponse.CarrierFormatId, extractResponse.CarrierFormatId);
            Assert.Equal("stegoforge.application.embed-service", embedResponse.Diagnostics.ProviderIdentifier);
            Assert.Equal("stegoforge.application.extract-service", extractResponse.Diagnostics.ProviderIdentifier);
            Assert.Equal("stegoforge.application.info-service", infoResponse.Diagnostics.ProviderIdentifier);
            Assert.True(infoResponse.EmbeddedDataPresent);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task CreateCarrierFixtureAsync(string outputPath, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32((byte)(x % 255), (byte)(y % 255), 100, 255);
                }
            }
        });

        await image.SaveAsync(outputPath, new PngEncoder());
    }
}
