using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Buffers;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Formats.Bmp;

public sealed class BmpLsbFormatHandler : ICarrierFormatHandler
{
    private static readonly BmpLsbCapacityCalculator CapacityCalculator = new();
    private static readonly CarrierFormatDetails Details = new("bmp-lsb-v1", "BMP LSB (v1)", "1.0.0");

    public string Format => Details.FormatId;

    public bool Supports(Stream carrierStream)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);

        using var buffer = CreateSeekableCopy(carrierStream);
        return TryGetSupportedBmpInfo(buffer, out _);
    }

    public async Task<bool> CanHandleAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        return TryGetSupportedBmpInfo(buffer, out _);
    }

    public async Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedBmpInfo(buffer);
        return CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
    }

    public async Task EmbedAsync(Stream carrierStream, Stream outputStream, byte[] payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (payload is null || payload.Length == 0)
        {
            throw new InvalidArgumentsException("Payload must contain at least one byte.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var inputBuffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedBmpInfo(inputBuffer);

        var maxPayloadBytes = CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
        if (payload.Length > maxPayloadBytes)
        {
            throw new InsufficientCapacityException(payload.Length, maxPayloadBytes);
        }

        inputBuffer.Position = 0;
        using var image = Image.Load<Rgba32>(inputBuffer);
        var framedPayload = new byte[BmpLsbCapacityCalculator.PayloadLengthPrefixBytes + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(framedPayload.AsSpan(0, BmpLsbCapacityCalculator.PayloadLengthPrefixBytes), payload.Length);
        payload.CopyTo(framedPayload, BmpLsbCapacityCalculator.PayloadLengthPrefixBytes);

        EmbedBits(image, framedPayload, cancellationToken);

        var encoder = new BmpEncoder
        {
            BitsPerPixel = info.BitsPerPixel,
            SupportTransparency = info.HasAlpha
        };

        await image.SaveAsBmpAsync(outputStream, encoder, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedBmpInfo(buffer);

        buffer.Position = 0;
        using var image = Image.Load<Rgba32>(buffer);

        var bitReader = EnumerateCarrierBits(image, cancellationToken).GetEnumerator();
        try
        {
            var header = ReadBytes(bitReader, BmpLsbCapacityCalculator.PayloadLengthPrefixBytes);
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
            if (payloadLength < 0)
            {
                throw new CorruptedDataException("Embedded payload length is invalid.");
            }

            var maxPayloadBytes = CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
            if (payloadLength > maxPayloadBytes)
            {
                throw new CorruptedDataException("Embedded payload length exceeds carrier capacity.");
            }

            return ReadBytes(bitReader, payloadLength);
        }
        finally
        {
            bitReader.Dispose();
        }
    }

    public async Task<CarrierInfoResponse> GetInfoAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedBmpInfo(buffer);

        var maxPayloadBytes = CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
        var diagnostics = new OperationDiagnostics(
            notes:
            [
                "Channel strategy: RGB least-significant-bit embedding with alpha channel excluded.",
                $"Supported BMP set for {Format}: {BmpLsbV1Formats.SupportedSetDescription}; detected carrier is {info.BitsPerPixel}-bit {(info.HasAlpha ? "BGRA" : "BGR")} uncompressed.",
                "Layout note: BMP row padding is normalized by ImageSharp row access and does not change embedding order."
            ],
            providerIdentifier: "SixLabors.ImageSharp");

        return new CarrierInfoResponse(
            formatId: Details.FormatId,
            formatDetails: Details,
            carrierSizeBytes: buffer.Length,
            estimatedCapacityBytes: maxPayloadBytes,
            availableCapacityBytes: maxPayloadBytes,
            embeddedDataPresent: false,
            supportsEncryption: true,
            supportsCompression: true,
            diagnostics: diagnostics);
    }

    private static void EmbedBits(Image<Rgba32> image, byte[] payload, CancellationToken cancellationToken)
    {
        var bitIndex = 0;
        var totalBits = payload.Length * 8;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && bitIndex < totalBits; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length && bitIndex < totalBits; x++)
                {
                    var pixel = row[x];
                    pixel.R = EmbedBit(pixel.R, payload, bitIndex++);
                    if (bitIndex < totalBits)
                    {
                        pixel.G = EmbedBit(pixel.G, payload, bitIndex++);
                    }

                    if (bitIndex < totalBits)
                    {
                        pixel.B = EmbedBit(pixel.B, payload, bitIndex++);
                    }

                    row[x] = pixel;
                }
            }
        });

        if (bitIndex < totalBits)
        {
            throw new CorruptedDataException("Carrier capacity was exhausted before payload embedding completed.");
        }
    }

    private static byte EmbedBit(byte channel, byte[] payload, int bitIndex)
    {
        var sourceByte = payload[bitIndex / 8];
        var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
        return (byte)((channel & 0b1111_1110) | sourceBit);
    }

    private static byte[] ReadBytes(IEnumerator<byte> bitReader, int length)
    {
        var output = new byte[length];
        for (var i = 0; i < length; i++)
        {
            var value = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                if (!bitReader.MoveNext())
                {
                    throw new CorruptedDataException("Carrier did not contain enough data for embedded payload.");
                }

                value = (value << 1) | bitReader.Current;
            }

            output[i] = (byte)value;
        }

        return output;
    }

    private static IEnumerable<byte> EnumerateCarrierBits(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        var bits = new List<byte>(checked(image.Width * image.Height * 3));

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    bits.Add((byte)(pixel.R & 1));
                    bits.Add((byte)(pixel.G & 1));
                    bits.Add((byte)(pixel.B & 1));
                }
            }
        });

        return bits;
    }

    private static BmpCarrierInfo GetRequiredSupportedBmpInfo(Stream stream)
    {
        var analysis = AnalyzeCarrier(stream);
        if (analysis.ValidationError is not null)
        {
            throw analysis.ValidationError;
        }

        return analysis.Info;
    }

    private static bool TryGetSupportedBmpInfo(Stream stream, out BmpCarrierInfo info)
    {
        var analysis = AnalyzeCarrier(stream);
        info = analysis.Info;
        return analysis.ValidationError is null;
    }

    private static BmpCarrierAnalysis AnalyzeCarrier(Stream stream)
    {
        try
        {
            stream.Position = 0;
            IImageFormat? format = Image.DetectFormat(stream);
            if (format is null || !string.Equals(format.Name, BmpFormat.Instance.Name, StringComparison.Ordinal))
            {
                return new(default, new UnsupportedFormatException("Carrier must be a BMP file for bmp-lsb-v1."));
            }

        var headerBuffer = ArrayPool<byte>.Shared.Rent(54);
        try
        {
            stream.Position = 0;
            var bytesRead = stream.Read(headerBuffer, 0, 54);
            if (bytesRead < 54)
            {
                return new(default, new InvalidHeaderException("BMP header is truncated; expected at least 54 bytes."));
            }

            if (headerBuffer[0] != (byte)'B' || headerBuffer[1] != (byte)'M')
            {
                return new(default, new InvalidHeaderException("BMP signature is invalid; expected BM marker."));
            }

            var dibHeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(headerBuffer.AsSpan(14, 4));
            if (dibHeaderSize < 40)
            {
                return new(default, new InvalidHeaderException("Unsupported BMP DIB header; expected BITMAPINFOHEADER (40 bytes) or larger."));
            }

            var width = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.AsSpan(18, 4));
            var heightRaw = BinaryPrimitives.ReadInt32LittleEndian(headerBuffer.AsSpan(22, 4));
            var bitsPerPixelValue = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(28, 2));
            var compression = BinaryPrimitives.ReadUInt32LittleEndian(headerBuffer.AsSpan(30, 4));

            if (width <= 0 || heightRaw == 0)
            {
                return new(default, new InvalidHeaderException("BMP dimensions are invalid; width must be positive and height must be non-zero."));
            }

            if (!BmpLsbV1Formats.IsSupportedBitsPerPixel(bitsPerPixelValue))
            {
                return new(default, new UnsupportedFormatException($"Unsupported BMP bit depth for bmp-lsb-v1: detected {bitsPerPixelValue}-bit; supported formats are {BmpLsbV1Formats.SupportedSetDescription}."));
            }

            if (!BmpLsbV1Formats.IsSupportedCompression(bitsPerPixelValue, compression))
            {
                return new(default, new UnsupportedFormatException($"Unsupported BMP compression for bmp-lsb-v1: detected mode {compression}; supported formats are {BmpLsbV1Formats.SupportedSetDescription}."));
            }

            var height = Math.Abs(heightRaw);
            var bitsPerPixel = bitsPerPixelValue == BmpLsbV1Formats.Bgr24BitsPerPixel ? BmpBitsPerPixel.Pixel24 : BmpBitsPerPixel.Pixel32;
            var carrierInfo = new BmpCarrierInfo(width, height, bitsPerPixel, bitsPerPixelValue == BmpLsbV1Formats.Bgra32BitsPerPixel);
            return new(carrierInfo, null);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
        }
        catch (UnknownImageFormatException)
        {
            return new(default, new UnsupportedFormatException("Carrier must be a BMP file for bmp-lsb-v1."));
        }
    }

    private static MemoryStream CreateSeekableCopy(Stream source)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var buffer = new MemoryStream();
        source.CopyTo(buffer);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task<MemoryStream> CreateSeekableCopyAsync(Stream source, CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }

    private readonly record struct BmpCarrierInfo(int Width, int Height, BmpBitsPerPixel BitsPerPixel, bool HasAlpha);

    private readonly record struct BmpCarrierAnalysis(BmpCarrierInfo Info, Exception? ValidationError);
}
