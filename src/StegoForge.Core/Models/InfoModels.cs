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

public sealed record CarrierInfoResponse
{
    public string FormatId { get; }
    public long CarrierSizeBytes { get; }
    public long AvailableCapacityBytes { get; }
    public bool SupportsEncryption { get; }
    public bool SupportsCompression { get; }
    public OperationDiagnostics Diagnostics { get; }

    public CarrierInfoResponse(
        string formatId,
        long carrierSizeBytes,
        long availableCapacityBytes,
        bool supportsEncryption,
        bool supportsCompression,
        OperationDiagnostics? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(formatId))
        {
            throw new ArgumentException("Format identifier is required.", nameof(formatId));
        }

        if (carrierSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(carrierSizeBytes), "Carrier size cannot be negative.");
        }

        if (availableCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableCapacityBytes), "Available capacity cannot be negative.");
        }

        FormatId = formatId;
        CarrierSizeBytes = carrierSizeBytes;
        AvailableCapacityBytes = availableCapacityBytes;
        SupportsEncryption = supportsEncryption;
        SupportsCompression = supportsCompression;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
