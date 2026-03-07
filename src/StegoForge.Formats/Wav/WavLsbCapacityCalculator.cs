namespace StegoForge.Formats.Wav;

public sealed class WavLsbCapacityCalculator
{
    public const int PayloadLengthPrefixBytes = sizeof(int);
    public const int DefaultReservedEnvelopeOverheadBytes = 128;

    public WavLsbCapacityEstimate CalculateFromSampleCount(
        long sampleCount,
        long reservedEnvelopeOverheadBytes = DefaultReservedEnvelopeOverheadBytes,
        long requestedPayloadBytes = 0)
    {
        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count cannot be negative.");
        }

        if (reservedEnvelopeOverheadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedEnvelopeOverheadBytes), "Reserved overhead cannot be negative.");
        }

        if (requestedPayloadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPayloadBytes), "Requested payload cannot be negative.");
        }

        var rawEmbeddableBytes = GetMaximumRawEmbeddableBytes(sampleCount);
        var safeUsableBytes = Math.Max(0L, rawEmbeddableBytes - reservedEnvelopeOverheadBytes);
        var canEmbed = requestedPayloadBytes <= safeUsableBytes;

        return new WavLsbCapacityEstimate(
            MaximumRawEmbeddableBytes: rawEmbeddableBytes,
            SafeUsableBytes: safeUsableBytes,
            ReservedEnvelopeOverheadBytes: reservedEnvelopeOverheadBytes,
            CanEmbedRequestedPayload: canEmbed,
            ConstraintDiagnostics: canEmbed
                ? []
                :
                [
                    $"Requested payload ({requestedPayloadBytes} bytes) exceeds safe usable capacity ({safeUsableBytes} bytes) by {requestedPayloadBytes - safeUsableBytes} byte(s).",
                    $"Safe usable capacity = raw embeddable capacity ({rawEmbeddableBytes} bytes) - reserved envelope overhead ({reservedEnvelopeOverheadBytes} bytes)."
                ]);
    }

    public static long GetMaximumRawEmbeddableBytes(long sampleCount)
    {
        var totalCarrierBytes = sampleCount / 8L;
        return Math.Max(0L, totalCarrierBytes - PayloadLengthPrefixBytes);
    }
}

public sealed record WavLsbCapacityEstimate(
    long MaximumRawEmbeddableBytes,
    long SafeUsableBytes,
    long ReservedEnvelopeOverheadBytes,
    bool CanEmbedRequestedPayload,
    IReadOnlyList<string> ConstraintDiagnostics);
