namespace StegoForge.Formats.Wav;

public sealed class WavLsbCapacityCalculator
{
    public const int PayloadLengthPrefixBytes = sizeof(int);
    public const int DefaultReservedEnvelopeOverheadBytes = 128;

    public WavLsbCapacityEstimate CalculateFromPcmLayout(
        long sampleFramesPerChannel,
        int channels,
        int bitsPerSample,
        long reservedEnvelopeOverheadBytes = DefaultReservedEnvelopeOverheadBytes,
        long requestedPayloadBytes = 0)
    {
        if (sampleFramesPerChannel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleFramesPerChannel), "Sample frame count cannot be negative.");
        }

        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be greater than zero.");
        }

        if (bitsPerSample <= 0 || (bitsPerSample % 8) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "Bits-per-sample must be a positive multiple of 8.");
        }

        var totalSampleCount = checked(sampleFramesPerChannel * channels);
        return CalculateFromSampleCount(totalSampleCount, reservedEnvelopeOverheadBytes, requestedPayloadBytes);
    }

    public WavLsbCapacityEstimate CalculateFromPcmDataSize(
        long dataChunkSizeBytes,
        int bitsPerSample,
        long reservedEnvelopeOverheadBytes = DefaultReservedEnvelopeOverheadBytes,
        long requestedPayloadBytes = 0)
    {
        if (dataChunkSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataChunkSizeBytes), "Data chunk size cannot be negative.");
        }

        if (bitsPerSample <= 0 || (bitsPerSample % 8) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitsPerSample), "Bits-per-sample must be a positive multiple of 8.");
        }

        var bytesPerSample = bitsPerSample / 8;
        var sampleCount = dataChunkSizeBytes / bytesPerSample;

        return CalculateFromSampleCount(sampleCount, reservedEnvelopeOverheadBytes, requestedPayloadBytes);
    }

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
                : BuildConstraintDiagnostics(requestedPayloadBytes, safeUsableBytes, rawEmbeddableBytes, reservedEnvelopeOverheadBytes));
    }

    public static long GetMaximumRawEmbeddableBytes(long sampleCount)
    {
        var totalCarrierBytes = sampleCount / 8L;
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

public sealed record WavLsbCapacityEstimate(
    long MaximumRawEmbeddableBytes,
    long SafeUsableBytes,
    long ReservedEnvelopeOverheadBytes,
    bool CanEmbedRequestedPayload,
    IReadOnlyList<string> ConstraintDiagnostics);
