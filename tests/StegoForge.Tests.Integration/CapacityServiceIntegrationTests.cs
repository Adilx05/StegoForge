using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Application.Capacity;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Application.Formats;
using StegoForge.Application.Validation;
using StegoForge.Formats.Png;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class CapacityServiceIntegrationTests
{
    private readonly ICapacityService _service = new CapacityService(new CarrierFormatResolver([new PngLsbFormatHandler()]), new OperationPolicyValidator());

    [Fact]
    public async Task GetCapacityAsync_PngCarrier_ReturnsExpectedFormatAndCanEmbedDecisions()
    {
        var carrierPath = await CreateCarrierFileAsync(width: 40, height: 40);

        try
        {
            var fitRequest = new CapacityRequest(carrierPath, payloadSizeBytes: 468);
            var fitResponse = await _service.GetCapacityAsync(fitRequest);

            Assert.Equal("png-lsb-v1", fitResponse.CarrierFormatId);
            Assert.Equal(596, fitResponse.MaximumCapacityBytes);
            Assert.Equal(468, fitResponse.SafeUsableCapacityBytes);
            Assert.Equal(128, fitResponse.EstimatedOverheadBytes);
            Assert.True(fitResponse.CanEmbed);
            Assert.Equal(0, fitResponse.RemainingBytes);
            Assert.Null(fitResponse.FailureReason);

            var overflowRequest = new CapacityRequest(carrierPath, payloadSizeBytes: 469);
            var overflowResponse = await _service.GetCapacityAsync(overflowRequest);

            Assert.False(overflowResponse.CanEmbed);
            Assert.Equal(-1, overflowResponse.RemainingBytes);
            Assert.Contains("exceeds safe png-lsb-v1 capacity by 1 byte(s)", overflowResponse.FailureReason);
            Assert.Contains("exceeds safe usable capacity", overflowResponse.ConstraintBreakdown[0]);
        }
        finally
        {
            File.Delete(carrierPath);
        }
    }

    private static async Task<string> CreateCarrierFileAsync(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stegoforge-capacity-{Guid.NewGuid():N}.png");

        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(30, 40, 50, 255);
            }
        }

        await image.SaveAsPngAsync(path, new PngEncoder
        {
            ColorType = PngColorType.RgbWithAlpha,
            BitDepth = PngBitDepth.Bit8
        });

        return path;
    }
}
