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
    public void EmbedRequest_Throws_WhenPayloadEmpty()
    {
        Assert.Throws<ArgumentException>(() => new EmbedRequest("carrier.png", "out.png", []));
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
}
