using StegoForge.Core.Models;

namespace StegoForge.Cli.Output;

internal sealed record EmbedCommandOutput(
    string Command,
    string OutputPath,
    string CarrierFormatId,
    long PayloadSizeBytes,
    long BytesEmbedded,
    OperationDiagnostics Diagnostics) : ICommandOutput
{
    public IReadOnlyList<string> ToTextLines()
        =>
        [
            $"Command: {Command}",
            $"Output path: {OutputPath}",
            $"Carrier format: {CarrierFormatId}",
            $"Payload size (bytes): {PayloadSizeBytes}",
            $"Bytes embedded: {BytesEmbedded}"
        ];
}

internal sealed record ExtractCommandOutput(
    string Command,
    string OutputPath,
    string ResolvedOutputPath,
    string CarrierFormatId,
    long PayloadSizeBytes,
    bool WasCompressed,
    bool WasEncrypted,
    string? OriginalFileName,
    IntegrityVerificationResult IntegrityVerificationResult,
    IReadOnlyList<string> Warnings,
    OperationDiagnostics Diagnostics) : ICommandOutput
{
    public IReadOnlyList<string> ToTextLines()
    {
        var lines = new List<string>
        {
            $"Command: {Command}",
            $"Output path: {OutputPath}",
            $"Resolved output path: {ResolvedOutputPath}",
            $"Carrier format: {CarrierFormatId}",
            $"Extracted payload bytes: {PayloadSizeBytes}",
            $"Integrity verification: {IntegrityVerificationResult}",
            $"Was compressed: {WasCompressed}",
            $"Was encrypted: {WasEncrypted}"
        };

        if (!string.IsNullOrWhiteSpace(OriginalFileName))
        {
            lines.Add($"Original file name: {OriginalFileName}");
        }

        return lines;
    }
}

internal sealed record CapacityCommandOutput(
    string Command,
    string CarrierFormatId,
    long RequestedPayloadSizeBytes,
    long AvailableCapacityBytes,
    long MaximumCapacityBytes,
    long SafeUsableCapacityBytes,
    long EstimatedOverheadBytes,
    bool CanEmbed,
    long RemainingBytes,
    string? FailureReason,
    IReadOnlyList<string> ConstraintBreakdown,
    OperationDiagnostics Diagnostics) : ICommandOutput
{
    public IReadOnlyList<string> ToTextLines()
    {
        var lines = new List<string>
        {
            $"Command: {Command}",
            $"Carrier format: {CarrierFormatId}",
            $"Requested payload size (bytes): {RequestedPayloadSizeBytes}",
            $"Available capacity (bytes): {AvailableCapacityBytes}",
            $"Maximum capacity (bytes): {MaximumCapacityBytes}",
            $"Safe usable capacity (bytes): {SafeUsableCapacityBytes}",
            $"Estimated overhead (bytes): {EstimatedOverheadBytes}",
            $"Can embed: {CanEmbed}",
            $"Remaining bytes: {RemainingBytes}"
        };

        if (!string.IsNullOrWhiteSpace(FailureReason))
        {
            lines.Add($"Failure reason: {FailureReason}");
        }

        if (ConstraintBreakdown.Count > 0)
        {
            lines.Add($"Constraints: {string.Join("; ", ConstraintBreakdown)}");
        }

        return lines;
    }
}

internal sealed record InfoCommandOutput(
    string Command,
    string FormatId,
    CarrierFormatDetails FormatDetails,
    long CarrierSizeBytes,
    long EstimatedCapacityBytes,
    long AvailableCapacityBytes,
    bool EmbeddedDataPresent,
    bool SupportsEncryption,
    bool SupportsCompression,
    PayloadMetadataSummary? PayloadMetadata,
    PayloadProtectionDescriptors ProtectionDescriptors,
    OperationDiagnostics Diagnostics) : ICommandOutput
{
    public IReadOnlyList<string> ToTextLines()
        =>
        [
            $"Command: {Command}",
            $"Format ID: {FormatId}",
            $"Carrier size (bytes): {CarrierSizeBytes}",
            $"Estimated capacity (bytes): {EstimatedCapacityBytes}",
            $"Available capacity (bytes): {AvailableCapacityBytes}",
            $"Embedded data present: {EmbeddedDataPresent}",
            $"Supports encryption: {SupportsEncryption}",
            $"Supports compression: {SupportsCompression}"
        ];
}

internal sealed record VersionCommandOutput(string Command, string Name, string Version) : ICommandOutput
{
    public IReadOnlyList<string> ToTextLines()
        =>
        [
            $"Command: {Command}",
            $"Name: {Name}",
            $"Version: {Version}"
        ];
}
