namespace StegoForge.Core.Models;

public sealed record InfoRequest
{
    public string CarrierPath { get; }

    public InfoRequest(string carrierPath)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            throw new ArgumentException("Carrier path is required.", nameof(carrierPath));
        }

        CarrierPath = carrierPath;
    }
}

public sealed record CarrierInfoResponse(
    string Format,
    long CarrierSizeBytes,
    long AvailableCapacityBytes,
    bool SupportsEncryption,
    bool SupportsCompression);
