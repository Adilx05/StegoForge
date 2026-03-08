using System.Buffers.Binary;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Unit.Wav;

public sealed class WavLsbFormatHandlerTests
{
    private readonly WavLsbFormatHandler _handler = new();

    [Fact]
    public async Task EmbedAndExtract_RoundTripsPayload_AndPreservesNonAudioChunks()
    {
        var payload = "wav-stegoforge"u8.ToArray();
        using var carrier = CreatePcm16Wav(sampleCountPerChannel: 2_000, channels: 2, includeJunkChunk: true);
        var originalBytes = carrier.ToArray();

        using var output = new MemoryStream();
        await _handler.EmbedAsync(carrier, output, payload);

        output.Position = 0;
        var extracted = await _handler.ExtractAsync(output);
        Assert.Equal(payload, extracted);

        var embeddedBytes = output.ToArray();
        Assert.Equal(originalBytes.Length, embeddedBytes.Length);

        var originalJunkOffset = FindChunkDataOffset(originalBytes, "JUNK");
        var embeddedJunkOffset = FindChunkDataOffset(embeddedBytes, "JUNK");
        Assert.True(originalJunkOffset >= 0);
        Assert.Equal(originalJunkOffset, embeddedJunkOffset);

        for (var i = 0; i < 6; i++)
        {
            Assert.Equal(originalBytes[originalJunkOffset + i], embeddedBytes[embeddedJunkOffset + i]);
        }
    }

    [Fact]
    public async Task GetCapacityAsync_MatchesDeterministicSampleLsbCapacity()
    {
        const int samplesPerChannel = 4_096;
        const int channels = 2;
        using var carrier = CreatePcm16Wav(samplesPerChannel, channels);

        var capacity = await _handler.GetCapacityAsync(carrier);

        var totalSamples = samplesPerChannel * channels;
        var expected = (totalSamples / 8) - sizeof(int);
        Assert.Equal(expected, capacity);
    }

    [Fact]
    public async Task EmbedAsync_WhenPayloadExceedsCapacityByOneByte_ThrowsInsufficientCapacity()
    {
        using var carrier = CreatePcm16Wav(sampleCountPerChannel: 512, channels: 1);
        var capacity = await _handler.GetCapacityAsync(carrier);

        carrier.Position = 0;
        using var output = new MemoryStream();
        var payload = new byte[capacity + 1];

        var exception = await Assert.ThrowsAsync<InsufficientCapacityException>(() => _handler.EmbedAsync(carrier, output, payload));

        Assert.Equal(capacity + 1, exception.RequiredBytes);
        Assert.Equal(capacity, exception.AvailableBytes);
    }

    [Fact]
    public void Supports_ReturnsFalse_ForUnsupportedBitDepth()
    {
        using var carrier = CreatePcmWavWithBitDepth(sampleCountPerChannel: 500, channels: 2, bitsPerSample: 8);

        var supported = _handler.Supports(carrier);

        Assert.False(supported);
    }

    [Fact]
    public async Task ExtractAsync_WhenEmbeddedLengthPrefixExceedsCarrierCapacity_ThrowsCorruptedData()
    {
        using var carrier = CreatePcm16Wav(sampleCountPerChannel: 2_048, channels: 1);
        MutateDataChunkSampleLsbWithLengthPrefix(carrier, payloadLength: 100_000);

        carrier.Position = 0;
        var exception = await Assert.ThrowsAsync<CorruptedDataException>(() => _handler.ExtractAsync(carrier));

        Assert.Contains("exceeds carrier capacity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmbedAsync_EnvelopeBeyondConfiguredLimit_ThrowsInvalidArguments()
    {
        var limited = new WavLsbFormatHandler(new ProcessingLimits(maxEnvelopeBytes: 8, maxPayloadBytes: 8, maxHeaderBytes: 64));
        using var carrier = CreatePcm16Wav(sampleCountPerChannel: 2_048, channels: 1);
        using var output = new MemoryStream();

        await Assert.ThrowsAsync<InvalidArgumentsException>(() => limited.EmbedAsync(carrier, output, new byte[16]));
    }

    [Fact]
    public async Task GetCapacityAsync_PreCanceledToken_ThrowsOperationCanceledException()
    {
        using var carrier = CreatePcm16Wav(sampleCountPerChannel: 1_024, channels: 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.GetCapacityAsync(carrier, cts.Token));
    }

    private static MemoryStream CreatePcm16Wav(int sampleCountPerChannel, int channels, bool includeJunkChunk = false)
        => CreatePcmWavWithBitDepth(sampleCountPerChannel, channels, bitsPerSample: 16, includeJunkChunk);

    private static MemoryStream CreatePcmWavWithBitDepth(int sampleCountPerChannel, int channels, int bitsPerSample, bool includeJunkChunk = false)
    {
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = channels * bytesPerSample;
        var sampleRate = 44_100;
        var byteRate = sampleRate * blockAlign;
        var dataBytes = sampleCountPerChannel * blockAlign;

        var fmtChunkSize = 16;
        var junkChunkSize = includeJunkChunk ? 6 : 0;
        var dataChunkSize = dataBytes;

        var riffPayloadSize = 4 + (8 + fmtChunkSize) + (includeJunkChunk ? (8 + junkChunkSize) : 0) + (8 + dataChunkSize);
        var stream = new MemoryStream();

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

        if (includeJunkChunk)
        {
            stream.Write("JUNK"u8);
            stream.Write(BitConverter.GetBytes(junkChunkSize));
            stream.Write([1, 2, 3, 4, 5, 6]);
        }

        stream.Write("data"u8);
        stream.Write(BitConverter.GetBytes(dataChunkSize));

        var sampleValue = (short)-2000;
        for (var i = 0; i < (sampleCountPerChannel * channels); i++)
        {
            stream.Write(BitConverter.GetBytes(sampleValue));
            sampleValue = (short)(sampleValue + 3);
        }

        stream.Position = 0;
        return stream;
    }

    private static void MutateDataChunkSampleLsbWithLengthPrefix(MemoryStream carrier, int payloadLength)
    {
        var bytes = carrier.ToArray();
        var dataOffset = FindChunkDataOffset(bytes, "data");
        var framed = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(framed, payloadLength);

        for (var bitIndex = 0; bitIndex < (sizeof(int) * 8); bitIndex++)
        {
            var sourceByte = framed[bitIndex / 8];
            var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
            var lowByteOffset = dataOffset + (bitIndex * 2);
            bytes[lowByteOffset] = (byte)((bytes[lowByteOffset] & 0b1111_1110) | sourceBit);
        }

        carrier.SetLength(0);
        carrier.Write(bytes);
        carrier.Position = 0;
    }

    private static int FindChunkDataOffset(byte[] wavBytes, string chunkId)
    {
        var cursor = 12;
        while (cursor + 8 <= wavBytes.Length)
        {
            if (wavBytes[cursor] == chunkId[0] && wavBytes[cursor + 1] == chunkId[1] && wavBytes[cursor + 2] == chunkId[2] && wavBytes[cursor + 3] == chunkId[3])
            {
                return cursor + 8;
            }

            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(wavBytes.AsSpan(cursor + 4, 4));
            var chunkEnd = cursor + 8 + (int)chunkSize;
            cursor = chunkEnd + ((chunkSize % 2) == 1 ? 1 : 0);
        }

        return -1;
    }
}
