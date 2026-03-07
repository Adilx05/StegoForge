using System.Buffers.Binary;
using StegoForge.Application.Payload;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Crypto.AesGcm;
using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class WavRoundTripIntegrationTests
{
    private readonly WavLsbFormatHandler _formatHandler = new();
    private readonly PayloadOrchestrationService _orchestration = new(new DeflateCompressionProvider(), new AesGcmCryptoProvider());
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public async Task EmbedExtract_BaselineRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateDeterministicPayload(512, seed: 211);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);

        var extracted = await ExecuteRoundTripAsync(CreateMonoCarrier, payload, processing, passphrase: null);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_CompressedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateHighlyCompressibleDeterministicPayload(4_000);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9, encryptionMode: EncryptionMode.None);

        var extracted = await ExecuteRoundTripAsync(CreateMonoCarrier, payload, processing, passphrase: null);

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_EncryptedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateDeterministicPayload(640, seed: 409);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.Optional);

        var extracted = await ExecuteRoundTripAsync(CreateStereoCarrier, payload, processing, passphrase: "wav-integration-secret");

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task EmbedExtract_EncryptedAndCompressedRoundTrip_ProducesByteIdenticalPayload()
    {
        var payload = CreateHighlyCompressibleDeterministicPayload(7_000);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Enabled, compressionLevel: 9, encryptionMode: EncryptionMode.Optional);

        var extracted = await ExecuteRoundTripAsync(CreateStereoCarrier, payload, processing, passphrase: "wav-combined-secret");

        Assert.Equal(payload, extracted);
    }

    [Fact]
    public async Task Embed_WhenCarrierHasInsufficientCapacity_ThrowsDeterministicErrorTypeAndCode()
    {
        var payload = CreateDeterministicPayload(6_000, seed: 77);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = CreateMonoCarrier();
        await using var output = new MemoryStream();

        var exception = await Assert.ThrowsAsync<InsufficientCapacityException>(() =>
            _formatHandler.EmbedAsync(carrier, output, envelopeBytes));

        var mapped = StegoErrorMapper.FromException(exception);
        Assert.Equal(StegoErrorCode.InsufficientCapacity, mapped.Code);
        Assert.True(exception.RequiredBytes > exception.AvailableBytes);
    }

    [Fact]
    public async Task Extract_WhenEmbeddedLengthHeaderIsCorrupted_MapsToCorruptedDataError()
    {
        var payload = CreateDeterministicPayload(400, seed: 71);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.None);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: null);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = CreateStereoCarrier();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var corruptedCarrier = CorruptEmbeddedLengthPrefix(stegoCarrier, int.MaxValue);

        var exception = await Assert.ThrowsAsync<CorruptedDataException>(() => _formatHandler.ExtractAsync(corruptedCarrier));
        var mapped = StegoErrorMapper.FromException(exception);

        Assert.Equal(StegoErrorCode.CorruptedData, mapped.Code);
        Assert.Contains("length", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Extract_EncryptedPayload_WithWrongPassword_MapsToWrongPasswordError()
    {
        var payload = CreateDeterministicPayload(768, seed: 501);
        var processing = new ProcessingOptions(compressionMode: CompressionMode.Disabled, encryptionMode: EncryptionMode.Optional);
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase: "wav-correct-password");
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = CreateStereoCarrier();
        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        stegoCarrier.Position = 0;
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(stegoCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);

        var exception = Assert.Throws<WrongPasswordException>(() =>
            _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase: "wav-wrong-password"));

        var mapped = StegoErrorMapper.FromException(exception);
        Assert.Equal(StegoErrorCode.WrongPassword, mapped.Code);
    }

    private async Task<byte[]> ExecuteRoundTripAsync(Func<MemoryStream> carrierFactory, byte[] payload, ProcessingOptions processing, string? passphrase)
    {
        var envelope = _orchestration.CreateEnvelopeForEmbed(payload, processing, PasswordOptions.Optional, passphrase);
        var envelopeBytes = _serializer.Serialize(envelope);

        await using var carrier = carrierFactory();
        var expectedMetadata = ReadWavMetadata(carrier);

        await using var stegoCarrier = new MemoryStream();
        await _formatHandler.EmbedAsync(carrier, stegoCarrier, envelopeBytes);

        var validatedStegoCarrier = ValidateEmbeddedWavMetadata(stegoCarrier, expectedMetadata);
        var extractedEnvelopeBytes = await _formatHandler.ExtractAsync(validatedStegoCarrier);
        var extractedEnvelope = _serializer.Deserialize(extractedEnvelopeBytes);

        return _orchestration.ExtractPayload(extractedEnvelope, processing, PasswordOptions.Optional, passphrase);
    }

    private static MemoryStream ValidateEmbeddedWavMetadata(MemoryStream stegoCarrier, WavMetadata expected)
    {
        var output = new MemoryStream(stegoCarrier.ToArray(), writable: false);
        var actual = ReadWavMetadata(output);

        Assert.Equal(expected.SampleRate, actual.SampleRate);
        Assert.Equal(expected.Channels, actual.Channels);
        Assert.Equal(expected.BitsPerSample, actual.BitsPerSample);
        Assert.True(actual.DataChunkSize > 0);

        output.Position = 0;
        return output;
    }

    private static MemoryStream CorruptEmbeddedLengthPrefix(MemoryStream stegoCarrier, int corruptedLength)
    {
        var bytes = stegoCarrier.ToArray();
        var metadata = ReadWavMetadata(new MemoryStream(bytes, writable: false));

        Span<byte> headerBytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(headerBytes, corruptedLength);

        var bitCount = headerBytes.Length * 8;
        for (var bitIndex = 0; bitIndex < bitCount; bitIndex++)
        {
            var sourceByte = headerBytes[bitIndex / 8];
            var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
            var sampleLowByteOffset = checked((int)(metadata.DataChunkOffset + (bitIndex * 2L)));
            bytes[sampleLowByteOffset] = (byte)((bytes[sampleLowByteOffset] & 0b1111_1110) | sourceBit);
        }

        return new MemoryStream(bytes, writable: false);
    }



    private static MemoryStream CreateMonoCarrier()
        => CreatePcm16Wav(sampleRate: 44_100, channels: 1, sampleFramesPerChannel: 44_100, leftFrequencyHz: 440f, rightFrequencyHz: 0f);

    private static MemoryStream CreateStereoCarrier()
        => CreatePcm16Wav(sampleRate: 48_000, channels: 2, sampleFramesPerChannel: 48_000, leftFrequencyHz: 523.25f, rightFrequencyHz: 659.25f);

    private static MemoryStream CreatePcm16Wav(int sampleRate, int channels, int sampleFramesPerChannel, float leftFrequencyHz, float rightFrequencyHz)
    {
        const short amplitude = 12_000;
        var bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = channels * bytesPerSample;
        var byteRate = sampleRate * blockAlign;
        var dataBytes = sampleFramesPerChannel * blockAlign;

        var fmtChunkSize = 16;
        var riffPayloadSize = 4 + (8 + fmtChunkSize) + (8 + dataBytes);

        using var stream = new MemoryStream();
        stream.Write("RIFF"u8);
        stream.Write(BitConverter.GetBytes(riffPayloadSize));
        stream.Write("WAVE"u8);

        stream.Write("fmt "u8);
        stream.Write(BitConverter.GetBytes(fmtChunkSize));
        stream.Write(BitConverter.GetBytes((ushort)1));
        stream.Write(BitConverter.GetBytes((ushort)channels));
        stream.Write(BitConverter.GetBytes(sampleRate));
        stream.Write(BitConverter.GetBytes(byteRate));
        stream.Write(BitConverter.GetBytes((ushort)blockAlign));
        stream.Write(BitConverter.GetBytes((ushort)bitsPerSample));

        stream.Write("data"u8);
        stream.Write(BitConverter.GetBytes(dataBytes));

        for (var frame = 0; frame < sampleFramesPerChannel; frame++)
        {
            var t = frame / (double)sampleRate;
            var leftSample = (short)(amplitude * Math.Sin(2 * Math.PI * leftFrequencyHz * t));

            stream.Write(BitConverter.GetBytes(leftSample));

            if (channels == 2)
            {
                var rightSample = (short)(amplitude * Math.Sin(2 * Math.PI * rightFrequencyHz * t));
                stream.Write(BitConverter.GetBytes(rightSample));
            }
        }

        return new MemoryStream(stream.ToArray());
    }

    private static WavMetadata ReadWavMetadata(Stream wavStream)
    {
        wavStream.Position = 0;
        using var copy = new MemoryStream();
        wavStream.CopyTo(copy);
        var bytes = copy.ToArray();

        if (bytes.Length < 12 || bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F' || bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E')
        {
            throw new InvalidDataException("Expected RIFF/WAVE file.");
        }

        var cursor = 12;
        ushort channels = 0;
        uint sampleRate = 0;
        ushort bitsPerSample = 0;
        int dataOffset = -1;
        int dataSize = 0;

        while (cursor + 8 <= bytes.Length)
        {
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(cursor + 4, 4));
            var chunkDataOffset = cursor + 8;
            var chunkDataEnd = chunkDataOffset + (int)chunkSize;
            if (chunkDataEnd > bytes.Length)
            {
                throw new InvalidDataException("WAV chunk extends beyond file length.");
            }

            if (bytes[cursor] == 'f' && bytes[cursor + 1] == 'm' && bytes[cursor + 2] == 't' && bytes[cursor + 3] == ' ')
            {
                if (chunkSize < 16)
                {
                    throw new InvalidDataException("WAV fmt chunk must be at least 16 bytes.");
                }

                var span = bytes.AsSpan(chunkDataOffset, (int)chunkSize);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(span[2..4]);
                sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(span[4..8]);
                bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(span[14..16]);
            }
            else if (bytes[cursor] == 'd' && bytes[cursor + 1] == 'a' && bytes[cursor + 2] == 't' && bytes[cursor + 3] == 'a' && dataOffset < 0)
            {
                dataOffset = chunkDataOffset;
                dataSize = (int)chunkSize;
            }

            var padding = chunkSize % 2;
            cursor = chunkDataEnd + (int)padding;
        }

        if (dataOffset < 0 || channels == 0 || sampleRate == 0 || bitsPerSample == 0)
        {
            throw new InvalidDataException("Required WAV metadata is missing.");
        }

        return new WavMetadata(sampleRate, channels, bitsPerSample, dataOffset, dataSize);
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

    private sealed record WavMetadata(uint SampleRate, ushort Channels, ushort BitsPerSample, int DataChunkOffset, int DataChunkSize);
}
