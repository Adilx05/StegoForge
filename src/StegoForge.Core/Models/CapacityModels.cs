namespace StegoForge.Core.Models;

public sealed record CapacityRequest
{
    public string CarrierPath { get; }
    public long PayloadSizeBytes { get; }

    public CapacityRequest(string carrierPath, long payloadSizeBytes)
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
    }
}

public sealed record CapacityResponse(long AvailableCapacityBytes, bool CanEmbed, long RemainingBytes);
