namespace StegoForge.Core.Models;

public sealed record EmbedRequest
{
    public string CarrierPath { get; }
    public string OutputPath { get; }
    public byte[] Payload { get; }
    public string? Password { get; }
    public bool Compress { get; }

    public EmbedRequest(string carrierPath, string outputPath, byte[] payload, string? password = null, bool compress = false)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            throw new ArgumentException("Carrier path is required.", nameof(carrierPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (payload is null || payload.Length == 0)
        {
            throw new ArgumentException("Payload must contain at least one byte.", nameof(payload));
        }

        CarrierPath = carrierPath;
        OutputPath = outputPath;
        Payload = payload;
        Password = password;
        Compress = compress;
    }
}

public sealed record EmbedResponse(string OutputPath, long BytesEmbedded);
