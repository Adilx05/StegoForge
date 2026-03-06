using System.IO;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class CompressionProviderContractTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void CompressionRequest_Throws_WhenCompressionLevelOutsideGlobalRange(int compressionLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompressionRequest([1, 2, 3], compressionLevel));
    }

    [Fact]
    public void CompressionRequest_Throws_WhenPayloadNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => new CompressionRequest(null!, 5));
        Assert.Throws<ArgumentException>(() => new CompressionRequest([], 5));
    }

    [Fact]
    public void DecompressionRequest_Throws_WhenPayloadNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => new DecompressionRequest(null!));
        Assert.Throws<ArgumentException>(() => new DecompressionRequest([]));
    }

    [Fact]
    public void CompressionProviderContract_ValidatesProviderLevelRange()
    {
        ICompressionProvider provider = new StubCompressionProvider(minimumCompressionLevel: 2, maximumCompressionLevel: 6);

        CompressionProviderContract.EnsureSupportedLevel(provider, 4);

        Assert.Throws<ArgumentOutOfRangeException>(() => CompressionProviderContract.EnsureSupportedLevel(provider, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CompressionProviderContract.EnsureSupportedLevel(provider, 7));
    }

    [Fact]
    public void CompressionProviderContract_MapsFormatFailures_ToInvalidPayloadException()
    {
        var mapped = CompressionProviderContract.MapDecompressionException(new InvalidDataException("invalid stream"), "extract:carrier.png");

        var invalidPayload = Assert.IsType<InvalidPayloadException>(mapped);
        Assert.Contains("malformed", invalidPayload.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("extract:carrier.png", invalidPayload.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompressionProviderContract_PreservesTypedExceptions()
    {
        var invalidPayload = new InvalidPayloadException("bad payload");
        var internalFailure = new InternalProcessingException("boom");

        Assert.Same(invalidPayload, CompressionProviderContract.MapDecompressionException(invalidPayload));
        Assert.Same(internalFailure, CompressionProviderContract.MapDecompressionException(internalFailure));
    }

    [Fact]
    public void CompressionProviderContract_MapsUnexpectedFailures_ToInternalProcessingException()
    {
        var mapped = CompressionProviderContract.MapDecompressionException(new NotSupportedException("no codec"));

        var internalFailure = Assert.IsType<InternalProcessingException>(mapped);
        Assert.IsType<NotSupportedException>(internalFailure.InnerException);
    }

    [Fact]
    public void CompressionProviderInterface_ExposesMetadataAndRequestResponseMethods()
    {
        var providerType = typeof(ICompressionProvider);

        Assert.NotNull(providerType.GetProperty(nameof(ICompressionProvider.AlgorithmId)));
        Assert.NotNull(providerType.GetProperty(nameof(ICompressionProvider.MinimumCompressionLevel)));
        Assert.NotNull(providerType.GetProperty(nameof(ICompressionProvider.MaximumCompressionLevel)));

        var compressMethod = providerType.GetMethod(nameof(ICompressionProvider.Compress));
        Assert.NotNull(compressMethod);
        Assert.Equal(typeof(CompressionResponse), compressMethod!.ReturnType);
        Assert.Equal(typeof(CompressionRequest), compressMethod.GetParameters().Single().ParameterType);

        var decompressMethod = providerType.GetMethod(nameof(ICompressionProvider.Decompress));
        Assert.NotNull(decompressMethod);
        Assert.Equal(typeof(DecompressionResponse), decompressMethod!.ReturnType);
        Assert.Equal(typeof(DecompressionRequest), decompressMethod.GetParameters().Single().ParameterType);
    }

    private sealed class StubCompressionProvider(int minimumCompressionLevel, int maximumCompressionLevel) : ICompressionProvider
    {
        public string AlgorithmId => "stub";

        public int MinimumCompressionLevel { get; } = minimumCompressionLevel;

        public int MaximumCompressionLevel { get; } = maximumCompressionLevel;

        public CompressionResponse Compress(CompressionRequest request)
        {
            CompressionProviderContract.EnsureSupportedLevel(this, request.CompressionLevel);
            return new CompressionResponse(request.Data, request.CompressionLevel, request.DiagnosticsContext);
        }

        public DecompressionResponse Decompress(DecompressionRequest request) => new(request.CompressedData, request.DiagnosticsContext);
    }
}
