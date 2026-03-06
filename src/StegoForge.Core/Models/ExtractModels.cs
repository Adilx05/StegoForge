namespace StegoForge.Core.Models;

public sealed record ExtractRequest
{
    public string CarrierPath { get; }
    public string OutputPath { get; }
    public string? Password { get; }

    public ExtractRequest(string carrierPath, string outputPath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            throw new ArgumentException("Carrier path is required.", nameof(carrierPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        CarrierPath = carrierPath;
        OutputPath = outputPath;
        Password = password;
    }
}

public sealed record ExtractResponse(string OutputPath, byte[] Payload);
