using StegoForge.Formats.Bmp;
using Xunit;

namespace StegoForge.Tests.Unit.Bmp;

public sealed class BmpLsbCapacityCalculatorTests
{
    private readonly BmpLsbCapacityCalculator _calculator = new();

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
