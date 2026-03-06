namespace StegoForge.Core.Models;

public enum IntegrityVerificationResult
{
    NotApplicable,
    Verified,
    Failed,
    Unknown
}

public sealed record ExtractRequest
{
    public string CarrierPath { get; }
    public string OutputPath { get; }
    public ProcessingOptions ProcessingOptions { get; }
    public PasswordOptions PasswordOptions { get; }
    public EncryptionOptions EncryptionOptions { get; }

    public ExtractRequest(
        string carrierPath,
        string outputPath,
        ProcessingOptions? processingOptions = null,
        PasswordOptions? passwordOptions = null,
        EncryptionOptions? encryptionOptions = null)
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
        EncryptionOptions = encryptionOptions ?? EncryptionOptions.Default;
    }
}

public sealed record ExtractResponse
{
    public string OutputPath { get; }
    public string ResolvedOutputPath { get; }
    public string CarrierFormatId { get; }
    public byte[] Payload { get; }
    public long PayloadSizeBytes { get; }
    public string? OriginalFileName { get; }
    public bool PreservedOriginalFileName { get; }
    public IntegrityVerificationResult IntegrityVerificationResult { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool WasCompressed { get; }
    public bool WasEncrypted { get; }
    public OperationDiagnostics Diagnostics { get; }

    public ExtractResponse(
        string outputPath,
        string resolvedOutputPath,
        string carrierFormatId,
        byte[] payload,
        bool wasCompressed,
        bool wasEncrypted,
        string? originalFileName = null,
        bool preservedOriginalFileName = false,
        IntegrityVerificationResult integrityVerificationResult = IntegrityVerificationResult.Unknown,
        IReadOnlyList<string>? warnings = null,
        OperationDiagnostics? diagnostics = null)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(resolvedOutputPath))
        {
            throw new ArgumentException("Resolved output path is required.", nameof(resolvedOutputPath));
        }

        if (string.IsNullOrWhiteSpace(carrierFormatId))
        {
            throw new ArgumentException("Carrier format identifier is required.", nameof(carrierFormatId));
        }

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (originalFileName is not null && string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be whitespace when provided.", nameof(originalFileName));
        }

        if (!Enum.IsDefined(integrityVerificationResult))
        {
            throw new ArgumentOutOfRangeException(nameof(integrityVerificationResult), "Invalid integrity verification result.");
        }

        OutputPath = outputPath;
        ResolvedOutputPath = resolvedOutputPath;
        CarrierFormatId = carrierFormatId;
        Payload = [.. payload];
        PayloadSizeBytes = payload.LongLength;
        OriginalFileName = originalFileName;
        PreservedOriginalFileName = preservedOriginalFileName;
        IntegrityVerificationResult = integrityVerificationResult;
        Warnings = warnings is null ? [] : [.. warnings];
        WasCompressed = wasCompressed;
        WasEncrypted = wasEncrypted;
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
