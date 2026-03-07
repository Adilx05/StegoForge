using StegoForge.Application.Formats;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit.Application;

public sealed class CarrierFormatResolverTests
{
    [Fact]
    public void Resolve_ReturnsSingleMatchingHandler()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var expected = new StubHandler("png-lsb-v1", supports: true);
        var resolver = new CarrierFormatResolver([
            new StubHandler("bmp-lsb-v1", supports: false),
            expected
        ]);

        var resolution = resolver.Resolve(stream, carrierPath: "carrier.png");

        Assert.Same(expected, resolution.Handler);
        Assert.Empty(resolution.Notes);
    }

    [Fact]
    public void Resolve_UsesDeterministicPrecedence_WhenMultipleHandlersMatch()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var preferred = new StubHandler("png-alpha-v1", supports: true);
        var secondary = new StubHandler("png-zeta-v1", supports: true);
        var resolver = new CarrierFormatResolver([
            secondary,
            preferred,
            new StubHandler("bmp-lsb-v1", supports: true)
        ]);

        var resolution = resolver.Resolve(stream, carrierPath: "carrier.png");

        Assert.Same(preferred, resolution.Handler);
        Assert.Contains(resolution.Notes, note => note.Contains("Resolver precedence", StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_ThrowsUnsupportedFormat_WhenNoHandlerMatches()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var resolver = new CarrierFormatResolver([
            new StubHandler("png-lsb-v1", supports: false),
            new StubHandler("bmp-lsb-v1", supports: false)
        ]);

        var exception = Assert.Throws<UnsupportedFormatException>(() => resolver.Resolve(stream, carrierPath: "carrier.png"));

        Assert.Equal(StegoErrorCode.UnsupportedFormat, exception.Code);
        Assert.Contains("unsupported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubHandler(string format, bool supports) : ICarrierFormatHandler
    {
        public string Format => format;

        public bool Supports(Stream carrierStream) => supports;

        public Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task EmbedAsync(Stream carrierStream, Stream outputStream, byte[] payload, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CarrierInfoResponse> GetInfoAsync(Stream carrierStream, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
