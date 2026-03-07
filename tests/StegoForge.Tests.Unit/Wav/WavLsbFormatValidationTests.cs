using System.Buffers.Binary;
using StegoForge.Core.Errors;
using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Unit.Wav;

public sealed class WavLsbFormatValidationTests
{
    private readonly WavLsbFormatHandler _handler = new();

    [Fact]
    public async Task EmbedAsync_WithNonPcmFormatTag_ThrowsUnsupportedFormat()
    {
        using var carrier = CreateWaveCarrier(formatTag: 3, bitsPerSample: 16, channels: 2, sampleRate: 44_100, sampleCountPerChannel: 1_024);
        using var output = new MemoryStream();

        var ex = await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.EmbedAsync(carrier, output, [1, 2, 3, 4]));

        Assert.Contains("format tag", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_WithUnsupportedBitDepth_ThrowsUnsupportedFormat()
    {
        using var carrier = CreateWaveCarrier(formatTag: 1, bitsPerSample: 8, channels: 1, sampleRate: 22_050, sampleCountPerChannel: 2_048);

        var ex = await Assert.ThrowsAsync<UnsupportedFormatException>(() => _handler.ExtractAsync(carrier));

        Assert.Contains("bit depth", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCapacityAsync_WithTruncatedHeader_ThrowsInvalidHeader()
    {
        using var carrier = new MemoryStream("RIFF"u8.ToArray());

        var ex = await Assert.ThrowsAsync<InvalidHeaderException>(() => _handler.GetCapacityAsync(carrier));

        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCapacityAsync_WithMissingRequiredChunks_ThrowsInvalidHeader()
    {
        using var carrier = CreateWavePreambleOnly();

        var ex = await Assert.ThrowsAsync<InvalidHeaderException>(() => _handler.GetCapacityAsync(carrier));

        Assert.Contains("missing required fmt chunk", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCapacityAsync_WithMissingDataChunk_ThrowsInvalidHeader()
    {
        using var carrier = CreateWaveWithoutDataChunk();

        var ex = await Assert.ThrowsAsync<InvalidHeaderException>(() => _handler.GetCapacityAsync(carrier));

        Assert.Contains("missing required data chunk", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream CreateWavePreambleOnly()
    {
        var stream = new MemoryStream();
        stream.Write("RIFF"u8);
        stream.Write(BitConverter.GetBytes(4));
        stream.Write("WAVE"u8);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateWaveWithoutDataChunk()
    {
        using var carrier = CreateWaveCarrier(formatTag: 1, bitsPerSample: 16, channels: 1, sampleRate: 44_100, sampleCountPerChannel: 32);
        var bytes = carrier.ToArray();

        var dataChunkHeaderOffset = FindChunkOffset(bytes, "data");
        var trimmed = bytes.AsSpan(0, dataChunkHeaderOffset).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(trimmed.AsSpan(4, 4), (uint)(trimmed.Length - 8));

        return new MemoryStream(trimmed);
    }

    private static MemoryStream CreateWaveCarrier(ushort formatTag, ushort bitsPerSample, ushort channels, int sampleRate, int sampleCountPerChannel)
    {
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = (ushort)(channels * bytesPerSample);
        var byteRate = sampleRate * blockAlign;
        var dataChunkSize = sampleCountPerChannel * blockAlign;

        var fmtChunkSize = 16;
        var riffPayloadSize = 4 + (8 + fmtChunkSize) + (8 + dataChunkSize);

        var stream = new MemoryStream();
        stream.Write("RIFF"u8);
        stream.Write(BitConverter.GetBytes(riffPayloadSize));
        stream.Write("WAVE"u8);

        stream.Write("fmt "u8);
        stream.Write(BitConverter.GetBytes(fmtChunkSize));
        stream.Write(BitConverter.GetBytes(formatTag));
        stream.Write(BitConverter.GetBytes(channels));
        stream.Write(BitConverter.GetBytes(sampleRate));
        stream.Write(BitConverter.GetBytes(byteRate));
        stream.Write(BitConverter.GetBytes(blockAlign));
        stream.Write(BitConverter.GetBytes(bitsPerSample));

        stream.Write("data"u8);
        stream.Write(BitConverter.GetBytes(dataChunkSize));
        for (var i = 0; i < dataChunkSize; i++)
        {
            stream.WriteByte((byte)(i % byte.MaxValue));
        }

        stream.Position = 0;
        return stream;
    }

    private static int FindChunkOffset(byte[] bytes, string chunkId)
    {
        var cursor = 12;
        while (cursor + 8 <= bytes.Length)
        {
            if (bytes[cursor] == chunkId[0] && bytes[cursor + 1] == chunkId[1] && bytes[cursor + 2] == chunkId[2] && bytes[cursor + 3] == chunkId[3])
            {
                return cursor;
            }

            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(cursor + 4, 4));
            cursor += 8 + (int)chunkSize + ((chunkSize % 2) == 1 ? 1 : 0);
        }

        return -1;
    }
}
