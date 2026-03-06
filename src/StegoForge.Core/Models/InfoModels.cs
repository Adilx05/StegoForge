namespace StegoForge.Core.Models;

public sealed record InfoRequest
{
    public string CarrierPath { get; }
    public ProcessingOptions ProcessingOptions { get; }

    public InfoRequest(string carrierPath, ProcessingOptions? processingOptions = null)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            throw new ArgumentException("Carrier path is required.", nameof(carrierPath));
        }

        CarrierPath = carrierPath;
        ProcessingOptions = processingOptions ?? ProcessingOptions.Default;
    }
}

public sealed record CarrierFormatDetails
{
    public string FormatId { get; }
    public string DisplayName { get; }
    public string HandlerVersion { get; }

    public CarrierFormatDetails(string formatId, string displayName, string handlerVersion)
    {
        if (string.IsNullOrWhiteSpace(formatId))
        {
            throw new ArgumentException("Format identifier is required.", nameof(formatId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(handlerVersion))
        {
            throw new ArgumentException("Handler version is required.", nameof(handlerVersion));
        }

        FormatId = formatId;
        DisplayName = displayName;
        HandlerVersion = handlerVersion;
    }
}

public sealed record PayloadMetadataSummary
{
    public string? OriginalFileName { get; }
    public long? OriginalSizeBytes { get; }
    public DateTimeOffset? CreatedUtc { get; }
    public int HeaderVersion { get; }

    public PayloadMetadataSummary(string? originalFileName, long? originalSizeBytes, DateTimeOffset? createdUtc, int headerVersion)
    {
        if (originalFileName is not null && string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be whitespace when provided.", nameof(originalFileName));
        }

        if (originalSizeBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalSizeBytes), "Original size cannot be negative.");
        }

        if (headerVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(headerVersion), "Header version cannot be negative.");
        }

        OriginalFileName = originalFileName;
        OriginalSizeBytes = originalSizeBytes;
        CreatedUtc = createdUtc;
        HeaderVersion = headerVersion;
    }
}

public sealed record PayloadProtectionDescriptors
{
    public string CompressionDescriptor { get; }
    public string EncryptionDescriptor { get; }
    public string IntegrityDescriptor { get; }

    public static PayloadProtectionDescriptors None { get; } = new("none", "none", "none");

    public PayloadProtectionDescriptors(string compressionDescriptor, string encryptionDescriptor, string integrityDescriptor)
    {
        if (string.IsNullOrWhiteSpace(compressionDescriptor))
        {
            throw new ArgumentException("Compression descriptor is required.", nameof(compressionDescriptor));
        }

        if (string.IsNullOrWhiteSpace(encryptionDescriptor))
        {
            throw new ArgumentException("Encryption descriptor is required.", nameof(encryptionDescriptor));
        }

        if (string.IsNullOrWhiteSpace(integrityDescriptor))
        {
            throw new ArgumentException("Integrity descriptor is required.", nameof(integrityDescriptor));
        }

        CompressionDescriptor = compressionDescriptor;
        EncryptionDescriptor = encryptionDescriptor;
        IntegrityDescriptor = integrityDescriptor;
    }
}

public sealed record CarrierInfoResponse
{
    public string FormatId { get; }
    public CarrierFormatDetails FormatDetails { get; }
    public long CarrierSizeBytes { get; }
    public long EstimatedCapacityBytes { get; }
    public long AvailableCapacityBytes { get; }
    public bool EmbeddedDataPresent { get; }
    public bool SupportsEncryption { get; }
    public bool SupportsCompression { get; }
    public PayloadMetadataSummary? PayloadMetadata { get; }
    public PayloadProtectionDescriptors ProtectionDescriptors { get; }
    public OperationDiagnostics Diagnostics { get; }

    public CarrierInfoResponse(
        string formatId,
        CarrierFormatDetails formatDetails,
        long carrierSizeBytes,
        long estimatedCapacityBytes,
        long availableCapacityBytes,
        bool embeddedDataPresent,
        bool supportsEncryption,
        bool supportsCompression,
        PayloadMetadataSummary? payloadMetadata = null,
        PayloadProtectionDescriptors? protectionDescriptors = null,
        OperationDiagnostics? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(formatId))
        {
            throw new ArgumentException("Format identifier is required.", nameof(formatId));
        }

        if (formatDetails is null)
        {
            throw new ArgumentNullException(nameof(formatDetails));
        }

        if (!string.Equals(formatId, formatDetails.FormatId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Format identifier must match format details identifier.", nameof(formatDetails));
        }

        if (carrierSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(carrierSizeBytes), "Carrier size cannot be negative.");
        }

        if (estimatedCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCapacityBytes), "Estimated capacity cannot be negative.");
        }

        if (availableCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableCapacityBytes), "Available capacity cannot be negative.");
        }

        FormatId = formatId;
        FormatDetails = formatDetails;
        CarrierSizeBytes = carrierSizeBytes;
        EstimatedCapacityBytes = estimatedCapacityBytes;
        AvailableCapacityBytes = availableCapacityBytes;
        EmbeddedDataPresent = embeddedDataPresent;
        SupportsEncryption = supportsEncryption;
        SupportsCompression = supportsCompression;
        PayloadMetadata = payloadMetadata;
        ProtectionDescriptors = protectionDescriptors ?? PayloadProtectionDescriptors.None;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
