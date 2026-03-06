using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class CryptoContractTests
{
    [Fact]
    public void CryptoProvider_UsesStructuredRequestResponseContracts()
    {
        var encryptMethod = typeof(ICryptoProvider).GetMethod(nameof(ICryptoProvider.Encrypt));
        Assert.NotNull(encryptMethod);
        Assert.Equal(typeof(CryptoEncryptResult), encryptMethod!.ReturnType);
        Assert.Single(encryptMethod.GetParameters());
        Assert.Equal(typeof(CryptoEncryptRequest), encryptMethod.GetParameters()[0].ParameterType);

        var decryptMethod = typeof(ICryptoProvider).GetMethod(nameof(ICryptoProvider.Decrypt));
        Assert.NotNull(decryptMethod);
        Assert.Equal(typeof(CryptoDecryptResult), decryptMethod!.ReturnType);
        Assert.Single(decryptMethod.GetParameters());
        Assert.Equal(typeof(CryptoDecryptRequest), decryptMethod.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void ProcessingOptions_Defaults_IncludeEncryptionDefaults()
    {
        var options = new ProcessingOptions();

        Assert.Equal(EncryptionOptions.Default, options.EncryptionOptions);
        Assert.Equal(KdfOptions.Default, options.EncryptionOptions.KdfOptions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void KdfOptions_Throws_WhenIterationCountInvalid(int iterationCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KdfOptions(iterationCount: iterationCount));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(1025)]
    public void KdfOptions_Throws_WhenSaltLengthOutOfRange(int saltLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KdfOptions(saltLengthBytes: saltLength));
    }

    [Fact]
    public void CryptoEncryptRequest_Throws_WhenNoPassphraseOrKeyMaterial()
    {
        Assert.Throws<ArgumentException>(() => new CryptoEncryptRequest([1, 2, 3]));
    }

    [Fact]
    public void CryptoDecryptRequest_Throws_WhenExpectedAlgorithmIdentifiersAreWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            new CryptoDecryptRequest([1], [2], [3], [4], passphrase: "secret", expectedEncryptionAlgorithmId: " "));

        Assert.Throws<ArgumentException>(() =>
            new CryptoDecryptRequest([1], [2], [3], [4], passphrase: "secret", expectedKdfAlgorithmId: "\t"));
    }

    [Fact]
    public void CryptoResultModels_CopyBuffersAndPreserveDiagnosticsContract()
    {
        var ciphertext = new byte[] { 10, 11, 12 };
        var encryptResult = new CryptoEncryptResult(ciphertext, [1], [2], [3], "aes-256-gcm", "pbkdf2-sha256");

        ciphertext[0] = 99;
        Assert.Equal((byte)10, encryptResult.Ciphertext[0]);

        var diagnostics = new OperationDiagnostics(notes: ["round-trip"]);
        var decryptResult = new CryptoDecryptResult([1, 2, 3], diagnostics);

        Assert.Equal(diagnostics, decryptResult.Diagnostics);
        Assert.NotNull(decryptResult.Plaintext);
    }
}
