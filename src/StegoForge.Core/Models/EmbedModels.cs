namespace StegoForge.Core.Models;

public sealed record EmbedRequest
{
    public string CarrierPath { get; }
    public string OutputPath { get; }
    public byte[] Payload { get; }
    public ProcessingOptions ProcessingOptions { get; }
    public PasswordOptions PasswordOptions { get; }

    public EmbedRequest(
        string carrierPath,
        string outputPath,
        byte[] payload,
        ProcessingOptions? processingOptions = null,
        PasswordOptions? passwordOptions = null)
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
        ProcessingOptions = processingOptions ?? ProcessingOptions.Default;
        PasswordOptions = passwordOptions ?? PasswordOptions.Optional;
    }
}

public sealed record EmbedResponse
{
    public string OutputPath { get; }
    public string CarrierFormatId { get; }
    public long PayloadSizeBytes { get; }
    public long BytesEmbedded { get; }
    public OperationDiagnostics Diagnostics { get; }

    public EmbedResponse(
        string outputPath,
        string carrierFormatId,
        long payloadSizeBytes,
        long bytesEmbedded,
        OperationDiagnostics? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(carrierFormatId))
        {
            throw new ArgumentException("Carrier format identifier is required.", nameof(carrierFormatId));
        }

        if (payloadSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadSizeBytes), "Payload size cannot be negative.");
        }

        if (bytesEmbedded < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesEmbedded), "Embedded bytes cannot be negative.");
        }

        OutputPath = outputPath;
        CarrierFormatId = carrierFormatId;
        PayloadSizeBytes = payloadSizeBytes;
        BytesEmbedded = bytesEmbedded;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
