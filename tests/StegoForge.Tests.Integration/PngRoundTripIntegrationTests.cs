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

    [Fact]
    public async Task Extract_WhenEmbeddedEnvelopeHeaderIsCorrupted_ThrowsInvalidHeaderException()
    {
        var payload = CreateDeterministicPayload(300, seed: 13);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await CreateMediumRgbCarrierAsync();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        extractedEnvelopeBytes[0] ^= 0xFF; // Corrupt SGF1 magic.

        await using var corruptedCarrier = await EmbedEnvelopeBytesIntoCarrierAsync(stegoCarrier, extractedEnvelopeBytes, PngColorType.Rgb);
        var validatedCorruptedCarrier = await ValidateEmbeddedPngAsync(corruptedCarrier, expectedColorType: PngColorType.Rgb, expectedWidth: 128, expectedHeight: 128);

        var extractedFromCorruptedOutput = await _formatHandler.ExtractAsync(validatedCorruptedCarrier);

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(extractedFromCorruptedOutput));
        Assert.Equal(StegoErrorCode.InvalidHeader, exception.ErrorCode);
    }

    [Fact]
    public async Task Extract_WhenEmbeddedEnvelopePayloadIsTruncated_ThrowsInvalidPayloadException()
    {
        var payload = CreateDeterministicPayload(300, seed: 17);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await CreateMediumRgbCarrierAsync();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        Array.Resize(ref extractedEnvelopeBytes, extractedEnvelopeBytes.Length - 1);

        await using var corruptedCarrier = await EmbedEnvelopeBytesIntoCarrierAsync(stegoCarrier, extractedEnvelopeBytes, PngColorType.Rgb);
        var validatedCorruptedCarrier = await ValidateEmbeddedPngAsync(corruptedCarrier, expectedColorType: PngColorType.Rgb, expectedWidth: 128, expectedHeight: 128);

        var extractedFromCorruptedOutput = await _formatHandler.ExtractAsync(validatedCorruptedCarrier);
        var exception = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(extractedFromCorruptedOutput));
        Assert.Equal(StegoErrorCode.InvalidPayload, exception.ErrorCode);
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
        carrier.Position = 0;
        var (expectedWidth, expectedHeight, expectedColorType) = ReadPngIhdr(carrier);

        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var validatedCarrier = await ValidateEmbeddedPngAsync(stegoCarrier, expectedColorType, expectedWidth, expectedHeight);
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(validatedCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);

        return _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase);
    }



    private static async Task<MemoryStream> EmbedEnvelopeBytesIntoCarrierAsync(
        MemoryStream sourceCarrier,
        byte[] envelopeBytes,
        PngColorType expectedColorType)
    {
        sourceCarrier.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(sourceCarrier);

        var framedPayload = new byte[sizeof(int) + envelopeBytes.Length];
        framedPayload[0] = (byte)((envelopeBytes.Length >> 24) & 0xFF);
        framedPayload[1] = (byte)((envelopeBytes.Length >> 16) & 0xFF);
        framedPayload[2] = (byte)((envelopeBytes.Length >> 8) & 0xFF);
        framedPayload[3] = (byte)(envelopeBytes.Length & 0xFF);
        envelopeBytes.CopyTo(framedPayload, sizeof(int));

        var bitIndex = 0;
        var totalBits = framedPayload.Length * 8;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && bitIndex < totalBits; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length && bitIndex < totalBits; x++)
                {
                    var pixel = row[x];
                    pixel.R = EmbedBit(pixel.R, framedPayload, bitIndex++);
                    if (bitIndex < totalBits)
                    {
                        pixel.G = EmbedBit(pixel.G, framedPayload, bitIndex++);
                    }

                    if (bitIndex < totalBits)
                    {
                        pixel.B = EmbedBit(pixel.B, framedPayload, bitIndex++);
                    }

                    row[x] = pixel;
                }
            }
        });

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output, new PngEncoder
        {
            ColorType = expectedColorType,
            BitDepth = PngBitDepth.Bit8
        });

        output.Position = 0;
        return output;
    }

    private static byte EmbedBit(byte channel, byte[] payload, int bitIndex)
    {
        var sourceByte = payload[bitIndex / 8];
        var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
        return (byte)((channel & 0b1111_1110) | sourceBit);
    }

    private static async Task<MemoryStream> ValidateEmbeddedPngAsync(
        MemoryStream embeddedPng,
        PngColorType expectedColorType,
        int expectedWidth,
        int expectedHeight)
    {
        embeddedPng.Position = 0;
        var outputBytes = embeddedPng.ToArray();

        using (var parserStream = new MemoryStream(outputBytes, writable: false))
        {
            var (width, height, colorType) = ReadPngIhdr(parserStream);
            Assert.Equal(expectedWidth, width);
            Assert.Equal(expectedHeight, height);
            Assert.Equal(expectedColorType, colorType);
        }

        using (var identifyStream = new MemoryStream(outputBytes, writable: false))
        {
            var imageInfo = Image.Identify(identifyStream);
            Assert.NotNull(imageInfo);
            Assert.Equal(expectedWidth, imageInfo!.Width);
            Assert.Equal(expectedHeight, imageInfo.Height);

            var pngMetadata = imageInfo.Metadata.GetPngMetadata();
            Assert.Equal(PngBitDepth.Bit8, pngMetadata.BitDepth);
            Assert.Equal(expectedColorType, pngMetadata.ColorType);
        }

        using (var decodeStream = new MemoryStream(outputBytes, writable: false))
        using (var decodedImage = await Image.LoadAsync<Rgba32>(decodeStream))
        {
            Assert.Equal(expectedWidth, decodedImage.Width);
            Assert.Equal(expectedHeight, decodedImage.Height);
        }

        return new MemoryStream(outputBytes, writable: false);
    }

    private static (int Width, int Height, PngColorType ColorType) ReadPngIhdr(Stream pngStream)
    {
        pngStream.Position = 0;
        using var reader = new BinaryReader(pngStream, System.Text.Encoding.UTF8, leaveOpen: true);

        var signature = reader.ReadBytes(8);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, signature);

        var ihdrLength = ReadBigEndianInt32(reader);
        Assert.Equal(13, ihdrLength);

        var chunkType = reader.ReadBytes(4);
        Assert.Equal("IHDR"u8.ToArray(), chunkType);

        var width = ReadBigEndianInt32(reader);
        var height = ReadBigEndianInt32(reader);
        var bitDepth = reader.ReadByte();
        var colorTypeByte = reader.ReadByte();
        _ = reader.ReadByte(); // compression method
        _ = reader.ReadByte(); // filter method
        _ = reader.ReadByte(); // interlace method
        _ = reader.ReadBytes(4); // CRC

        Assert.Equal(8, bitDepth);

        var colorType = colorTypeByte switch
        {
            2 => PngColorType.Rgb,
            6 => PngColorType.RgbWithAlpha,
            _ => throw new Xunit.Sdk.XunitException($"Unsupported PNG color type byte '{colorTypeByte}'.")
        };

        return (width, height, colorType);
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        Assert.Equal(4, bytes.Length);
        return (bytes[0] << 24)
               | (bytes[1] << 16)
               | (bytes[2] << 8)
               | bytes[3];
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
