using StegoForge.Formats.Bmp;
using Xunit;

namespace StegoForge.Tests.Unit.Bmp;

public sealed class BmpLsbCapacityCalculatorTests
{
    private readonly BmpLsbCapacityCalculator _calculator = new();

    [Fact]
    public void Calculate_TinyImages_ReportZeroAndNearZeroSafeUsableCapacity()
    {
        var zeroCapacity = _calculator.Calculate(width: 1, height: 1, channelsUsed: 3, requestedPayloadBytes: 1);
        var nearZeroCapacity = _calculator.Calculate(width: 12, height: 9, channelsUsed: 3, reservedEnvelopeOverheadBytes: 35);

        Assert.Equal(0, zeroCapacity.MaximumRawEmbeddableBytes);
        Assert.Equal(0, zeroCapacity.SafeUsableBytes);
        Assert.False(zeroCapacity.CanEmbedRequestedPayload);

        Assert.Equal(36, nearZeroCapacity.MaximumRawEmbeddableBytes);
        Assert.Equal(1, nearZeroCapacity.SafeUsableBytes);
    }

    [Fact]
    public void Calculate_ExactFitPayload_ReturnsEmbeddableWithoutDiagnostics()
    {
        const int width = 40;
        const int height = 40;

        var baseline = _calculator.Calculate(width, height, channelsUsed: 3);
        var result = _calculator.Calculate(width, height, channelsUsed: 3, requestedPayloadBytes: baseline.SafeUsableBytes);

        Assert.True(result.CanEmbedRequestedPayload);
        Assert.Empty(result.ConstraintDiagnostics);
    }

    [Fact]
    public void Calculate_OverCapacityByOneByte_ReturnsDeterministicOverflowDiagnostic()
    {
        const int width = 40;
        const int height = 40;

        var baseline = _calculator.Calculate(width, height, channelsUsed: 3);
        var requested = baseline.SafeUsableBytes + 1;

        var result = _calculator.Calculate(width, height, channelsUsed: 3, requestedPayloadBytes: requested);

        Assert.False(result.CanEmbedRequestedPayload);
        Assert.Equal(2, result.ConstraintDiagnostics.Count);
        Assert.Equal($"Requested payload ({requested} bytes) exceeds safe usable capacity ({baseline.SafeUsableBytes} bytes) by 1 byte(s).", result.ConstraintDiagnostics[0]);
    }
}
