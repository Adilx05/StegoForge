using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
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
    public async Task EmbedAndExtract_RoundTripsExactFitPayload()
    {
        using var carrier = await CreateBmpAsync(40, 40, BmpBitsPerPixel.Pixel24);
        var capacity = await _handler.GetCapacityAsync(carrier);
        var payload = CreateDeterministicPayload((int)capacity, seed: 7);

        carrier.Position = 0;
        using var output = new MemoryStream();
        await _handler.EmbedAsync(carrier, output, payload);

        output.Position = 0;
        var extracted = await _handler.ExtractAsync(output);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedAsync_WhenPayloadExceedsCapacityByOneByte_ThrowsInsufficientCapacity()
    {
        using var carrier = await CreateBmpAsync(40, 40, BmpBitsPerPixel.Pixel24);
        var capacity = await _handler.GetCapacityAsync(carrier);
        var payload = CreateDeterministicPayload((int)capacity + 1, seed: 8);

        carrier.Position = 0;
        using var output = new MemoryStream();
        var exception = await Assert.ThrowsAsync<InsufficientCapacityException>(() => _handler.EmbedAsync(carrier, output, payload));

        Assert.Equal(capacity + 1, exception.RequiredBytes);
        Assert.Equal(capacity, exception.AvailableBytes);
    }

    [Fact]
    public async Task ExtractAsync_WhenEmbeddedLengthPrefixIsTruncated_ThrowsCorruptedData()
    {
        using var carrier = await CreateBmpAsync(3, 3, BmpBitsPerPixel.Pixel24);

        var exception = await Assert.ThrowsAsync<CorruptedDataException>(() => _handler.ExtractAsync(carrier));

        Assert.Contains("enough data", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StegoErrorCode.CorruptedData, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public async Task ExtractAsync_WhenEmbeddedLengthPrefixExceedsCarrierCapacity_ThrowsCorruptedData()
    {
        using var carrier = await CreateCarrierWithEmbeddedLengthPrefixAsync(100_000, width: 24, height: 24, BmpBitsPerPixel.Pixel24);

        var exception = await Assert.ThrowsAsync<CorruptedDataException>(() => _handler.ExtractAsync(carrier));

        Assert.Contains("exceeds carrier capacity", exception.Message);
        Assert.Equal(StegoErrorCode.CorruptedData, StegoErrorMapper.FromException(exception).Code);
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

        var exception = await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.EmbedAsync(carrier, output, [1, 2, 3]));

        Assert.Contains("detected 8-bit", exception.Message);
        Assert.Equal(StegoErrorCode.UnsupportedFormat, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public async Task EmbedAsync_ThrowsUnsupportedFormat_ForUnsupportedCompressionMode()
    {
        using var carrier = await CreateBmpWithHeaderMutationAsync(16, 16, BmpBitsPerPixel.Pixel32, span =>
        {
            span[30] = 1;
            span[31] = 0;
            span[32] = 0;
            span[33] = 0;
        });
        using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.EmbedAsync(carrier, output, [1, 2, 3]));

        Assert.Contains("detected mode 1", exception.Message);
        Assert.Equal(StegoErrorCode.UnsupportedFormat, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public async Task EmbedAsync_ThrowsInvalidHeader_ForTruncatedBmpHeader()
    {
        using var carrier = new MemoryStream([0x42, 0x4D, 0x01, 0x02]);
        using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InvalidHeaderException>(() => _handler.EmbedAsync(carrier, output, [1, 2, 3]));

        Assert.Contains("truncated", exception.Message);
        Assert.Equal(StegoErrorCode.InvalidHeader, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public async Task EmbedAsync_EnvelopeBeyondConfiguredLimit_ThrowsInvalidArguments()
    {
        var limited = new BmpLsbFormatHandler(new ProcessingLimits(maxEnvelopeBytes: 8, maxPayloadBytes: 8, maxHeaderBytes: 64));
        using var carrier = await CreateBmpAsync(32, 32, BmpBitsPerPixel.Pixel24);
        using var output = new MemoryStream();

        await Assert.ThrowsAsync<InvalidArgumentsException>(() => limited.EmbedAsync(carrier, output, new byte[16]));
    }

    [Fact]
    public async Task ExtractAsync_PreCanceledToken_ThrowsOperationCanceledException()
    {
        using var carrier = await CreateBmpAsync(16, 16, BmpBitsPerPixel.Pixel24);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.ExtractAsync(carrier, cts.Token));
    }

    private static async Task<MemoryStream> CreateCarrierWithEmbeddedLengthPrefixAsync(
        int payloadLength,
        int width,
        int height,
        BmpBitsPerPixel bitsPerPixel)
    {
        using var carrier = await CreateBmpAsync(width, height, bitsPerPixel);
        carrier.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(carrier);

        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, payloadLength);
        var bitIndex = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && bitIndex < header.Length * 8; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length && bitIndex < header.Length * 8; x++)
                {
                    var pixel = row[x];
                    pixel.R = EmbedBit(pixel.R, header, bitIndex++);
                    if (bitIndex < header.Length * 8)
                    {
                        pixel.G = EmbedBit(pixel.G, header, bitIndex++);
                    }

                    if (bitIndex < header.Length * 8)
                    {
                        pixel.B = EmbedBit(pixel.B, header, bitIndex++);
                    }

                    row[x] = pixel;
                }
            }
        });

        var output = new MemoryStream();
        await image.SaveAsBmpAsync(output, new BmpEncoder
        {
            BitsPerPixel = bitsPerPixel,
            SupportTransparency = bitsPerPixel == BmpBitsPerPixel.Pixel32
        });

        output.Position = 0;
        return output;
    }

    private static byte EmbedBit(byte channel, ReadOnlySpan<byte> payload, int bitIndex)
    {
        var sourceByte = payload[bitIndex / 8];
        var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
        return (byte)((channel & 0b1111_1110) | sourceBit);
    }

    private static byte[] CreateDeterministicPayload(int length, int seed)
    {
        var bytes = new byte[length];
        var state = seed;
        for (var i = 0; i < bytes.Length; i++)
        {
            state = unchecked((state * 1103515245) + 12345);
            bytes[i] = (byte)(state >> 16);
        }

        return bytes;
    }

    private static async Task<MemoryStream> CreateBmpWithHeaderMutationAsync(
        int width,
        int height,
        BmpBitsPerPixel bitsPerPixel,
        Action<byte[]> mutateHeader)
    {
        using var stream = await CreateBmpAsync(width, height, bitsPerPixel);
        var bytes = stream.ToArray();
        mutateHeader(bytes);
        return new MemoryStream(bytes, writable: false);
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
