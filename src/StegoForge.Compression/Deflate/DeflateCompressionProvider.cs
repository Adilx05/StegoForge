using System.IO.Compression;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;

namespace StegoForge.Compression.Deflate;

public sealed class DeflateCompressionProvider : ICompressionProvider
{
    public const string DeflateAlgorithmId = "deflate";

    public string AlgorithmId => DeflateAlgorithmId;

    public int MinimumCompressionLevel => CompressionProviderContract.GlobalMinimumCompressionLevel;

    public int MaximumCompressionLevel => CompressionProviderContract.GlobalMaximumCompressionLevel;

    public CompressionResponse Compress(CompressionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CompressionProviderContract.EnsureSupportedLevel(this, request.CompressionLevel);

        try
        {
            using var output = new MemoryStream();
            var mappedLevel = MapCompressionLevel(request.CompressionLevel);

            using (var deflateStream = new DeflateStream(output, mappedLevel, leaveOpen: true))
            {
                deflateStream.Write(request.Data, 0, request.Data.Length);
            }

            var compressed = output.ToArray();
            return new CompressionResponse(compressed, request.CompressionLevel, request.DiagnosticsContext);
        }
        catch (Exception exception) when (exception is not InvalidPayloadException and not InternalProcessingException)
        {
            throw new InternalProcessingException("Compression provider failed unexpectedly during compression.", exception);
        }
    }

    public DecompressionResponse Decompress(DecompressionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var input = new MemoryStream(request.CompressedData, writable: false);
            using var deflateStream = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: false);
            using var output = new MemoryStream();

            deflateStream.CopyTo(output);

            var data = output.ToArray();
            return new DecompressionResponse(data, request.DiagnosticsContext);
        }
        catch (Exception exception)
        {
            throw CompressionProviderContract.MapDecompressionException(exception, request.DiagnosticsContext);
        }
    }

    private static CompressionLevel MapCompressionLevel(int compressionLevel)
    {
        return compressionLevel switch
        {
            0 => CompressionLevel.NoCompression,
            <= 3 => CompressionLevel.Fastest,
            <= 8 => CompressionLevel.Optimal,
            _ => CompressionLevel.SmallestSize
        };
    }
}
