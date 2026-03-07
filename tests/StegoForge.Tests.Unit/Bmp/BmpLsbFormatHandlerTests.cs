using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Core.Errors;
using StegoForge.Formats.Bmp;
using Xunit;

namespace StegoForge.Tests.Unit.Bmp;

public sealed class BmpLsbFormatHandlerTests
{
    private readonly BmpLsbFormatHandler _handler = new();

    [Fact]
    public async Task EmbedAndExtract_RoundTripsPayload_For24BitBmp()
    {
        using var carrier = await CreateBmpAsync(16, 16, BmpBitsPerPixel.Pixel24);
        using var output = new MemoryStream();
        var payload = "bmp-stegoforge"u8.ToArray();

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
        using var carrier = await CreateBmpAsync(width, height, BmpBitsPerPixel.Pixel32);

        var capacity = await _handler.GetCapacityAsync(carrier);

        var expected = ((width * height * 3) / 8) - sizeof(int);
        Assert.Equal(expected, capacity);
    }

    [Fact]
    public async Task Supports_ReturnsFalse_ForUnsupportedBitDepth()
    {
        using var carrier = await CreateBmpAsync(12, 12, BmpBitsPerPixel.Pixel8);

        var supported = _handler.Supports(carrier);

        Assert.False(supported);
    }

    [Fact]
    public async Task EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedBitDepth()
    {
        using var carrier = await CreateBmpAsync(12, 12, BmpBitsPerPixel.Pixel8);
        using var output = new MemoryStream();

        await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.EmbedAsync(carrier, output, [1, 2, 3]));
    }

    private static async Task<MemoryStream> CreateBmpAsync(int width, int height, BmpBitsPerPixel bitsPerPixel)
    {
        using Image<Rgba32> image = new(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)(x * 4), (byte)(y * 3), (byte)(x + y), 255);
            }
        }

        var stream = new MemoryStream();
        await image.SaveAsBmpAsync(stream, new BmpEncoder
        {
            BitsPerPixel = bitsPerPixel,
            SupportTransparency = bitsPerPixel == BmpBitsPerPixel.Pixel32
        });

        stream.Position = 0;
        return stream;
    }
}
