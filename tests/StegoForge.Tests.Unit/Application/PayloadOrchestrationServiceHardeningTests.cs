using StegoForge.Application.Payload;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit.Application;

public sealed class PayloadOrchestrationServiceHardeningTests
{
    [Fact]
    public void CreateEnvelopeForEmbed_OversizedPayloadRejectedBeforeCompression()
    {
        var compression = new CountingCompressionProvider();
        var crypto = new PassthroughCryptoProvider();
        var service = new PayloadOrchestrationService(
            compression,
            crypto,
            new ProcessingLimits(maxPayloadBytes: 32, maxHeaderBytes: 1024, maxEnvelopeBytes: 4096));

        var payload = new byte[128];
        var ex = Assert.Throws<InvalidArgumentsException>(() =>
            service.CreateEnvelopeForEmbed(payload, ProcessingOptions.Default, PasswordOptions.Optional, passphrase: null));

        Assert.Equal(0, compression.CompressCalls);
        Assert.Equal(0, crypto.EncryptCalls);
        Assert.Contains("configured limit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateEnvelopeForEmbed_PreCanceledToken_ThrowsOperationCanceledException()
    {
        var service = new PayloadOrchestrationService(
            new CountingCompressionProvider(),
            new PassthroughCryptoProvider(),
            ProcessingLimits.SafeDefaults);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            service.CreateEnvelopeForEmbed(new byte[] { 1, 2, 3 }, ProcessingOptions.Default, PasswordOptions.Optional, passphrase: null, cancellationToken: cts.Token));
    }

    private sealed class CountingCompressionProvider : ICompressionProvider
    {
        public string AlgorithmId => "counting";
        public int MinimumCompressionLevel => 0;
        public int MaximumCompressionLevel => 9;
        public int CompressCalls { get; private set; }

        public CompressionResponse Compress(CompressionRequest request)
        {
            CompressCalls++;
            return new CompressionResponse(request.Data, request.CompressionLevel, request.DiagnosticsContext);
        }

        public DecompressionResponse Decompress(DecompressionRequest request) => new(request.CompressedData, request.DiagnosticsContext);
    }

    private sealed class PassthroughCryptoProvider : ICryptoProvider
    {
        public int EncryptCalls { get; private set; }

        public CryptoEncryptResult Encrypt(CryptoEncryptRequest request)
        {
            EncryptCalls++;
            return new CryptoEncryptResult(request.Plaintext, new byte[12], new byte[8], new byte[16], "none", "none");
        }

        public CryptoDecryptResult Decrypt(CryptoDecryptRequest request)
            => new(request.Ciphertext);
    }
}
