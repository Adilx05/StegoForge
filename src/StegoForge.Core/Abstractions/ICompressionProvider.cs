using System.IO;
using StegoForge.Core.Errors;

namespace StegoForge.Core.Abstractions;

/// <summary>
/// Contract for payload compression providers.
/// </summary>
/// <remarks>
/// Implementations must expose stable provider identity and supported compression-level range metadata.
/// Decompression failures caused by malformed payloads or compressed-data mismatches must throw
/// <see cref="InvalidPayloadException"/>. Unexpected implementation failures must be wrapped as
/// <see cref="InternalProcessingException"/> so callers can map failures deterministically.
/// </remarks>
public interface ICompressionProvider
{
    /// <summary>
    /// Stable machine-readable compression algorithm identifier (for example, <c>deflate</c>).
    /// </summary>
    string AlgorithmId { get; }

    /// <summary>
    /// Inclusive minimum compression level accepted by this provider.
    /// </summary>
    int MinimumCompressionLevel { get; }

    /// <summary>
    /// Inclusive maximum compression level accepted by this provider.
    /// </summary>
    int MaximumCompressionLevel { get; }

    CompressionResponse Compress(CompressionRequest request);

    DecompressionResponse Decompress(DecompressionRequest request);
}

public sealed record CompressionRequest
{
    public CompressionRequest(byte[] data, int compressionLevel, string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            throw new ArgumentException("Compression payload cannot be empty.", nameof(data));
        }

        if (compressionLevel is < CompressionProviderContract.GlobalMinimumCompressionLevel or > CompressionProviderContract.GlobalMaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(compressionLevel), compressionLevel, $"Compression level must be between {CompressionProviderContract.GlobalMinimumCompressionLevel} and {CompressionProviderContract.GlobalMaximumCompressionLevel}.");
        }

        Data = [.. data];
        CompressionLevel = compressionLevel;
        DiagnosticsContext = diagnosticsContext;
    }

    public byte[] Data { get; }

    public int CompressionLevel { get; }

    public string? DiagnosticsContext { get; }
}

public sealed record CompressionResponse
{
    public CompressionResponse(byte[] compressedData, int compressionLevelApplied, string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(compressedData);

        if (compressedData.Length == 0)
        {
            throw new ArgumentException("Compressed payload cannot be empty.", nameof(compressedData));
        }

        if (compressionLevelApplied is < CompressionProviderContract.GlobalMinimumCompressionLevel or > CompressionProviderContract.GlobalMaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(compressionLevelApplied), compressionLevelApplied, $"Compression level must be between {CompressionProviderContract.GlobalMinimumCompressionLevel} and {CompressionProviderContract.GlobalMaximumCompressionLevel}.");
        }

        CompressedData = [.. compressedData];
        CompressionLevelApplied = compressionLevelApplied;
        DiagnosticsContext = diagnosticsContext;
    }

    public byte[] CompressedData { get; }

    public int CompressionLevelApplied { get; }

    public string? DiagnosticsContext { get; }
}

public sealed record DecompressionRequest
{
    public DecompressionRequest(byte[] compressedData, string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(compressedData);

        if (compressedData.Length == 0)
        {
            throw new ArgumentException("Compressed payload cannot be empty.", nameof(compressedData));
        }

        CompressedData = [.. compressedData];
        DiagnosticsContext = diagnosticsContext;
    }

    public byte[] CompressedData { get; }

    public string? DiagnosticsContext { get; }
}

public sealed record DecompressionResponse
{
    public DecompressionResponse(byte[] data, string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length == 0)
        {
            throw new ArgumentException("Decompressed payload cannot be empty.", nameof(data));
        }

        Data = [.. data];
        DiagnosticsContext = diagnosticsContext;
    }

    public byte[] Data { get; }

    public string? DiagnosticsContext { get; }
}

public static class CompressionProviderContract
{
    public const int GlobalMinimumCompressionLevel = 0;

    public const int GlobalMaximumCompressionLevel = 9;

    public static void EnsureSupportedLevel(ICompressionProvider provider, int compressionLevel)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (compressionLevel < provider.MinimumCompressionLevel || compressionLevel > provider.MaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(compressionLevel),
                compressionLevel,
                $"Compression level {compressionLevel} is not supported by provider '{provider.AlgorithmId}'. Supported range is {provider.MinimumCompressionLevel}-{provider.MaximumCompressionLevel}.");
        }
    }

    public static Exception MapDecompressionException(Exception exception, string? diagnosticsContext = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is InvalidPayloadException or InternalProcessingException)
        {
            return exception;
        }

        if (exception is InvalidDataException or EndOfStreamException or FormatException)
        {
            var contextSuffix = string.IsNullOrWhiteSpace(diagnosticsContext) ? string.Empty : $" Context: {diagnosticsContext}.";
            return new InvalidPayloadException($"Compressed payload is malformed or does not match the expected compression format.{contextSuffix}");
        }

        return new InternalProcessingException(
            "Compression provider failed unexpectedly during decompression.",
            exception);
    }
}
