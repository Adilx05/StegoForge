using StegoForge.Cli;
using StegoForge.Core.Errors;
using Xunit;

namespace StegoForge.Tests.Cli;

public sealed class BaselineCliTests
{
    [Fact]
    public void CreateFailureFromException_DecompressionCorruption_MapsToInvalidPayloadDeterministically()
    {
        var exception = new InvalidPayloadException("Compressed payload is malformed or does not match the expected compression format. Context: extract:payload.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.Equal(6, failure.ExitCode);
        Assert.Equal(
            "ERROR [InvalidPayload] Compressed payload is malformed or does not match the expected compression format. Context: extract:payload.",
            failure.Message);
    }

    [Fact]
    public void CreateFailureFromException_DecompressionUnexpectedFailure_ProducesNonZeroExitCode()
    {
        var exception = new InternalProcessingException("Compression provider failed unexpectedly during decompression.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.NotEqual(0, failure.ExitCode);
        Assert.Equal(1, failure.ExitCode);
        Assert.Equal(
            "ERROR [InternalProcessingFailure] Compression provider failed unexpectedly during decompression.",
            failure.Message);
    }

    [Fact]
    public void CreateFailureFromException_WrongPassword_UsesStableExitCodeAndMessage()
    {
        var exception = new WrongPasswordException("Unable to authenticate and decrypt payload.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.Equal(8, failure.ExitCode);
        Assert.Equal("ERROR [WrongPassword] Unable to authenticate and decrypt payload.", failure.Message);
    }

    [Fact]
    public void CreateFailureFromException_TamperedCiphertextAuthenticationFailure_UsesStableExitCodeAndMessage()
    {
        var exception = new WrongPasswordException("Unable to authenticate and decrypt payload. The computed authentication tag did not match the input authentication tag.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.Equal(8, failure.ExitCode);
        Assert.StartsWith("ERROR [WrongPassword] Unable to authenticate and decrypt payload.", failure.Message, StringComparison.Ordinal);
    }
}
