using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Unit.Wav;

public sealed class WavLsbCapacityCalculatorTests
{
    private readonly WavLsbCapacityCalculator _calculator = new();

    [Fact]
    public void CalculateFromSampleCount_TinyCarrier_ReturnsZeroSafeCapacityWithDeterministicDiagnostics()
    {
        var result = _calculator.CalculateFromSampleCount(sampleCount: 1, requestedPayloadBytes: 1);

        Assert.Equal(0, result.MaximumRawEmbeddableBytes);
        Assert.Equal(0, result.SafeUsableBytes);
        Assert.False(result.CanEmbedRequestedPayload);
        Assert.Collection(
            result.ConstraintDiagnostics,
            first => Assert.Equal("Requested payload (1 bytes) exceeds safe usable capacity (0 bytes) by 1 byte(s).", first),
            second => Assert.Equal("Safe usable capacity = raw embeddable capacity (0 bytes) - reserved envelope overhead (128 bytes).", second));
    }

    [Fact]
    public void CalculateFromSampleCount_ExactFitPayload_CanEmbedWithoutDiagnostics()
    {
        var baseline = _calculator.CalculateFromSampleCount(sampleCount: 8_000);

        var result = _calculator.CalculateFromSampleCount(sampleCount: 8_000, requestedPayloadBytes: baseline.SafeUsableBytes);

        Assert.True(result.CanEmbedRequestedPayload);
        Assert.Equal(baseline.SafeUsableBytes, result.SafeUsableBytes);
        Assert.Empty(result.ConstraintDiagnostics);
    }

    [Fact]
    public void CalculateFromSampleCount_OverflowByOneByte_ReturnsDeterministicDiagnostics()
    {
        var baseline = _calculator.CalculateFromSampleCount(sampleCount: 8_000);
        var requestedPayloadBytes = baseline.SafeUsableBytes + 1;

        var result = _calculator.CalculateFromSampleCount(sampleCount: 8_000, requestedPayloadBytes: requestedPayloadBytes);

        Assert.False(result.CanEmbedRequestedPayload);
        Assert.Equal(2, result.ConstraintDiagnostics.Count);
        Assert.Equal($"Requested payload ({requestedPayloadBytes} bytes) exceeds safe usable capacity ({baseline.SafeUsableBytes} bytes) by 1 byte(s).", result.ConstraintDiagnostics[0]);
    }

    [Fact]
    public void CalculateFromPcmLayoutAndDataSize_ChannelAndBitDepthAffectDeterministicRawCapacity()
    {
        var monoLayout = _calculator.CalculateFromPcmLayout(sampleFramesPerChannel: 8_192, channels: 1, bitsPerSample: 16);
        var stereoLayout = _calculator.CalculateFromPcmLayout(sampleFramesPerChannel: 8_192, channels: 2, bitsPerSample: 16);

        Assert.True(stereoLayout.MaximumRawEmbeddableBytes > monoLayout.MaximumRawEmbeddableBytes);

        var sameDataAt16Bit = _calculator.CalculateFromPcmDataSize(dataChunkSizeBytes: 32_768, bitsPerSample: 16);
        var sameDataAt24Bit = _calculator.CalculateFromPcmDataSize(dataChunkSizeBytes: 32_768, bitsPerSample: 24);

        Assert.True(sameDataAt16Bit.MaximumRawEmbeddableBytes > sameDataAt24Bit.MaximumRawEmbeddableBytes);
    }
}
