using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Application.Capacity;
using StegoForge.Application.Formats;
using StegoForge.Application.Policies;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Formats.Bmp;
using StegoForge.Formats.Png;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class BmpCapacityServiceIntegrationTests
{
    private readonly ICapacityService _service = new CapacityService(new CarrierFormatResolver([new PngLsbFormatHandler(), new BmpLsbFormatHandler()]), new OperationPolicyGate());

    [Fact]
    public async Task GetCapacityAsync_BmpCarrier_ResolvesBmpHandler()
    {
        var carrierPath = await CreateCarrierFileAsync(40, 40);

        try
        {
            var response = await _service.GetCapacityAsync(new CapacityRequest(carrierPath, payloadSizeBytes: 100));

            Assert.Equal("bmp-lsb-v1", response.CarrierFormatId);
            Assert.Equal(596, response.MaximumCapacityBytes);
            Assert.Equal(468, response.SafeUsableCapacityBytes);
            Assert.True(response.CanEmbed);
        }
        finally
        {
            File.Delete(carrierPath);
        }
    }

    private static async Task<string> CreateCarrierFileAsync(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stegoforge-capacity-{Guid.NewGuid():N}.bmp");

        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(30, 40, 50, 255);
            }
        }

        await image.SaveAsBmpAsync(path, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
        return path;
    }
}
