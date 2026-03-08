using StegoForge.Application.Diagnostics;
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
        Assert.StartsWith("ERROR [InvalidPayload]", failure.Message, StringComparison.Ordinal);
        Assert.Contains("operation=unknown", failure.Message, StringComparison.Ordinal);
        Assert.Contains("correlationId=", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFailureFromException_DecompressionUnexpectedFailure_ProducesNonZeroExitCode()
    {
        var exception = new InternalProcessingException("Compression provider failed unexpectedly during decompression.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.NotEqual(0, failure.ExitCode);
        Assert.Equal(1, failure.ExitCode);
        Assert.StartsWith("ERROR [InternalProcessingFailure]", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFailureFromException_WrongPassword_UsesStableExitCodeAndMessage()
    {
        var exception = new WrongPasswordException("Unable to authenticate and decrypt payload.");

        var failure = CliErrorContract.CreateFailureFromException(exception, DiagnosticContext.Create("extract", "png"));

        Assert.Equal(8, failure.ExitCode);
        Assert.Contains("ERROR [WrongPassword] Unable to authenticate and decrypt payload.", failure.Message, StringComparison.Ordinal);
        Assert.Contains("operation=extract", failure.Message, StringComparison.Ordinal);
        Assert.Contains("carrierFormat=png", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFailureFromException_TamperedCiphertextAuthenticationFailure_UsesStableExitCodeAndMessage()
    {
        var exception = new WrongPasswordException("Unable to authenticate and decrypt payload. The computed authentication tag did not match the input authentication tag.");

        var failure = CliErrorContract.CreateFailureFromException(exception);

        Assert.Equal(8, failure.ExitCode);
        Assert.StartsWith("ERROR [WrongPassword] Unable to authenticate and decrypt payload.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFailureFromException_RedactsSensitiveTokens()
    {
        var exception = new InvalidArgumentsException("password=super-secret plaintextPayloadBytes=[1,2,3] derivedKey=00112233445566778899aabbccddeeff");

        var failure = CliErrorContract.CreateFailureFromException(exception, DiagnosticContext.Create("embed", "png"));

        Assert.DoesNotContain("super-secret", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("[1,2,3]", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("00112233445566778899aabbccddeeff", failure.Message, StringComparison.Ordinal);
        Assert.Contains("<redacted>", failure.Message, StringComparison.Ordinal);
    }
}
