using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;

namespace StegoForge.Application.Payload;

/// <summary>
/// Coordinates payload preprocessing for embed/extract orchestration.
/// </summary>
public sealed class PayloadOrchestrationService(ICompressionProvider compressionProvider)
{
    private const string NoCompressionDescriptor = "none";
    private const string NoEncryptionDescriptor = "none";

    public PayloadEnvelope CreateEnvelopeForEmbed(byte[] payload, ProcessingOptions processingOptions, string? originalFileName = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(processingOptions);

        if (payload.Length == 0)
        {
            throw new ArgumentException("Payload must contain at least one byte.", nameof(payload));
        }

        var (processedPayload, wasCompressed, descriptor) = ApplyCompressionPolicy(payload, processingOptions);

        var flags = wasCompressed ? EnvelopeFlags.Compressed : EnvelopeFlags.None;
        if (!string.IsNullOrWhiteSpace(originalFileName))
        {
            flags |= EnvelopeFlags.MetadataPresent;
        }

        var header = new PayloadHeader(
            originalSizeBytes: payload.LongLength,
            createdUtc: DateTimeOffset.UtcNow,
            compressionDescriptor: descriptor,
            encryptionDescriptor: NoEncryptionDescriptor,
            originalFileName: string.IsNullOrWhiteSpace(originalFileName) ? null : originalFileName);

        return new PayloadEnvelope(
            version: EnvelopeVersion.V1,
            flags: flags,
            header: header,
            payload: processedPayload,
            integrityData: []);
    }

    public byte[] ExtractPayload(PayloadEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var isCompressed = envelope.Flags.HasFlag(EnvelopeFlags.Compressed);
        if (!isCompressed)
        {
            return [.. envelope.Payload];
        }

        return compressionProvider.Decompress(new DecompressionRequest(envelope.Payload, "extract:payload")).Data;
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) ApplyCompressionPolicy(byte[] payload, ProcessingOptions processingOptions)
    {
        CompressionProviderContract.EnsureSupportedLevel(compressionProvider, processingOptions.CompressionLevel);

        return processingOptions.CompressionMode switch
        {
            CompressionMode.Disabled => ([.. payload], false, NoCompressionDescriptor),
            CompressionMode.Enabled => Compress(payload, processingOptions.CompressionLevel),
            CompressionMode.Automatic => CompressWhenSmaller(payload, processingOptions.CompressionLevel),
            _ => throw new ArgumentOutOfRangeException(nameof(processingOptions), $"Unsupported compression mode '{processingOptions.CompressionMode}'.")
        };
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) CompressWhenSmaller(byte[] payload, int level)
    {
        var compressed = compressionProvider.Compress(new CompressionRequest(payload, level, "embed:automatic"));
        if (compressed.CompressedData.Length >= payload.Length)
        {
            return ([.. payload], false, NoCompressionDescriptor);
        }

        return (compressed.CompressedData, true, compressionProvider.AlgorithmId);
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) Compress(byte[] payload, int level)
    {
        var compressed = compressionProvider.Compress(new CompressionRequest(payload, level, "embed:enabled"));
        return (compressed.CompressedData, true, compressionProvider.AlgorithmId);
    }
}
