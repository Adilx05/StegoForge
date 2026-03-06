using StegoForge.Compression.Deflate;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using Xunit;

namespace StegoForge.Tests.Unit.Compression;

public sealed class DeflateCompressionProviderTests
{
    private static readonly DeflateCompressionProvider Provider = new();

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(9)]
    public void CompressAndDecompress_RoundTripsBinaryPayload(int compressionLevel)
    {
        var payload = Enumerable.Range(0, 2048).Select(index => (byte)(index % 251)).ToArray();
        var compressed = Provider.Compress(new CompressionRequest(payload, compressionLevel, "embed:roundtrip"));

        var decompressed = Provider.Decompress(new DecompressionRequest(compressed.CompressedData, "extract:roundtrip"));

        Assert.Equal(payload, decompressed.Data);
        Assert.Equal(compressionLevel, compressed.CompressionLevelApplied);
    }

    [Fact]
    public void CompressAndDecompress_HandlesSmallPayloads()
    {
        var payload = new byte[] { 0x01, 0xAB, 0xFF };

        var compressed = Provider.Compress(new CompressionRequest(payload, compressionLevel: 4));
        var decompressed = Provider.Decompress(new DecompressionRequest(compressed.CompressedData));

        Assert.Equal(payload, decompressed.Data);
    }

    [Fact]
    public void CompressAndDecompress_HandlesRandomPayload()
    {
        var random = new Random(424242);
        var payload = new byte[4096];
        random.NextBytes(payload);

        var compressed = Provider.Compress(new CompressionRequest(payload, compressionLevel: 6));
        var decompressed = Provider.Decompress(new DecompressionRequest(compressed.CompressedData));

        Assert.Equal(payload, decompressed.Data);
    }

    [Fact]
    public void Compress_RejectsEmptyPayload()
    {
        Assert.Throws<ArgumentException>(() => new CompressionRequest([], compressionLevel: 5));
    }

    [Fact]
    public void Decompress_ThrowsInvalidPayloadException_ForMalformedCompressedBytes()
    {
        var malformed = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00 };

        var exception = Assert.Throws<InvalidPayloadException>(() => Provider.Decompress(new DecompressionRequest(malformed, "extract:malformed")));

        Assert.Equal(
            "Compressed payload is malformed or does not match the expected compression format. Context: extract:malformed.",
            exception.Message);
    }

    [Fact]
    public void ProviderMetadata_UsesExpectedRangeAndAlgorithmId()
    {
        Assert.Equal("deflate", Provider.AlgorithmId);
        Assert.Equal(0, Provider.MinimumCompressionLevel);
        Assert.Equal(9, Provider.MaximumCompressionLevel);
    }
}
