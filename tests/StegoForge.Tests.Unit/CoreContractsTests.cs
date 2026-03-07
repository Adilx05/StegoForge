using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class CoreContractsTests
{
    [Theory]
    [InlineData(typeof(FileNotFoundStegoException), StegoErrorCode.FileNotFound)]
    [InlineData(typeof(InvalidArgumentsException), StegoErrorCode.InvalidArguments)]
    [InlineData(typeof(CorruptedDataException), StegoErrorCode.CorruptedData)]
    [InlineData(typeof(UnsupportedFormatException), StegoErrorCode.UnsupportedFormat)]
    [InlineData(typeof(WrongPasswordException), StegoErrorCode.WrongPassword)]
    [InlineData(typeof(InvalidPayloadException), StegoErrorCode.InvalidPayload)]
    [InlineData(typeof(InvalidHeaderException), StegoErrorCode.InvalidHeader)]
    [InlineData(typeof(OutputExistsException), StegoErrorCode.OutputAlreadyExists)]
    [InlineData(typeof(InternalProcessingException), StegoErrorCode.InternalProcessingFailure)]
    public void ErrorMapper_MapsTypedExceptions(Type exceptionType, StegoErrorCode expectedCode)
    {
        Exception exception;
        if (exceptionType == typeof(FileNotFoundStegoException))
        {
            exception = new FileNotFoundStegoException("missing.bin");
        }
        else if (exceptionType == typeof(OutputExistsException))
        {
            exception = new OutputExistsException("output.png");
        }
        else if (exceptionType == typeof(InternalProcessingException))
        {
            exception = new InternalProcessingException("boom");
        }
        else
        {
            exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;
        }

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
        Assert.Equal("An internal processing failure occurred. Retry with verbose diagnostics or contact support.", mapped.Message);
    }

    [Fact]
    public void ErrorMapper_MapsAuthenticationFailureExceptions_ToWrongPasswordCode()
    {
        var mapped = StegoErrorMapper.FromException(new WrongPasswordException("Authentication failed."));

        Assert.Equal(StegoErrorCode.WrongPassword, mapped.Code);
        Assert.Contains("Authentication failed", mapped.Message);
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
        var payload = new byte[] { 1, 2, 3, 4 };
        var response = new ExtractResponse(
            "out.bin",
            "./out.bin",
            "png-lsb-v1",
            payload,
            wasCompressed: true,
            wasEncrypted: false,
            originalFileName: "payload.txt",
            preservedOriginalFileName: true,
            integrityVerificationResult: IntegrityVerificationResult.Verified,
            warnings: ["trailing bytes ignored"]);

        payload[0] = 9;

        Assert.Equal(4, response.PayloadSizeBytes);
        Assert.Equal(1, response.Payload[0]);
        Assert.Equal("payload.txt", response.OriginalFileName);
        Assert.True(response.PreservedOriginalFileName);
        Assert.Equal(IntegrityVerificationResult.Verified, response.IntegrityVerificationResult);
        Assert.Equal(OperationDiagnostics.Empty, response.Diagnostics);
    }

    [Fact]
    public void ExtractResponse_Throws_WhenIntegrityVerificationResultInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExtractResponse(
            "out.bin",
            "./out.bin",
            "png-lsb-v1",
            [1, 2],
            wasCompressed: false,
            wasEncrypted: false,
            integrityVerificationResult: (IntegrityVerificationResult)77));
    }

    [Fact]
    public void CapacityResponse_Throws_WhenRequestedPayloadNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CapacityResponse("png-lsb-v1", -1, 2048, 4096, 3072, 128, canEmbed: false, remainingBytes: 0, failureReason: "overflow"));
    }

    [Fact]
    public void CapacityResponse_RequiresFailureDetails_WhenCannotEmbed()
    {
        Assert.Throws<ArgumentException>(() =>
            new CapacityResponse("png-lsb-v1", 1024, 512, 1024, 768, 64, canEmbed: false, remainingBytes: -512));
    }

    [Fact]
    public void CapacityResponse_PopulatesConstraintBreakdown_WhenProvided()
    {
        var response = new CapacityResponse(
            "png-lsb-v1",
            4096,
            3000,
            4096,
            3500,
            128,
            canEmbed: false,
            remainingBytes: -596,
            failureReason: "Safe capacity exceeded",
            constraintBreakdown: ["Header overhead: 128 bytes", "Safe limit: 3500 bytes"]);

        Assert.Equal("Safe capacity exceeded", response.FailureReason);
        Assert.Collection(
            response.ConstraintBreakdown,
            item => Assert.Equal("Header overhead: 128 bytes", item),
            item => Assert.Equal("Safe limit: 3500 bytes", item));
    }

    [Fact]
    public void CarrierInfoResponse_DefaultsDiagnostics_WhenNotProvided()
    {
        var details = new CarrierFormatDetails("png-lsb-v1", "PNG LSB", "1.0.0");
        var metadata = new PayloadMetadataSummary("secret.txt", 1234, DateTimeOffset.Parse("2025-01-01T00:00:00+00:00"), 1);
        var descriptors = new PayloadProtectionDescriptors("deflate", "aes-gcm-256", "hmac-sha256");
        var response = new CarrierInfoResponse("png-lsb-v1", details, 10_000, 5_000, 4_000, true, true, true, metadata, descriptors);

        Assert.Equal(OperationDiagnostics.Empty, response.Diagnostics);
        Assert.True(response.EmbeddedDataPresent);
        Assert.Equal("secret.txt", response.PayloadMetadata?.OriginalFileName);
        Assert.Equal("aes-gcm-256", response.ProtectionDescriptors.EncryptionDescriptor);
    }

    [Fact]
    public void CarrierInfoResponse_Throws_WhenFormatIdentifiersMismatch()
    {
        Assert.Throws<ArgumentException>(() => new CarrierInfoResponse(
            "png-lsb-v1",
            new CarrierFormatDetails("wav-lsb-v1", "WAV LSB", "1.0.0"),
            100,
            50,
            45,
            embeddedDataPresent: false,
            supportsEncryption: true,
            supportsCompression: true));
    }
}
