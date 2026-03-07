using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Application.Payload;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Crypto.AesGcm;
using StegoForge.Formats.Bmp;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class BmpRoundTripIntegrationTests
{
    private readonly BmpLsbFormatHandler _formatHandler = new();
    private readonly PayloadOrchestrationService _orchestration = new(new DeflateCompressionProvider(), new AesGcmCryptoProvider());
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public async Task EmbedExtract_SmallBmpFixture_ProducesByteIdenticalRoundTrip()
    {
        var payload = CreateDeterministicPayload(256, seed: 101);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);

        var extracted = await ExecuteRoundTripAsync(() => CreateBmpFixtureAsync(48, 48), payload, processing, passphrase: null);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_MediumBmpFixture_ProducesByteIdenticalRoundTrip()
    {
        var payload = CreateHighlyCompressibleDeterministicPayload(4096);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9, encryptionMode: EncryptionMode.Optional);

        var extracted = await ExecuteRoundTripAsync(() => CreateBmpFixtureAsync(160, 160), payload, processing, passphrase: "integration-secret");

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task Extract_CorruptedCarrier_MapsToDeterministicCorruptedDataErrorCode()
    {
        var payload = CreateDeterministicPayload(400, seed: 33);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = await CreateBmpFixtureAsync(64, 64);
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var corruptedCarrier = await CorruptEmbeddedLengthPrefixAsync(stegoCarrier, int.MaxValue);

        var exception = await Assert.ThrowsAsync<CorruptedDataException>(() => ExtractPayloadThroughServicesAsync(corruptedCarrier, processing, passphrase: null));
        var mapped = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.CorruptedData, mapped.Code);
        Assert.Contains("length", exception.Message, StringComparison.OrdinalIgnoreCase);
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

        return await ExtractPayloadThroughServicesAsync(stegoCarrier, processing, passphrase);
    }

    private async Task<byte[]> ExtractPayloadThroughServicesAsync(
        Stream stegoCarrier,
        ProcessingOptions processing,
        string? passphrase)
    {
        stegoCarrier.Position = 0;
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);
        return _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase);
    }

    private static async Task<MemoryStream> CorruptEmbeddedLengthPrefixAsync(MemoryStream stegoCarrier, int corruptedLength)
    {
        stegoCarrier.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(stegoCarrier);

        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, corruptedLength);
        var bitIndex = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height && bitIndex < header.Length * 8; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length && bitIndex < header.Length * 8; x++)
                {
                    var pixel = row[x];
                    pixel.R = EmbedBit(pixel.R, header, bitIndex++);
                    if (bitIndex < header.Length * 8)
                    {
                        pixel.G = EmbedBit(pixel.G, header, bitIndex++);
                    }

                    if (bitIndex < header.Length * 8)
                    {
                        pixel.B = EmbedBit(pixel.B, header, bitIndex++);
                    }

                    row[x] = pixel;
                }
            }
        });

        var output = new MemoryStream();
        await image.SaveAsBmpAsync(output, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
        output.Position = 0;
        return output;
    }

    private static byte EmbedBit(byte channel, ReadOnlySpan<byte> payload, int bitIndex)
    {
        var sourceByte = payload[bitIndex / 8];
        var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
        return (byte)((channel & 0b1111_1110) | sourceBit);
    }

    private static byte[] CreateDeterministicPayload(int length, int seed)
    {
        var bytes = new byte[length];
        var state = seed;
        for (var i = 0; i < bytes.Length; i++)
        {
            state = unchecked((state * 1103515245) + 12345);
            bytes[i] = (byte)(state >> 16);
        }

        return bytes;
    }

    private static byte[] CreateHighlyCompressibleDeterministicPayload(int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)('A' + (i % 3));
        }

        return bytes;
    }

    private static async Task<MemoryStream> CreateBmpFixtureAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)((x * 11 + y * 3) % 256), (byte)((x * 7 + y * 13) % 256), (byte)((x + y * 5) % 256), 255);
            }
        }

        var stream = new MemoryStream();
        await image.SaveAsBmpAsync(stream, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
        stream.Position = 0;
        return stream;
    }
}
