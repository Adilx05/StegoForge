using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Core.Errors;
using StegoForge.Formats.Png;
using Xunit;

namespace StegoForge.Tests.Unit.Png;

public sealed class PngLsbFormatHandlerTests
{
    private readonly PngLsbFormatHandler _handler = new();

    [Fact]
    public async Task EmbedAndExtract_RoundTripsPayload_ForRgbPng()
    {
        using var carrier = await CreatePngAsync(16, 16, withAlpha: false);
        using var output = new MemoryStream();
        var payload = "stegoforge"u8.ToArray();

        await _handler.EmbedAsync(carrier, output, payload);

        output.Position = 0;
        var extracted = await _handler.ExtractAsync(output);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task GetCapacityAsync_MatchesRgbBitCapacityMinusHeader()
    {
        const int width = 10;
        const int height = 10;
        using var carrier = await CreatePngAsync(width, height, withAlpha: true);

        var capacity = await _handler.GetCapacityAsync(carrier);

        var expected = ((width * height * 3) / 8) - sizeof(int);
        Assert.Equal(expected, capacity);
    }

    [Fact]
    public async Task GetInfoAsync_ReturnsPngLsbV1DetailsAndCapabilities()
    {
        using var carrier = await CreatePngAsync(12, 12, withAlpha: true);

        var info = await _handler.GetInfoAsync(carrier);

        Assert.Equal("png-lsb-v1", info.FormatId);
        Assert.Equal("png-lsb-v1", info.FormatDetails.FormatId);
        Assert.True(info.SupportsCompression);
        Assert.True(info.SupportsEncryption);
        Assert.Contains(info.Diagnostics.Notes, note => note.Contains("alpha channel excluded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Supports_ReturnsFalse_ForUnsupportedGrayscaleColorType()
    {
        using var grayscale = await CreateGrayscalePngAsync();

        var supported = _handler.Supports(grayscale);

        Assert.False(supported);
    }

    [Fact]
    public async Task EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedColorType()
    {
        using var grayscale = await CreateGrayscalePngAsync();
        using var output = new MemoryStream();

        await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.EmbedAsync(grayscale, output, [1, 2, 3]));
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height, bool withAlpha)
    {
        using Image<Rgba32> image = new(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = withAlpha
                        ? new Rgba32((byte)(x * 5), (byte)(y * 3), (byte)(x + y), 128)
                        : new Rgba32((byte)(x * 5), (byte)(y * 3), (byte)(x + y), 255);
                }
            }
        });

        var stream = new MemoryStream();
        var encoder = new PngEncoder
        {
            ColorType = withAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb,
            BitDepth = PngBitDepth.Bit8
        };

        await image.SaveAsPngAsync(stream, encoder);
        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateGrayscalePngAsync()
    {
        using Image<L8> image = new(8, 8);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new L8((byte)(x + y));
                }
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, new PngEncoder
        {
            ColorType = PngColorType.Grayscale,
            BitDepth = PngBitDepth.Bit8
        });

        stream.Position = 0;
        return stream;
    }
}
