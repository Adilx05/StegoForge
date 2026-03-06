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
    public long MaximumCapacityBytes { get; }
    public long SafeUsableCapacityBytes { get; }
    public long EstimatedOverheadBytes { get; }
    public bool CanEmbed { get; }
    public long RemainingBytes { get; }
    public string? FailureReason { get; }
    public IReadOnlyList<string> ConstraintBreakdown { get; }
    public OperationDiagnostics Diagnostics { get; }

    public CapacityResponse(
        string carrierFormatId,
        long requestedPayloadSizeBytes,
        long availableCapacityBytes,
        long maximumCapacityBytes,
        long safeUsableCapacityBytes,
        long estimatedOverheadBytes,
        bool canEmbed,
        long remainingBytes,
        string? failureReason = null,
        IReadOnlyList<string>? constraintBreakdown = null,
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

        if (maximumCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCapacityBytes), "Maximum capacity cannot be negative.");
        }

        if (safeUsableCapacityBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safeUsableCapacityBytes), "Safe usable capacity cannot be negative.");
        }

        if (estimatedOverheadBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedOverheadBytes), "Estimated overhead cannot be negative.");
        }

        if (safeUsableCapacityBytes > maximumCapacityBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(safeUsableCapacityBytes), "Safe usable capacity cannot exceed maximum capacity.");
        }

        if (availableCapacityBytes > maximumCapacityBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(availableCapacityBytes), "Available capacity cannot exceed maximum capacity.");
        }

        if (!canEmbed && string.IsNullOrWhiteSpace(failureReason) && (constraintBreakdown is null || constraintBreakdown.Count == 0))
        {
            throw new ArgumentException("A failure reason or constraints breakdown is required when embed is not possible.", nameof(failureReason));
        }

        if (canEmbed && !string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason must be omitted when embed is possible.", nameof(failureReason));
        }

        CarrierFormatId = carrierFormatId;
        RequestedPayloadSizeBytes = requestedPayloadSizeBytes;
        AvailableCapacityBytes = availableCapacityBytes;
        MaximumCapacityBytes = maximumCapacityBytes;
        SafeUsableCapacityBytes = safeUsableCapacityBytes;
        EstimatedOverheadBytes = estimatedOverheadBytes;
        CanEmbed = canEmbed;
        RemainingBytes = remainingBytes;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason;
        ConstraintBreakdown = constraintBreakdown is null ? [] : [.. constraintBreakdown];
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
