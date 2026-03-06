using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class CoreContractsTests
{
    [Theory]
    [InlineData(typeof(UnsupportedFormatException), StegoErrorCode.UnsupportedFormat)]
    [InlineData(typeof(WrongPasswordException), StegoErrorCode.WrongPassword)]
    [InlineData(typeof(InvalidPayloadException), StegoErrorCode.InvalidPayload)]
    [InlineData(typeof(InvalidHeaderException), StegoErrorCode.InvalidHeader)]
    [InlineData(typeof(OutputExistsException), StegoErrorCode.OutputAlreadyExists)]
    public void ErrorMapper_MapsTypedExceptions(Type exceptionType, StegoErrorCode expectedCode)
    {
        var exception = exceptionType == typeof(OutputExistsException)
            ? new OutputExistsException("output.png")
            : (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        var mapped = StegoErrorMapper.FromException(exception);

        Assert.Equal(expectedCode, mapped.Code);
        Assert.False(string.IsNullOrWhiteSpace(mapped.Message));
    }

    [Fact]
    public void ErrorMapper_MapsInsufficientCapacityException_WithExpectedCode()
    {
        var exception = new InsufficientCapacityException(requiredBytes: 1024, availableBytes: 512);

        var mapped = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InsufficientCapacity, mapped.Code);
        Assert.Contains("Required 1024 bytes", mapped.Message);
    }

    [Fact]
    public void ErrorMapper_MapsUnknownException_ToInternalFailure()
    {
        var mapped = StegoErrorMapper.FromException(new InvalidOperationException("unexpected"));

        Assert.Equal(StegoErrorCode.InternalProcessingFailure, mapped.Code);
        Assert.Equal("unexpected", mapped.Message);
    }

    [Fact]
    public void ProcessingOptions_Defaults_AreStable()
    {
        var options = new ProcessingOptions();

        Assert.Equal(CompressionMode.Automatic, options.CompressionMode);
        Assert.Equal(5, options.CompressionLevel);
        Assert.Equal(EncryptionMode.Optional, options.EncryptionMode);
        Assert.Equal(OverwriteBehavior.Disallow, options.OverwriteBehavior);
        Assert.Equal(VerbosityMode.Normal, options.VerbosityMode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void ProcessingOptions_Throws_WhenCompressionLevelOutOfRange(int compressionLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProcessingOptions(compressionLevel: compressionLevel));
    }

    [Fact]
    public void PasswordOptions_Throws_WhenSourceReferenceWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new PasswordOptions(sourceReference: "  "));
    }

    [Fact]
    public void OperationDiagnostics_Defaults_AreEmptyAndStable()
    {
        var diagnostics = OperationDiagnostics.Empty;

        Assert.Empty(diagnostics.Warnings);
        Assert.Empty(diagnostics.Notes);
        Assert.Equal(TimeSpan.Zero, diagnostics.Duration);
        Assert.Null(diagnostics.AlgorithmIdentifier);
        Assert.Null(diagnostics.ProviderIdentifier);
    }

    [Fact]
    public void OperationDiagnostics_Throws_WhenDurationNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OperationDiagnostics(duration: TimeSpan.FromMilliseconds(-1)));
    }

    [Fact]
    public void EmbedRequest_Throws_WhenPayloadEmpty()
    {
        Assert.Throws<ArgumentException>(() => new EmbedRequest("carrier.png", "out.png", []));
    }

    [Fact]
    public void EmbedRequest_DefaultsOptions_WhenNotProvided()
    {
        var request = new EmbedRequest("carrier.png", "out.png", [1, 2, 3]);

        Assert.Equal(ProcessingOptions.Default, request.ProcessingOptions);
        Assert.Equal(PasswordOptions.Optional, request.PasswordOptions);
    }

    [Fact]
    public void ExtractRequest_Throws_WhenOutputPathMissing()
    {
        Assert.Throws<ArgumentException>(() => new ExtractRequest("carrier.png", " "));
    }

    [Fact]
    public void CapacityRequest_Throws_WhenPayloadSizeNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapacityRequest("carrier.png", -1));
    }

    [Fact]
    public void InfoRequest_DefaultsProcessingOptions_WhenNotProvided()
    {
        var request = new InfoRequest("carrier.png");

        Assert.Equal(ProcessingOptions.Default, request.ProcessingOptions);
    }

    [Fact]
    public void EmbedResponse_Throws_WhenCarrierFormatIdMissing()
    {
        Assert.Throws<ArgumentException>(() => new EmbedResponse("out.png", " ", 100, 120));
    }

    [Fact]
    public void ExtractResponse_ComputesPayloadSize_FromPayloadLength()
    {
        var response = new ExtractResponse("out.bin", "png-lsb-v1", [1, 2, 3, 4], wasCompressed: true, wasEncrypted: false);

        Assert.Equal(4, response.PayloadSizeBytes);
        Assert.Equal(OperationDiagnostics.Empty, response.Diagnostics);
    }

    [Fact]
    public void CapacityResponse_Throws_WhenRequestedPayloadNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CapacityResponse("png-lsb-v1", -1, 2048, canEmbed: false, remainingBytes: 0));
    }

    [Fact]
    public void CarrierInfoResponse_DefaultsDiagnostics_WhenNotProvided()
    {
        var response = new CarrierInfoResponse("png-lsb-v1", 10_000, 4_000, true, true);

        Assert.Equal(OperationDiagnostics.Empty, response.Diagnostics);
    }
}
