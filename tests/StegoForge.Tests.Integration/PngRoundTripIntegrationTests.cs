using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Application.Payload;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Crypto.AesGcm;
using StegoForge.Formats.Png;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class PngRoundTripIntegrationTests
{
    private readonly PngLsbFormatHandler _formatHandler = new();
    private readonly PayloadOrchestrationService _orchestration = new(new DeflateCompressionProvider(), new AesGcmCryptoProvider());
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public async Task EmbedExtract_BasicRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateDeterministicPayload(512, seed: 41);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);

        var extracted = await ExecuteRoundTripAsync(CreateMediumRgbCarrierAsync, payload, processing, passphrase: null);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateDeterministicPayload(640, seed: 73);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.Optional);

        var extracted = await ExecuteRoundTripAsync(CreateAlphaCarrierAsync, payload, processing, passphrase: "correct-horse-battery-staple");

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateHighlyCompressibleDeterministicPayload(4096);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9, encryptionMode: EncryptionMode.None);

        var extracted = await ExecuteRoundTripAsync(CreateSmallRgbCarrierAsync, payload, processing, passphrase: null);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_CompressedAndEncryptedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateHighlyCompressibleDeterministicPayload(3000);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9, encryptionMode: EncryptionMode.Optional);

        var extracted = await ExecuteRoundTripAsync(CreateMediumRgbCarrierAsync, payload, processing, passphrase: "s3cr3t");

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task Embed_WhenCarrierHasInsufficientCapacity_ThrowsDeterministicErrorTypeAndCode()
    {
        var payload = CreateDeterministicPayload(3000, seed: 19);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await CreateSmallRgbCarrierAsync();
        await using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InsufficientCapacityException>(() =>
            _formatHandler.EmbedAsync(carrier, output, envelopeBytes));

        var mapped = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.InsufficientCapacity, mapped.Code);
        Assert.True(exception.RequiredBytes > exception.AvailableBytes);
    }

    [Fact]
    public async Task Extract_EncryptedPayload_WithWrongPassword_MapsToWrongPasswordError()
    {
        var payload = CreateDeterministicPayload(768, seed: 121);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.Optional);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: "secret");
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await CreateAlphaCarrierAsync();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        stegoCarrier.Position = 0;
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);

        var exception = Assert.Throws<WrongPasswordException>(() =>
            _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase: "definitely-wrong"));

        var mapped = StegoErrorMapper.FromException(exception);
        Assert.Equal(StegoErrorCode.WrongPassword, mapped.Code);
    }

    private async Task<byte[]> ExecuteRoundTripAsync(
        Func<Task<MemoryStream>> carrierFactory,
        byte[] payload,
        ProcessingOptions processing,
        string? passphrase)
    {
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await carrierFactory();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        stegoCarrier.Position = 0;
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);

        return _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase);
    }

    // Procedurally generated in test code (no external binary fixtures required).
    private static Task<MemoryStream> CreateSmallRgbCarrierAsync() => CreateCarrierAsync(32, 32, withAlpha: false);

    private static Task<MemoryStream> CreateMediumRgbCarrierAsync() => CreateCarrierAsync(128, 128, withAlpha: false);

    private static Task<MemoryStream> CreateAlphaCarrierAsync() => CreateCarrierAsync(96, 96, withAlpha: true);

    private static async Task<MemoryStream> CreateCarrierAsync(int width, int height, bool withAlpha)
    {
        using Image<Rgba32> image = new(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var r = (byte)((x * 17 + y * 3) & 0xFF);
                    var g = (byte)((x * 5 + y * 11) & 0xFF);
                    var b = (byte)((x * 13 + y * 7) & 0xFF);
                    var a = withAlpha ? (byte)((64 + ((x * 9 + y * 5) % 192)) & 0xFF) : byte.MaxValue;
                    row[x] = new Rgba32(r, g, b, a);
                }
            }
        });

        var stream = new MemoryStream();
        var encoder = new PngEncoder
        {
            ColorType = withAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb,
            BitDepth = PngBitDepth.Bit8
        };

        await image.SaveAsPngAsync(stream, encoder);
        stream.Position = 0;
        return stream;
    }

    private static byte[] CreateDeterministicPayload(int length, int seed)
    {
        var random = new Random(seed);
        var payload = new byte[length];
        random.NextBytes(payload);
        return payload;
    }

    private static byte[] CreateHighlyCompressibleDeterministicPayload(int length)
    {
        var payload = new byte[length];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)('A' + (i % 4));
        }

        return payload;
    }
}
