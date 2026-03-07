using StegoForge.Application.Validation;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit.Application;

public sealed class OperationPolicyValidatorTests
{
    [Fact]
    public void ValidateEmbedRequest_AllowsValidCombination()
    {
        using var fixture = new TempFileFixture();
        var validator = new OperationPolicyValidator();

        var request = new EmbedRequest(
            carrierPath: fixture.CarrierPath,
            outputPath: fixture.OutputPath,
            payload: [1, 2, 3],
            processingOptions: new ProcessingOptions(
                compressionMode: CompressionMode.Enabled,
                compressionLevel: 6,
                encryptionMode: EncryptionMode.Required,
                overwriteBehavior: OverwriteBehavior.Allow),
            passwordOptions: new PasswordOptions(
                requirement: PasswordRequirement.Required,
                sourceHint: PasswordSourceHint.Prompt,
                sourceReference: "secret"));

        validator.ValidateEmbedRequest(request);
    }

    [Fact]
    public void ValidateEmbedRequest_ThrowsInvalidArguments_WhenEncryptionRequiredAndPasswordSourceMissing()
    {
        using var fixture = new TempFileFixture();
        var validator = new OperationPolicyValidator();

        var request = new EmbedRequest(
            carrierPath: fixture.CarrierPath,
            outputPath: fixture.OutputPath,
            payload: [1],
            processingOptions: new ProcessingOptions(
                encryptionMode: EncryptionMode.Required,
                overwriteBehavior: OverwriteBehavior.Allow),
            passwordOptions: new PasswordOptions(
                requirement: PasswordRequirement.Required,
                sourceHint: PasswordSourceHint.None,
                sourceReference: null));

        var exception = Assert.Throws<InvalidArgumentsException>(() => validator.ValidateEmbedRequest(request));
        Assert.Equal(StegoErrorCode.InvalidArguments, exception.Code);
        Assert.Contains("requires a password source reference", exception.Message);
    }

    [Fact]
    public void ValidateEmbedRequest_ThrowsInvalidArguments_WhenCompressionModeDisabledButCompressionLevelNotZero()
    {
        using var fixture = new TempFileFixture();
        var validator = new OperationPolicyValidator();

        var request = new EmbedRequest(
            carrierPath: fixture.CarrierPath,
            outputPath: fixture.OutputPath,
            payload: [1],
            processingOptions: new ProcessingOptions(
                compressionMode: CompressionMode.Disabled,
                compressionLevel: 5,
                overwriteBehavior: OverwriteBehavior.Allow));

        var exception = Assert.Throws<InvalidArgumentsException>(() => validator.ValidateEmbedRequest(request));
        Assert.Equal(StegoErrorCode.InvalidArguments, exception.Code);
        Assert.Contains("Compression mode 'Disabled' requires compression level 0", exception.Message);
    }

    [Fact]
    public void ValidateEmbedRequest_ThrowsOutputExists_WhenOutputExistsAndOverwriteDisallowed()
    {
        using var fixture = new TempFileFixture(createOutputFile: true);
        var validator = new OperationPolicyValidator();

        var request = new EmbedRequest(
            carrierPath: fixture.CarrierPath,
            outputPath: fixture.OutputPath,
            payload: [1],
            processingOptions: new ProcessingOptions(overwriteBehavior: OverwriteBehavior.Disallow));

        var exception = Assert.Throws<OutputExistsException>(() => validator.ValidateEmbedRequest(request));
        Assert.Equal(StegoErrorCode.OutputAlreadyExists, exception.Code);
    }



    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string OutputPath { get; }

        public TempFileFixture(bool createOutputFile = false)
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-validator-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            CarrierPath = Path.Combine(RootPath, "carrier.bin");
            OutputPath = Path.Combine(RootPath, "output.bin");

            File.WriteAllBytes(CarrierPath, [0, 1, 2]);
            if (createOutputFile)
            {
                File.WriteAllBytes(OutputPath, [9]);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
