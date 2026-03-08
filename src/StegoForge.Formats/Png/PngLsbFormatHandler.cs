using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Formats.Png;

public sealed class PngLsbFormatHandler : ICarrierFormatHandler
{
    private static readonly PngLsbCapacityCalculator CapacityCalculator = new();
    private static readonly CarrierFormatDetails Details = new("png-lsb-v1", "PNG LSB (v1)", "1.0.0");
    private static readonly string[] SupportedColorTypes = [nameof(PngColorType.Rgb), nameof(PngColorType.RgbWithAlpha)];
    private readonly ProcessingLimits _limits;

    public PngLsbFormatHandler(ProcessingLimits? limits = null)
    {
        _limits = limits ?? ProcessingLimits.SafeDefaults;
    }

    public string Format => Details.FormatId;

    public bool Supports(Stream carrierStream)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);

        using var buffer = CreateSeekableCopy(carrierStream);
        return TryGetSupportedPngInfo(buffer, out _);
    }

    public async Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedPngInfo(buffer);
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

        if (payload.Length > _limits.MaxEnvelopeBytes)
        {
            throw new InvalidArgumentsException($"Payload envelope exceeds configured limit of {_limits.MaxEnvelopeBytes} bytes.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var inputBuffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedPngInfo(inputBuffer);

        var maxPayloadBytes = CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
        if (payload.Length > maxPayloadBytes)
        {
            throw new InsufficientCapacityException(payload.Length, maxPayloadBytes);
        }

        inputBuffer.Position = 0;
        using var image = Image.Load<Rgba32>(inputBuffer);
        var framedPayload = new byte[PngLsbCapacityCalculator.PayloadLengthPrefixBytes + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(framedPayload.AsSpan(0, PngLsbCapacityCalculator.PayloadLengthPrefixBytes), payload.Length);
        payload.CopyTo(framedPayload, PngLsbCapacityCalculator.PayloadLengthPrefixBytes);

        EmbedBits(image, framedPayload, cancellationToken);

        var encoder = new PngEncoder
        {
            ColorType = info.ColorType,
            BitDepth = PngBitDepth.Bit8,
            CompressionLevel = PngCompressionLevel.DefaultCompression
        };

        await image.SaveAsPngAsync(outputStream, encoder, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedPngInfo(buffer);

        buffer.Position = 0;
        using var image = Image.Load<Rgba32>(buffer);

        var bitReader = EnumerateCarrierBits(image, cancellationToken).GetEnumerator();
        try
        {
            var header = ReadBytes(bitReader, PngLsbCapacityCalculator.PayloadLengthPrefixBytes);
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

            if (payloadLength > _limits.MaxEnvelopeBytes)
            {
                throw new CorruptedDataException($"Embedded payload length exceeds configured limit of {_limits.MaxEnvelopeBytes} bytes.");
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
        var info = GetRequiredSupportedPngInfo(buffer);

        var maxPayloadBytes = CapacityCalculator.Calculate(info.Width, info.Height, channelsUsed: 3).MaximumRawEmbeddableBytes;
        var diagnostics = new OperationDiagnostics(
            notes:
            [
                "Channel strategy: RGB least-significant-bit embedding with alpha channel excluded.",
                $"Supported PNG color types for {Format}: {string.Join(", ", SupportedColorTypes)} (8-bit depth only).",
                "Metadata note: Standard PNG metadata is preserved when possible, but unknown ancillary chunks may not round-trip in ImageSharp."
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

    private static PngCarrierInfo GetRequiredSupportedPngInfo(Stream stream)
    {
        if (!TryGetSupportedPngInfo(stream, out var info))
        {
            throw new UnsupportedFormatException(
                $"Carrier must be a PNG with color type {string.Join(" or ", SupportedColorTypes)} and 8-bit depth for {Details.FormatId}.");
        }

        return info;
    }

    private static bool TryGetSupportedPngInfo(Stream stream, out PngCarrierInfo info)
    {
        try
        {
            info = default;
            stream.Position = 0;
            IImageFormat? format = Image.DetectFormat(stream);
            if (format is null || !string.Equals(format.Name, PngFormat.Instance.Name, StringComparison.Ordinal))
            {
                return false;
            }

            stream.Position = 0;
            var imageInfo = Image.Identify(stream);
            if (imageInfo is null)
            {
                return false;
            }

        var pngMetadata = imageInfo.Metadata.GetPngMetadata();
        if (pngMetadata.BitDepth != PngBitDepth.Bit8)
        {
            return false;
        }

        if (pngMetadata.ColorType is not (PngColorType.Rgb or PngColorType.RgbWithAlpha))
        {
            return false;
        }

        info = new PngCarrierInfo(imageInfo.Width, imageInfo.Height, pngMetadata.ColorType!.Value);
        return true;
        }
        catch (UnknownImageFormatException)
        {
            info = default;
            return false;
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

    private async Task<MemoryStream> CreateSeekableCopyAsync(Stream source, CancellationToken cancellationToken)
    {
        EnsureCarrierSizeWithinLimit(source);
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }

    private void EnsureCarrierSizeWithinLimit(Stream source)
    {
        if (!source.CanSeek || _limits.MaxCarrierSizeBytes is null)
        {
            return;
        }

        if (source.Length > _limits.MaxCarrierSizeBytes.Value)
        {
            throw new InvalidArgumentsException($"Carrier size exceeds configured limit of {_limits.MaxCarrierSizeBytes.Value} bytes.");
        }
    }

    private readonly record struct PngCarrierInfo(int Width, int Height, PngColorType ColorType);
}
