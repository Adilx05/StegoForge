using StegoForge.Application.Capacity;
using StegoForge.Application.Formats;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Formats.Bmp;
using StegoForge.Formats.Png;
using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class WavCapacityServiceIntegrationTests
{
    private readonly ICapacityService _service = new CapacityService(new CarrierFormatResolver([new PngLsbFormatHandler(), new BmpLsbFormatHandler(), new WavLsbFormatHandler()]));

    [Fact]
    public async Task GetCapacityAsync_WavCarrier_ReturnsExpectedDeterministicCapacityAndOverCapacityDiagnostics()
    {
        var carrierPath = await CreateCarrierFileAsync(sampleCountPerChannel: 4_096, channels: 2);

        try
        {
            var fitResponse = await _service.GetCapacityAsync(new CapacityRequest(carrierPath, payloadSizeBytes: 892));

            Assert.Equal("wav-lsb-v1", fitResponse.CarrierFormatId);
            Assert.Equal(1020, fitResponse.MaximumCapacityBytes);
            Assert.Equal(892, fitResponse.SafeUsableCapacityBytes);
            Assert.Equal(128, fitResponse.EstimatedOverheadBytes);
            Assert.True(fitResponse.CanEmbed);
            Assert.Equal(0, fitResponse.RemainingBytes);
            Assert.Null(fitResponse.FailureReason);
            Assert.Empty(fitResponse.ConstraintBreakdown);

            var overflowResponse = await _service.GetCapacityAsync(new CapacityRequest(carrierPath, payloadSizeBytes: 893));

            Assert.False(overflowResponse.CanEmbed);
            Assert.Equal(-1, overflowResponse.RemainingBytes);
            Assert.Equal("Payload exceeds safe wav-lsb-v1 capacity by 1 byte(s).", overflowResponse.FailureReason);
            Assert.Collection(
                overflowResponse.ConstraintBreakdown,
                first => Assert.Equal("Requested payload (893 bytes) exceeds safe usable capacity (892 bytes) by 1 byte(s).", first),
                second => Assert.Equal("Safe usable capacity = raw embeddable capacity (1020 bytes) - reserved envelope overhead (128 bytes).", second));
        }
        finally
        {
            File.Delete(carrierPath);
        }
    }

    private static async Task<string> CreateCarrierFileAsync(int sampleCountPerChannel, int channels)
    {
        var path = Path.Combine(Path.GetTempPath(), $"stegoforge-capacity-{Guid.NewGuid():N}.wav");
        var carrier = CreatePcm16Wav(sampleCountPerChannel, channels);

        await File.WriteAllBytesAsync(path, carrier);
        return path;
    }

    private static byte[] CreatePcm16Wav(int sampleCountPerChannel, int channels)
    {
        var bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = channels * bytesPerSample;
        var sampleRate = 44_100;
        var byteRate = sampleRate * blockAlign;
        var dataBytes = sampleCountPerChannel * blockAlign;

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

        short sampleValue = 100;
        for (var i = 0; i < sampleCountPerChannel * channels; i++)
        {
            stream.Write(BitConverter.GetBytes(sampleValue));
            sampleValue = (short)(sampleValue + 2);
        }

        return stream.ToArray();
    }
}
