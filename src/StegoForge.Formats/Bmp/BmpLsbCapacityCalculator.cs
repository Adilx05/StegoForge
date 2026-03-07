namespace StegoForge.Formats.Bmp;

public sealed class BmpLsbCapacityCalculator
{
    public const int PayloadLengthPrefixBytes = sizeof(int);
    public const int DefaultReservedEnvelopeOverheadBytes = 128;

    public BmpLsbCapacityEstimate Calculate(
        int width,
        int height,
        int channelsUsed,
        long reservedEnvelopeOverheadBytes = DefaultReservedEnvelopeOverheadBytes,
        long requestedPayloadBytes = 0)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        if (channelsUsed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channelsUsed), "At least one channel must be used.");
        }

        if (reservedEnvelopeOverheadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedEnvelopeOverheadBytes), "Reserved overhead cannot be negative.");
        }

        if (requestedPayloadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPayloadBytes), "Requested payload cannot be negative.");
        }

        var rawEmbeddableBytes = GetMaximumRawEmbeddableBytes(width, height, channelsUsed);
        return CalculateFromRaw(rawEmbeddableBytes, reservedEnvelopeOverheadBytes, requestedPayloadBytes);
    }

    public BmpLsbCapacityEstimate CalculateFromRaw(
        long maximumRawEmbeddableBytes,
        long reservedEnvelopeOverheadBytes = DefaultReservedEnvelopeOverheadBytes,
        long requestedPayloadBytes = 0)
    {
        if (maximumRawEmbeddableBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRawEmbeddableBytes), "Raw embeddable capacity cannot be negative.");
        }

        if (reservedEnvelopeOverheadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedEnvelopeOverheadBytes), "Reserved overhead cannot be negative.");
        }

        if (requestedPayloadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPayloadBytes), "Requested payload cannot be negative.");
        }

        var safeUsableBytes = Math.Max(0L, maximumRawEmbeddableBytes - reservedEnvelopeOverheadBytes);
        var canEmbed = requestedPayloadBytes <= safeUsableBytes;

        return new BmpLsbCapacityEstimate(
            MaximumRawEmbeddableBytes: maximumRawEmbeddableBytes,
            SafeUsableBytes: safeUsableBytes,
            ReservedEnvelopeOverheadBytes: reservedEnvelopeOverheadBytes,
            CanEmbedRequestedPayload: canEmbed,
            ConstraintDiagnostics: canEmbed
                ? []
                : BuildConstraintDiagnostics(requestedPayloadBytes, safeUsableBytes, maximumRawEmbeddableBytes, reservedEnvelopeOverheadBytes));
    }

    public static long GetMaximumRawEmbeddableBytes(int width, int height, int channelsUsed)
    {
        var totalCarrierBits = checked((long)width * height * channelsUsed);
        var totalCarrierBytes = totalCarrierBits / 8L;
        return Math.Max(0L, totalCarrierBytes - PayloadLengthPrefixBytes);
    }

    private static IReadOnlyList<string> BuildConstraintDiagnostics(
        long requestedPayloadBytes,
        long safeUsableBytes,
        long rawEmbeddableBytes,
        long reservedEnvelopeOverheadBytes)
    {
        var overflowBytes = requestedPayloadBytes - safeUsableBytes;

        return
        [
            $"Requested payload ({requestedPayloadBytes} bytes) exceeds safe usable capacity ({safeUsableBytes} bytes) by {overflowBytes} byte(s).",
            $"Safe usable capacity = raw embeddable capacity ({rawEmbeddableBytes} bytes) - reserved envelope overhead ({reservedEnvelopeOverheadBytes} bytes)."
        ];
    }
}

public sealed record BmpLsbCapacityEstimate(
    long MaximumRawEmbeddableBytes,
    long SafeUsableBytes,
    long ReservedEnvelopeOverheadBytes,
    bool CanEmbedRequestedPayload,
    IReadOnlyList<string> ConstraintDiagnostics);
