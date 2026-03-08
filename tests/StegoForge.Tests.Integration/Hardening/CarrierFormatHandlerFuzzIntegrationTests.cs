using System.Buffers.Binary;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StegoForge.Core.Errors;
using StegoForge.Formats.Bmp;
using StegoForge.Formats.Png;
using StegoForge.Formats.Wav;
using Xunit;

namespace StegoForge.Tests.Integration.Hardening;

public sealed class CarrierFormatHandlerFuzzIntegrationTests
{
    private const int Seed = 889_331;
    private const string HardeningArtifactsDirEnvironmentVariable = "STEGOFORGE_HARDENING_ARTIFACTS_DIR";

    [Fact]
    [Trait("Category", "Hardening")]
    [Trait("Campaign", "Fuzz-Bounded")]
    public async Task PngHandler_FuzzedCarrierCorruption_MapsDeterministicExceptions()
    {
        var handler = new PngLsbFormatHandler();

        await using var malformedCarrier = new MemoryStream(CreateRandomBytes(length: 64, seedOffset: 1));
        var malformedException = await Assert.ThrowsAsync<UnsupportedFormatException>(() => handler.GetCapacityAsync(malformedCarrier));
        AssertMappedCode(malformedException, StegoErrorCode.UnsupportedFormat);

        await using var signatureCorruptedCarrier = await CreatePngCarrierAsync(8, 8, oddLsbChannels: false);
        var corruptedPngBytes = signatureCorruptedCarrier.ToArray();
        corruptedPngBytes[0] ^= 0xFF;
        await using var corruptedPng = new MemoryStream(corruptedPngBytes, writable: false);
        var signatureException = await Assert.ThrowsAsync<UnsupportedFormatException>(() => handler.ExtractAsync(corruptedPng));
        AssertMappedCode(signatureException, StegoErrorCode.UnsupportedFormat);

        await using var corruptedLengthCarrier = await CreatePngCarrierAsync(8, 8, oddLsbChannels: true);
        var corruptedLengthException = await Assert.ThrowsAsync<CorruptedDataException>(() => handler.ExtractAsync(corruptedLengthCarrier));
        AssertMappedCode(corruptedLengthException, StegoErrorCode.CorruptedData);
    }

    [Fact]
    [Trait("Category", "Hardening")]
    [Trait("Campaign", "Fuzz-Bounded")]
    public async Task BmpHandler_FuzzedCarrierCorruption_MapsDeterministicExceptions()
    {
        var handler = new BmpLsbFormatHandler();

        await using var malformedCarrier = new MemoryStream(CreateRandomBytes(length: 54, seedOffset: 2));
        var malformedException = await Assert.ThrowsAsync<UnsupportedFormatException>(() => handler.GetCapacityAsync(malformedCarrier));
        AssertMappedCode(malformedException, StegoErrorCode.UnsupportedFormat);

        await using var headerCorruptedCarrier = await CreateBmpCarrierAsync(8, 8, oddLsbChannels: false);
        var corruptedBmpBytes = headerCorruptedCarrier.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(corruptedBmpBytes.AsSpan(14, 4), 12U);
        await using var corruptedBmp = new MemoryStream(corruptedBmpBytes, writable: false);
        var headerException = await Assert.ThrowsAsync<InvalidHeaderException>(() => handler.GetCapacityAsync(corruptedBmp));
        AssertMappedCode(headerException, StegoErrorCode.InvalidHeader);

        await using var corruptedLengthCarrier = await CreateBmpCarrierAsync(8, 8, oddLsbChannels: true);
        var corruptedLengthException = await Assert.ThrowsAsync<CorruptedDataException>(() => handler.ExtractAsync(corruptedLengthCarrier));
        AssertMappedCode(corruptedLengthException, StegoErrorCode.CorruptedData);
    }

    [Fact]
    [Trait("Category", "Hardening")]
    [Trait("Campaign", "Fuzz-Bounded")]
    public async Task WavHandler_FuzzedCarrierCorruption_MapsDeterministicExceptions()
    {
        var handler = new WavLsbFormatHandler();

        await using var malformedCarrier = new MemoryStream(CreateRandomBytes(length: 8, seedOffset: 3));
        var malformedException = await Assert.ThrowsAsync<InvalidHeaderException>(() => handler.GetCapacityAsync(malformedCarrier));
        AssertMappedCode(malformedException, StegoErrorCode.InvalidHeader);

        await using var headerCorruptedCarrier = CreateWavCarrier(sampleFramesPerChannel: 128, forceOddSampleLowBytes: false);
        var corruptedWavBytes = headerCorruptedCarrier.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(corruptedWavBytes.AsSpan(4, 4), uint.MaxValue);
        await using var corruptedWav = new MemoryStream(corruptedWavBytes, writable: false);
        var headerException = await Assert.ThrowsAsync<InvalidHeaderException>(() => handler.GetCapacityAsync(corruptedWav));
        AssertMappedCode(headerException, StegoErrorCode.InvalidHeader);

        await using var corruptedLengthCarrier = CreateWavCarrier(sampleFramesPerChannel: 256, forceOddSampleLowBytes: true);
        var corruptedLengthException = await Assert.ThrowsAsync<CorruptedDataException>(() => handler.ExtractAsync(corruptedLengthCarrier));
        AssertMappedCode(corruptedLengthException, StegoErrorCode.CorruptedData);
    }

    [Fact]
    [Trait("Category", "Hardening")]
    [Trait("Campaign", "Fuzz-Full")]
    [Trait("Execution", "Nightly")]
    public async Task CarrierHandlers_FuzzedCarrierCorruption_LongRunningCampaign_MapsDeterministicExceptions()
    {
        for (var run = 0; run < 6; run++)
        {
            try
            {
                var png = new PngLsbFormatHandler();
                await using var malformedPngCarrier = new MemoryStream(CreateRandomBytes(length: 64 + run, seedOffset: 101 + run));
                var pngException = await Assert.ThrowsAsync<UnsupportedFormatException>(() => png.GetCapacityAsync(malformedPngCarrier));
                AssertMappedCode(pngException, StegoErrorCode.UnsupportedFormat);

                var bmp = new BmpLsbFormatHandler();
                await using var malformedBmpCarrier = new MemoryStream(CreateRandomBytes(length: 54 + run, seedOffset: 201 + run));
                var bmpException = await Assert.ThrowsAsync<UnsupportedFormatException>(() => bmp.GetCapacityAsync(malformedBmpCarrier));
                AssertMappedCode(bmpException, StegoErrorCode.UnsupportedFormat);

                var wav = new WavLsbFormatHandler();
                await using var malformedWavCarrier = new MemoryStream(CreateRandomBytes(length: 8 + run, seedOffset: 301 + run));
                var wavException = await Assert.ThrowsAnyAsync<StegoForgeException>(() => wav.GetCapacityAsync(malformedWavCarrier));
                Assert.True(
                    wavException is InvalidHeaderException or UnsupportedFormatException,
                    $"Unexpected exception type during long-running wav fuzz: {wavException.GetType().FullName}");

                var expectedCode = wavException is InvalidHeaderException
                    ? StegoErrorCode.InvalidHeader
                    : StegoErrorCode.UnsupportedFormat;
                AssertMappedCode(wavException, expectedCode);
            }
            catch (Exception exception)
            {
                PersistReproArtifact(
                    "carrier-handler-fuzz-full",
                    Seed + run,
                    CreateRandomBytes(length: 96 + run, seedOffset: 401 + run),
                    exception);
                throw;
            }
        }
    }

    private static void AssertMappedCode(StegoForgeException exception, StegoErrorCode expectedCode)
    {
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedCode, StegoErrorMapper.FromException(exception).Code);
    }

    private static byte[] CreateRandomBytes(int length, int seedOffset)
    {
        var random = new Random(Seed + seedOffset);
        var buffer = new byte[length];
        random.NextBytes(buffer);
        return buffer;
    }

    private static async Task<MemoryStream> CreatePngCarrierAsync(int width, int height, bool oddLsbChannels)
    {
        using Image<Rgba32> image = new(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = oddLsbChannels
                        ? new Rgba32(0xFF, 0xFF, 0xFF, 0xFF)
                        : new Rgba32((byte)(x * 11), (byte)(y * 13), (byte)(x + y), 0xFF);
                }
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, new PngEncoder { ColorType = PngColorType.RgbWithAlpha, BitDepth = PngBitDepth.Bit8 });
        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateBmpCarrierAsync(int width, int height, bool oddLsbChannels)
    {
        using Image<Rgba32> image = new(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = oddLsbChannels
                        ? new Rgba32(0xFF, 0xFF, 0xFF, 0xFF)
                        : new Rgba32((byte)(x * 9), (byte)(y * 7), (byte)(x + y), 0xFF);
                }
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsBmpAsync(stream, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24, SupportTransparency = false });
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateWavCarrier(int sampleFramesPerChannel, bool forceOddSampleLowBytes)
    {
        const int sampleRate = 44_100;
        const ushort channels = 1;
        const ushort bitsPerSample = 16;
        const ushort audioFormatPcm = 1;
        const ushort blockAlign = channels * (bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        var dataSize = sampleFramesPerChannel * blockAlign;
        var riffPayloadSize = 4 + (8 + 16) + (8 + dataSize);

        using var stream = new MemoryStream();
        stream.Write("RIFF"u8);
        stream.Write(BitConverter.GetBytes(riffPayloadSize));
        stream.Write("WAVE"u8);

        stream.Write("fmt "u8);
        stream.Write(BitConverter.GetBytes(16));
        stream.Write(BitConverter.GetBytes(audioFormatPcm));
        stream.Write(BitConverter.GetBytes(channels));
        stream.Write(BitConverter.GetBytes(sampleRate));
        stream.Write(BitConverter.GetBytes(byteRate));
        stream.Write(BitConverter.GetBytes(blockAlign));
        stream.Write(BitConverter.GetBytes(bitsPerSample));

        stream.Write("data"u8);
        stream.Write(BitConverter.GetBytes(dataSize));

        for (var i = 0; i < sampleFramesPerChannel; i++)
        {
            short sample = forceOddSampleLowBytes ? (short)0x7FFF : (short)(i % short.MaxValue);
            stream.Write(BitConverter.GetBytes(sample));
        }

        return new MemoryStream(stream.ToArray(), writable: false);
    }

    private static void PersistReproArtifact(string campaignName, int seed, byte[] bytes, Exception exception)
    {
        var root = Environment.GetEnvironmentVariable(HardeningArtifactsDirEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        Directory.CreateDirectory(root);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var basePath = Path.Combine(root, $"{campaignName}-{stamp}-{Guid.NewGuid():N}");
        File.WriteAllBytes($"{basePath}.bin", bytes);
        File.WriteAllText(
            $"{basePath}.txt",
            $"seed={seed}{Environment.NewLine}length={bytes.Length}{Environment.NewLine}exception={exception.GetType().FullName}{Environment.NewLine}message={exception.Message}");
    }

}
