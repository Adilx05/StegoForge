using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using StegoForge.Core.Errors;

namespace StegoForge.Formats.Png;

public sealed class PngLsbCapacityAnalyzer
{
    private readonly PngLsbCapacityCalculator _calculator = new();

    public async Task<PngLsbCarrierAnalysis> AnalyzeAsync(
        Stream carrierStream,
        long requestedPayloadBytes,
        long reservedEnvelopeOverheadBytes = PngLsbCapacityCalculator.DefaultReservedEnvelopeOverheadBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var info = GetRequiredSupportedPngInfo(buffer);

        var estimate = _calculator.Calculate(
            width: info.Width,
            height: info.Height,
            channelsUsed: 3,
            reservedEnvelopeOverheadBytes: reservedEnvelopeOverheadBytes,
            requestedPayloadBytes: requestedPayloadBytes);

        return new PngLsbCarrierAnalysis(
            Width: info.Width,
            Height: info.Height,
            ChannelsUsed: 3,
            ColorType: info.ColorType.ToString(),
            Estimate: estimate);
    }

    private static PngCarrierInfo GetRequiredSupportedPngInfo(Stream stream)
    {
        if (!TryGetSupportedPngInfo(stream, out var info))
        {
            throw new UnsupportedFormatException("Carrier must be a PNG with RGB or RGBA color type and 8-bit depth for png-lsb-v1.");
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

    private readonly record struct PngCarrierInfo(int Width, int Height, PngColorType ColorType);
}

public sealed record PngLsbCarrierAnalysis(
    int Width,
    int Height,
    int ChannelsUsed,
    string ColorType,
    PngLsbCapacityEstimate Estimate);
