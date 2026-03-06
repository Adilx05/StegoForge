namespace StegoForge.Core.Models;

public sealed record ExtractRequest
{
    public string CarrierPath { get; }
    public string OutputPath { get; }
    public ProcessingOptions ProcessingOptions { get; }
    public PasswordOptions PasswordOptions { get; }

    public ExtractRequest(
        string carrierPath,
        string outputPath,
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

        CarrierPath = carrierPath;
        OutputPath = outputPath;
        ProcessingOptions = processingOptions ?? ProcessingOptions.Default;
        PasswordOptions = passwordOptions ?? PasswordOptions.Optional;
    }
}

public sealed record ExtractResponse
{
    public string OutputPath { get; }
    public string CarrierFormatId { get; }
    public byte[] Payload { get; }
    public long PayloadSizeBytes { get; }
    public bool WasCompressed { get; }
    public bool WasEncrypted { get; }
    public OperationDiagnostics Diagnostics { get; }

    public ExtractResponse(
        string outputPath,
        string carrierFormatId,
        byte[] payload,
        bool wasCompressed,
        bool wasEncrypted,
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

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        OutputPath = outputPath;
        CarrierFormatId = carrierFormatId;
        Payload = payload;
        PayloadSizeBytes = payload.LongLength;
        WasCompressed = wasCompressed;
        WasEncrypted = wasEncrypted;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
