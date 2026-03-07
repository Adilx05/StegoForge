using StegoForge.Application.Payload;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;
using StegoForge.Crypto.AesGcm;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class CompressionOrchestrationIntegrationTests
{
    private readonly PayloadOrchestrationService _service = new(new DeflateCompressionProvider(), new AesGcmCryptoProvider());
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public void EmbedExtract_CompressionDisabled_SkipsCompressionAndKeepsMetadataConsistent()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Disabled, compressionLevel: 9);

        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: null, originalFileName: "payload.txt");
        var extracted = _service.ExtractPayload(envelope, options, PasswordOptions.Optional, passphrase: null);

        Assert.False(envelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("none", envelope.Header.CompressionDescriptor);
        Assert.Equal(source, envelope.Payload);
        Assert.Equal(source, extracted);
    }

    [Fact]
    public void EmbedExtract_CompressionEnabled_AlwaysCompressesAndExtractDecompresses()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);

        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: null);
        var extracted = _service.ExtractPayload(envelope, options, PasswordOptions.Optional, passphrase: null);

        Assert.True(envelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("deflate", envelope.Header.CompressionDescriptor);
        Assert.NotEqual(source, envelope.Payload);
        Assert.True(envelope.Payload.Length < source.Length);
        Assert.Equal(source, extracted);
    }

    [Fact]
    public void EmbedExtract_CompressionAutomatic_CompressesOnlyWhenSmaller_WithConsistentMetadataFlag()
    {
        var compressible = CreateHighlyCompressiblePayload();
        var incompressible = CreatePseudoRandomPayload(2048, seed: 12345);
        var options = new ProcessingOptions(compressionMode: CompressionMode.Automatic, compressionLevel: 9);

        var compressedEnvelope = _service.CreateEnvelopeForEmbed(compressible, options, PasswordOptions.Optional, passphrase: null);
        var rawEnvelope = _service.CreateEnvelopeForEmbed(incompressible, options, PasswordOptions.Optional, passphrase: null);

        Assert.True(compressedEnvelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("deflate", compressedEnvelope.Header.CompressionDescriptor);
        Assert.Equal(compressible, _service.ExtractPayload(compressedEnvelope, options, PasswordOptions.Optional, passphrase: null));

        Assert.False(rawEnvelope.Flags.HasFlag(EnvelopeFlags.Compressed));
        Assert.Equal("none", rawEnvelope.Header.CompressionDescriptor);
        Assert.Equal(incompressible, _service.ExtractPayload(rawEnvelope, options, PasswordOptions.Optional, passphrase: null));
    }

    [Fact]
    public void EmbedExtract_EncryptionOptional_WithoutPassword_RemainsUnencrypted()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Optional);

        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: null);

        Assert.False(envelope.Flags.HasFlag(EnvelopeFlags.Encrypted));
        Assert.Equal("none", envelope.Header.EncryptionDescriptor);
        Assert.Equal(source, _service.ExtractPayload(envelope, options, PasswordOptions.Optional, passphrase: null));
    }

    [Fact]
    public void EmbedExtract_EncryptionEnabledWithPassword_EncryptsAndDecrypts()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Optional);

        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: "secret");
        var extracted = _service.ExtractPayload(envelope, options, PasswordOptions.Optional, passphrase: "secret");

        Assert.True(envelope.Flags.HasFlag(EnvelopeFlags.Encrypted));
        Assert.Contains("enc:aes-256-gcm", envelope.Header.EncryptionDescriptor);
        Assert.NotNull(envelope.Header.SaltMetadata);
        Assert.NotNull(envelope.Header.NonceMetadata);
        Assert.NotEmpty(envelope.IntegrityData);
        Assert.Equal(source, extracted);
    }

    [Fact]
    public void Embed_EncryptionRequiredWithoutPassword_Throws()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Required);

        var ex = Assert.Throws<WrongPasswordException>(() =>
            _service.CreateEnvelopeForEmbed(source, options, new PasswordOptions(PasswordRequirement.Required), passphrase: null));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_EncryptedEnvelopeWithoutRequiredPassword_Throws()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Optional);
        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: "secret");

        Assert.Throws<WrongPasswordException>(() =>
            _service.ExtractPayload(envelope, options, new PasswordOptions(PasswordRequirement.Required), passphrase: null));
    }

    [Fact]
    public void EmbedExtract_EncryptedPayload_WithWrongPassword_MapsToWrongPasswordCode()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Optional);
        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: "secret");

        var exception = Assert.Throws<WrongPasswordException>(() =>
            _service.ExtractPayload(envelope, options, PasswordOptions.Optional, passphrase: "not-secret"));

        Assert.Equal(StegoErrorCode.WrongPassword, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public void EmbedExtract_EncryptedPayload_WithTamperedEnvelopeBytes_FailsWithDeterministicTypedError()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(encryptionMode: EncryptionMode.Optional);
        var envelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: "secret");
        var envelopeBytes = _serializer.Serialize(envelope);

        var tamperedEnvelopeBytes = envelopeBytes.ToArray();
        tamperedEnvelopeBytes[^3] ^= 0x5A;

        var tamperedEnvelope = _serializer.Deserialize(tamperedEnvelopeBytes);

        var exception = Assert.Throws<WrongPasswordException>(() =>
            _service.ExtractPayload(tamperedEnvelope, options, PasswordOptions.Optional, passphrase: "secret"));

        var mappedError = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.WrongPassword, mappedError.Code);
        Assert.Contains("authenticate", mappedError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractPayload_CompressedEnvelopeWithCorruptedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);
        var validEnvelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: null);

        var corruptedPayload = validEnvelope.Payload.ToArray();
        corruptedPayload[0] ^= 0xFF;
        corruptedPayload[^1] ^= 0x7A;

        var corruptedEnvelope = new PayloadEnvelope(
            validEnvelope.Version,
            validEnvelope.Flags,
            validEnvelope.Header,
            corruptedPayload,
            validEnvelope.IntegrityData);

        var exception = Assert.Throws<InvalidPayloadException>(() => _service.ExtractPayload(corruptedEnvelope, options, PasswordOptions.Optional, passphrase: null));
        var mappedError = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InvalidPayload, mappedError.Code);
        Assert.Contains("Compressed payload is malformed", mappedError.Message);
        Assert.Contains("extract:payload", mappedError.Message);
    }

    [Fact]
    public void ExtractPayload_CompressedEnvelopeWithTruncatedPayload_ThrowsInvalidPayloadAndMapsToInvalidPayloadCode()
    {
        var source = CreateHighlyCompressiblePayload();
        var options = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9);
        var validEnvelope = _service.CreateEnvelopeForEmbed(source, options, PasswordOptions.Optional, passphrase: null);

        var truncatedPayload = validEnvelope.Payload.Take(validEnvelope.Payload.Length / 2).ToArray();
        var truncatedEnvelope = new PayloadEnvelope(
            validEnvelope.Version,
            validEnvelope.Flags,
            validEnvelope.Header,
            truncatedPayload,
            validEnvelope.IntegrityData);

        var exception = Assert.Throws<InvalidPayloadException>(() => _service.ExtractPayload(truncatedEnvelope, options, PasswordOptions.Optional, passphrase: null));
        var mappedError = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InvalidPayload, mappedError.Code);
        Assert.Contains("Compressed payload is malformed", mappedError.Message);
    }

    [Fact]
    public void CreateEnvelopeForEmbed_OversizePayload_ThrowsInvalidArgumentsDeterministically()
    {
        var limitedService = new PayloadOrchestrationService(
            new DeflateCompressionProvider(),
            new AesGcmCryptoProvider(),
            new ProcessingLimits(maxPayloadBytes: 64, maxHeaderBytes: 1024, maxEnvelopeBytes: 4096));

        var exception = Assert.Throws<InvalidArgumentsException>(() =>
            limitedService.CreateEnvelopeForEmbed(new byte[128], ProcessingOptions.Default, PasswordOptions.Optional, passphrase: null));

        Assert.Equal(StegoErrorCode.InvalidArguments, StegoErrorMapper.FromException(exception).Code);
    }

    [Fact]
    public void CreateEnvelopeForEmbed_PreCanceledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            _service.CreateEnvelopeForEmbed(new byte[] { 1, 2, 3 }, ProcessingOptions.Default, PasswordOptions.Optional, passphrase: null, cancellationToken: cts.Token));
    }

    private static byte[] CreateHighlyCompressiblePayload()
    {
        return Enumerable.Repeat((byte)'A', 4096).ToArray();
    }

    private static byte[] CreatePseudoRandomPayload(int size, int seed)
    {
        var random = new Random(seed);
        var payload = new byte[size];
        random.NextBytes(payload);
        return payload;
    }
}
