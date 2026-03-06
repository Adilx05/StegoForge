using StegoForge.Formats.Png;
using Xunit;

namespace StegoForge.Tests.Unit.Png;

public sealed class PngLsbCapacityCalculatorTests
{
    private readonly PngLsbCapacityCalculator _calculator = new();

    [Fact]
    public void Calculate_TinyImage_ReturnsZeroSafeCapacityAndDeterministicConstraintDiagnostics()
    {
        var result = _calculator.Calculate(width: 1, height: 1, channelsUsed: 3, requestedPayloadBytes: 1);

        Assert.Equal(0, result.MaximumRawEmbeddableBytes);
        Assert.Equal(0, result.SafeUsableBytes);
        Assert.False(result.CanEmbedRequestedPayload);
        Assert.Collection(
            result.ConstraintDiagnostics,
            first => Assert.Equal("Requested payload (1 bytes) exceeds safe usable capacity (0 bytes) by 1 byte(s).", first),
            second => Assert.Equal("Safe usable capacity = raw embeddable capacity (0 bytes) - reserved envelope overhead (128 bytes).", second));
    }

    [Fact]
    public void Calculate_ExactFitPayload_CanEmbedWithZeroRemainingHeadroom()
    {
        const int width = 40;
        const int height = 40;
        const int channels = 3;

        var baseline = _calculator.Calculate(width, height, channels);
        var exactPayload = baseline.SafeUsableBytes;

        var result = _calculator.Calculate(width, height, channels, requestedPayloadBytes: exactPayload);

        Assert.True(result.CanEmbedRequestedPayload);
        Assert.Equal(exactPayload, result.SafeUsableBytes);
        Assert.Empty(result.ConstraintDiagnostics);
    }

    [Fact]
    public void Calculate_OverCapacityByOneByte_ReturnsDeterministicOverflowDiagnostic()
    {
        const int width = 40;
        const int height = 40;
        const int channels = 3;

        var baseline = _calculator.Calculate(width, height, channels);
        var requested = baseline.SafeUsableBytes + 1;

        var result = _calculator.Calculate(width, height, channels, requestedPayloadBytes: requested);

        Assert.False(result.CanEmbedRequestedPayload);
        Assert.Equal(2, result.ConstraintDiagnostics.Count);
        Assert.Equal($"Requested payload ({requested} bytes) exceeds safe usable capacity ({baseline.SafeUsableBytes} bytes) by 1 byte(s).", result.ConstraintDiagnostics[0]);
    }
}
