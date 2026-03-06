namespace StegoForge.Core.Payload;

/// <summary>
/// Immutable metadata carried by a <see cref="PayloadEnvelope"/>.
/// </summary>
/// <remarks>
/// Serialization invariants for v1:
/// <list type="bullet">
/// <item><description>Header bytes are emitted after a 2-byte little-endian header length prefix.</description></item>
/// <item><description>String fields are UTF-8 and length-prefixed by the serializer.</description></item>
/// <item><description><see cref="OriginalSizeBytes"/> is serialized as 8-byte little-endian signed integer.</description></item>
/// <item><description><see cref="CreatedUtc"/> is serialized as Unix epoch milliseconds in 8-byte little-endian form.</description></item>
/// </list>
/// </remarks>
public sealed record PayloadHeader
{
    public string? OriginalFileName { get; }
    public long OriginalSizeBytes { get; }
    public DateTimeOffset CreatedUtc { get; }
    public string CompressionDescriptor { get; }
    public string EncryptionDescriptor { get; }
    public string? SaltMetadata { get; }
    public string? NonceMetadata { get; }

    public PayloadHeader(
        long originalSizeBytes,
        DateTimeOffset createdUtc,
        string compressionDescriptor,
        string encryptionDescriptor,
        string? originalFileName = null,
        string? saltMetadata = null,
        string? nonceMetadata = null)
    {
        if (originalSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalSizeBytes), "Original size cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(compressionDescriptor))
        {
            throw new ArgumentException("Compression descriptor is required.", nameof(compressionDescriptor));
        }

        if (string.IsNullOrWhiteSpace(encryptionDescriptor))
        {
            throw new ArgumentException("Encryption descriptor is required.", nameof(encryptionDescriptor));
        }

        if (originalFileName is not null && string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be whitespace when provided.", nameof(originalFileName));
        }

        if (saltMetadata is not null && string.IsNullOrWhiteSpace(saltMetadata))
        {
            throw new ArgumentException("Salt metadata cannot be whitespace when provided.", nameof(saltMetadata));
        }

        if (nonceMetadata is not null && string.IsNullOrWhiteSpace(nonceMetadata))
        {
            throw new ArgumentException("Nonce metadata cannot be whitespace when provided.", nameof(nonceMetadata));
        }

        OriginalFileName = originalFileName;
        OriginalSizeBytes = originalSizeBytes;
        CreatedUtc = createdUtc;
        CompressionDescriptor = compressionDescriptor;
        EncryptionDescriptor = encryptionDescriptor;
        SaltMetadata = saltMetadata;
        NonceMetadata = nonceMetadata;
    }
}
