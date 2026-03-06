namespace StegoForge.Core.Models;

public sealed record CapacityRequest
{
    public string CarrierPath { get; }
    public long PayloadSizeBytes { get; }
    public ProcessingOptions ProcessingOptions { get; }

    public CapacityRequest(string carrierPath, long payloadSizeBytes, ProcessingOptions? processingOptions = null)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            throw new ArgumentException("Carrier path is required.", nameof(carrierPath));
        }

        if (payloadSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadSizeBytes), "Payload size cannot be negative.");
        }

        CarrierPath = carrierPath;
        PayloadSizeBytes = payloadSizeBytes;
        ProcessingOptions = processingOptions ?? ProcessingOptions.Default;
    }
}

public sealed record CapacityResponse
{
    public string CarrierFormatId { get; }
    public long RequestedPayloadSizeBytes { get; }
    public long AvailableCapacityBytes { get; }
    public bool CanEmbed { get; }
    public long RemainingBytes { get; }
    public OperationDiagnostics Diagnostics { get; }

    public CapacityResponse(
        string carrierFormatId,
        long requestedPayloadSizeBytes,
        long availableCapacityBytes,
        bool canEmbed,
        long remainingBytes,
        OperationDiagnostics? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(carrierFormatId))
        {
            throw new ArgumentException("Carrier format identifier is required.", nameof(carrierFormatId));
        }

        if (requestedPayloadSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPayloadSizeBytes), "Requested payload size cannot be negative.");
        }

        if (availableCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableCapacityBytes), "Available capacity cannot be negative.");
        }

        CarrierFormatId = carrierFormatId;
        RequestedPayloadSizeBytes = requestedPayloadSizeBytes;
        AvailableCapacityBytes = availableCapacityBytes;
        CanEmbed = canEmbed;
        RemainingBytes = remainingBytes;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
