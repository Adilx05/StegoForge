using StegoForge.Application.Payload;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class CompressionOrchestrationIntegrationTests
{
    private readonly PayloadOrchestrationService _service = new(new DeflateCompressionProvider());

    [Fact]
    public void EmbedExtract_CompressionDisabled_SkipsCompressionAndKeepsMetadataConsistent()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Disabled, compressionLevel: 9);

        var envelope = _service.CreateEnvelopeForEmbed(source, options, originalFileName: "payload.txt");
        var extracted = _service.ExtractPayload(envelope);

        Assert.False(envelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("none", envelope.Header.CompressionDescriptor);
        Assert.Equal(source, envelope.Payload);
        Assert.Equal(source, extracted);
    }

    [Fact]
    public void EmbedExtract_CompressionEnabled_AlwaysCompressesAndExtractDecompresses()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);

        var envelope = _service.CreateEnvelopeForEmbed(source, options);
        var extracted = _service.ExtractPayload(envelope);

        Assert.True(envelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("deflate", envelope.Header.CompressionDescriptor);
        Assert.NotEqual(source, envelope.Payload);
        Assert.True(envelope.Payload.Length < source.Length);
        Assert.Equal(source, extracted);
    }

    [Fact]
    public void EmbedExtract_CompressionAutomatic_CompressesOnlyWhenSmaller_WithConsistentMetadataFlag()
    {
        var compressible = CreateHighlyCompressiblePayload();
        var incompressible = CreatePseudoRandomPayload(2048, seed: 12345);
        var options = new ProcessingOptions(compressionMode: CompressionMode.Automatic, compressionLevel: 9);

        var compressedEnvelope = _service.CreateEnvelopeForEmbed(compressible, options);
        var rawEnvelope = _service.CreateEnvelopeForEmbed(incompressible, options);

        Assert.True(compressedEnvelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("deflate", compressedEnvelope.Header.CompressionDescriptor);
        Assert.Equal(compressible, _service.ExtractPayload(compressedEnvelope));

        Assert.False(rawEnvelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("none", rawEnvelope.Header.CompressionDescriptor);
        Assert.Equal(incompressible, _service.ExtractPayload(rawEnvelope));
    }

    [Fact]
    public void ExtractPayload_CompressedEnvelopeWithCorruptedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);
        var validEnvelope = _service.CreateEnvelopeForEmbed(source, options);

        var corruptedPayload = validEnvelope.Payload.ToArray();
        corruptedPayload[0] ^= 0xFF;
        corruptedPayload[^1] ^= 0x7A;

        var corruptedEnvelope = new PayloadEnvelope(
            validEnvelope.Version,
            validEnvelope.Flags,
            validEnvelope.Header,
            corruptedPayload,
            validEnvelope.IntegrityData);

        var exception = Assert.Throws<InvalidPayloadException>(() => _service.ExtractPayload(corruptedEnvelope));
        var mappedError = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InvalidPayload, mappedError.Code);
        Assert.Contains("Compressed payload is malformed", mappedError.Message);
        Assert.Contains("extract:payload", mappedError.Message);
    }

    [Fact]
    public void ExtractPayload_CompressedEnvelopeWithTruncatedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);
        var validEnvelope = _service.CreateEnvelopeForEmbed(source, options);

        var truncatedPayload = validEnvelope.Payload.Take(validEnvelope.Payload.Length / 2).ToArray();
        var truncatedEnvelope = new PayloadEnvelope(
            validEnvelope.Version,
            validEnvelope.Flags,
            validEnvelope.Header,
            truncatedPayload,
            validEnvelope.IntegrityData);

        var exception = Assert.Throws<InvalidPayloadException>(() => _service.ExtractPayload(truncatedEnvelope));
        var mappedError = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InvalidPayload, mappedError.Code);
        Assert.Contains("Compressed payload is malformed", mappedError.Message);
    }

    private static byte[] CreateHighlyCompressiblePayload()
    {
        return Enumerable.Repeat((byte)'A', 4096).ToArray();
    }

    private static byte[] CreatePseudoRandomPayload(int size, int seed)
    {
        var random = new Random(seed);
        var payload = new byte[size];
        random.NextBytes(payload);
        return payload;
    }
}
