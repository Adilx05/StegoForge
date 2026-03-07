using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Application;
using StegoForge.Application.Capacity;
using StegoForge.Application.Embed;
using StegoForge.Application.Extract;
using StegoForge.Application.Formats;
using StegoForge.Application.Info;
using StegoForge.Application.Payload;
using StegoForge.Application.Validation;
using StegoForge.Compression.Deflate;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Crypto.AesGcm;
using Xunit;

namespace StegoForge.Tests.Integration;

public sealed class OrchestrationConsistencyIntegrationTests
{
    [Theory]
    [InlineData(CarrierType.Png)]
    [InlineData(CarrierType.Bmp)]
    [InlineData(CarrierType.Wav)]
    public async Task EmbedExtract_RoundTripConsistency_IsEquivalentAcrossFormats(CarrierType carrierType)
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateCarrierAsync(sandbox.Root, carrierType, "carrier");
        var stegoPath = Path.Combine(sandbox.Root, $"carrier.stego.{GetExtension(carrierType)}");
        var extractPath = Path.Combine(sandbox.Root, "payload.bin");
        var payload = CreateDeterministicPayload(2048, seed: 17);

        using var services = BuildDefaultServiceProvider();
        var embedService = services.GetRequiredService<IEmbedService>();
        var extractService = services.GetRequiredService<IExtractService>();

        var embedResponse = await embedService.EmbedAsync(new EmbedRequest(
            carrierPath,
            stegoPath,
            payload,
            new ProcessingOptions(
                compressionMode: CompressionMode.Enabled,
                compressionLevel: 6,
                encryptionMode: EncryptionMode.None,
                overwriteBehavior: OverwriteBehavior.Allow)));

        var extractResponse = await extractService.ExtractAsync(new ExtractRequest(
            stegoPath,
            extractPath,
            new ProcessingOptions(
                compressionMode: CompressionMode.Automatic,
                encryptionMode: EncryptionMode.Optional,
                overwriteBehavior: OverwriteBehavior.Allow)));

        Assert.Equal(payload, extractResponse.Payload);
        Assert.Equal(GetFormatId(carrierType), embedResponse.CarrierFormatId);
        Assert.Equal(GetFormatId(carrierType), extractResponse.CarrierFormatId);
    }

    [Theory]
    [InlineData(CarrierType.Png)]
    [InlineData(CarrierType.Bmp)]
    [InlineData(CarrierType.Wav)]
    public async Task Embed_WhenCapacityIsInsufficient_UsesSameErrorSemanticsPatternAcrossFormats(CarrierType carrierType)
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateSmallCarrierAsync(sandbox.Root, carrierType, "small");
        var stegoPath = Path.Combine(sandbox.Root, $"small.stego.{GetExtension(carrierType)}");
        var payload = CreateDeterministicPayload(2048, seed: 23);

        using var services = BuildDefaultServiceProvider();
        var embedService = services.GetRequiredService<IEmbedService>();

        var exception = await Assert.ThrowsAsync<InsufficientCapacityException>(() =>
            embedService.EmbedAsync(new EmbedRequest(
                carrierPath,
                stegoPath,
                payload,
                new ProcessingOptions(
                    compressionMode: CompressionMode.Disabled,
                    encryptionMode: EncryptionMode.None,
                    overwriteBehavior: OverwriteBehavior.Allow))));

        var mapped = StegoErrorMapper.FromException(exception);
        Assert.Equal(StegoErrorCode.InsufficientCapacity, mapped.Code);
        Assert.Contains("Insufficient capacity", mapped.Message);
        Assert.True(exception.RequiredBytes > exception.AvailableBytes);
    }

    [Theory]
    [InlineData(CarrierType.Png)]
    [InlineData(CarrierType.Bmp)]
    [InlineData(CarrierType.Wav)]
    public async Task GetInfo_ResponseContract_IsComparableAcrossFormats(CarrierType carrierType)
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateCarrierAsync(sandbox.Root, carrierType, "info");
        var stegoPath = Path.Combine(sandbox.Root, $"info.stego.{GetExtension(carrierType)}");
        var payload = CreateDeterministicPayload(1024, seed: 81);

        using var services = BuildDefaultServiceProvider();
        var embedService = services.GetRequiredService<IEmbedService>();
        var infoService = services.GetRequiredService<IInfoService>();

        await embedService.EmbedAsync(new EmbedRequest(
            carrierPath,
            stegoPath,
            payload,
            new ProcessingOptions(
                compressionMode: CompressionMode.Enabled,
                compressionLevel: 9,
                encryptionMode: EncryptionMode.Optional,
                overwriteBehavior: OverwriteBehavior.Allow),
            new PasswordOptions(PasswordRequirement.Required, PasswordSourceHint.None, "MatrixPassphrase!")));

        var info = await infoService.GetInfoAsync(new InfoRequest(stegoPath));

        Assert.True(info.EmbeddedDataPresent);
        Assert.Equal(GetFormatId(carrierType), info.FormatId);
        Assert.True(info.SupportsEncryption);
        Assert.True(info.SupportsCompression);
        Assert.NotNull(info.PayloadMetadata);
        Assert.Equal(payload.LongLength, info.PayloadMetadata.OriginalSizeBytes);
        Assert.NotNull(info.ProtectionDescriptors);
        Assert.False(string.IsNullOrWhiteSpace(info.ProtectionDescriptors.CompressionDescriptor));
        Assert.False(string.IsNullOrWhiteSpace(info.ProtectionDescriptors.EncryptionDescriptor));
        Assert.Equal("tagged", info.ProtectionDescriptors.IntegrityDescriptor);
        Assert.StartsWith("cmp:", info.Diagnostics.AlgorithmIdentifier);
        Assert.Equal("stegoforge.application.info-service", info.Diagnostics.ProviderIdentifier);
    }

    [Theory]
    [InlineData(CarrierType.Png)]
    [InlineData(CarrierType.Bmp)]
    [InlineData(CarrierType.Wav)]
    public async Task Extract_EncryptedPayload_WrongPasswordAndTamperPaths_AreCoveredViaApplicationServices(CarrierType carrierType)
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateCarrierAsync(sandbox.Root, carrierType, "encrypted");
        var stegoPath = Path.Combine(sandbox.Root, $"encrypted.stego.{GetExtension(carrierType)}");
        var wrongOutPath = Path.Combine(sandbox.Root, "wrong.bin");
        var tamperedPath = Path.Combine(sandbox.Root, $"encrypted.tampered.{GetExtension(carrierType)}");
        var tamperedOutPath = Path.Combine(sandbox.Root, "tampered.bin");
        var payload = CreateDeterministicPayload(900, seed: 55);

        using var services = BuildDefaultServiceProvider();
        var embedService = services.GetRequiredService<IEmbedService>();
        var extractService = services.GetRequiredService<IExtractService>();

        await embedService.EmbedAsync(new EmbedRequest(
            carrierPath,
            stegoPath,
            payload,
            new ProcessingOptions(
                compressionMode: CompressionMode.Disabled,
                encryptionMode: EncryptionMode.Required,
                overwriteBehavior: OverwriteBehavior.Allow),
            new PasswordOptions(PasswordRequirement.Required, PasswordSourceHint.None, "correct-password")));

        var wrongPasswordException = await Assert.ThrowsAsync<WrongPasswordException>(() =>
            extractService.ExtractAsync(new ExtractRequest(
                stegoPath,
                wrongOutPath,
                new ProcessingOptions(
                    compressionMode: CompressionMode.Automatic,
                    encryptionMode: EncryptionMode.Required,
                    overwriteBehavior: OverwriteBehavior.Allow),
                new PasswordOptions(PasswordRequirement.Required, PasswordSourceHint.None, "wrong-password"))));

        Assert.Equal(StegoErrorCode.WrongPassword, StegoErrorMapper.FromException(wrongPasswordException).Code);

        await TamperCarrierAsync(stegoPath, tamperedPath);

        var tamperException = await Assert.ThrowsAnyAsync<StegoForgeException>(() =>
            extractService.ExtractAsync(new ExtractRequest(
                tamperedPath,
                tamperedOutPath,
                new ProcessingOptions(
                    compressionMode: CompressionMode.Automatic,
                    encryptionMode: EncryptionMode.Required,
                    overwriteBehavior: OverwriteBehavior.Allow),
                new PasswordOptions(PasswordRequirement.Required, PasswordSourceHint.None, "correct-password"))));

        var tamperCode = StegoErrorMapper.FromException(tamperException).Code;
        Assert.Contains(tamperCode, [StegoErrorCode.WrongPassword, StegoErrorCode.CorruptedData, StegoErrorCode.InvalidHeader, StegoErrorCode.InvalidPayload]);
    }

    [Fact]
    public async Task Embed_InvalidPolicyCombination_FailsBeforeHandlerIo()
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateCarrierAsync(sandbox.Root, CarrierType.Png, "policy-embed");
        var outputPath = Path.Combine(sandbox.Root, "policy-embed.stego.png");
        var probe = new ProbeCarrierFormatHandler();

        using var services = BuildServiceProviderWithProbeHandler(probe);
        var embedService = services.GetRequiredService<IEmbedService>();

        await Assert.ThrowsAsync<InvalidArgumentsException>(() => embedService.EmbedAsync(new EmbedRequest(
            carrierPath,
            outputPath,
            CreateDeterministicPayload(128, seed: 7),
            new ProcessingOptions(
                compressionMode: CompressionMode.Disabled,
                compressionLevel: 3,
                encryptionMode: EncryptionMode.None,
                overwriteBehavior: OverwriteBehavior.Allow))));

        Assert.Equal(0, probe.IoInvocationCount);
    }

    [Fact]
    public async Task Extract_InvalidPolicyCombination_FailsBeforeHandlerIo()
    {
        using var sandbox = new TempSandbox();
        var carrierPath = await CreateCarrierAsync(sandbox.Root, CarrierType.Png, "policy-extract");
        var outputPath = Path.Combine(sandbox.Root, "policy-extract.bin");
        var probe = new ProbeCarrierFormatHandler();

        using var services = BuildServiceProviderWithProbeHandler(probe);
        var extractService = services.GetRequiredService<IExtractService>();

        await Assert.ThrowsAsync<InvalidArgumentsException>(() => extractService.ExtractAsync(new ExtractRequest(
            carrierPath,
            outputPath,
            new ProcessingOptions(
                compressionMode: CompressionMode.Automatic,
                encryptionMode: EncryptionMode.Required,
                overwriteBehavior: OverwriteBehavior.Allow),
            passwordOptions: PasswordOptions.Optional)));

        Assert.Equal(0, probe.IoInvocationCount);
    }

    private static ServiceProvider BuildDefaultServiceProvider()
        => new ServiceCollection()
            .AddStegoForgeApplicationServices()
            .BuildServiceProvider();

    private static ServiceProvider BuildServiceProviderWithProbeHandler(ICarrierFormatHandler handler)
        => new ServiceCollection()
            .AddSingleton(handler)
            .AddSingleton<ICompressionProvider, DeflateCompressionProvider>()
            .AddSingleton<ICryptoProvider, AesGcmCryptoProvider>()
            .AddSingleton<CarrierFormatResolver>()
            .AddSingleton<OperationPolicyValidator>()
            .AddSingleton<PayloadOrchestrationService>()
            .AddSingleton<IPayloadEnvelopeSerializer, PayloadEnvelopeSerializer>()
            .AddSingleton<IEmbedService, EmbedService>()
            .AddSingleton<IExtractService, ExtractService>()
            .AddSingleton<IInfoService, InfoService>()
            .AddSingleton<ICapacityService, CapacityService>()
            .BuildServiceProvider();

    private static async Task<string> CreateCarrierAsync(string root, CarrierType type, string fileStem)
    {
        return type switch
        {
            CarrierType.Png => await CreatePngAsync(Path.Combine(root, $"{fileStem}.png"), 220, 220),
            CarrierType.Bmp => await CreateBmpAsync(Path.Combine(root, $"{fileStem}.bmp"), 220, 220),
            CarrierType.Wav => await CreateWavAsync(Path.Combine(root, $"{fileStem}.wav"), sampleFramesPerChannel: 44_100, channels: 2),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported test carrier type.")
        };
    }

    private static async Task<string> CreateSmallCarrierAsync(string root, CarrierType type, string fileStem)
    {
        return type switch
        {
            CarrierType.Png => await CreatePngAsync(Path.Combine(root, $"{fileStem}.png"), 32, 32),
            CarrierType.Bmp => await CreateBmpAsync(Path.Combine(root, $"{fileStem}.bmp"), 32, 32),
            CarrierType.Wav => await CreateWavAsync(Path.Combine(root, $"{fileStem}.wav"), sampleFramesPerChannel: 1_024, channels: 1),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported test carrier type.")
        };
    }

    private static async Task<string> CreatePngAsync(string path, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32((byte)(x % 251), (byte)(y % 251), (byte)((x + y) % 251), 255);
                }
            }
        });

        await image.SaveAsync(path, new PngEncoder());
        return path;
    }

    private static async Task<string> CreateBmpAsync(string path, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32((byte)(x % 239), (byte)(y % 239), 90, 255);
                }
            }
        });

        await image.SaveAsBmpAsync(path, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
        return path;
    }

    private static async Task<string> CreateWavAsync(string path, int sampleFramesPerChannel, int channels)
    {
        var wav = CreatePcm16Wav(sampleFramesPerChannel, channels);
        await File.WriteAllBytesAsync(path, wav);
        return path;
    }

    private static byte[] CreatePcm16Wav(int sampleFramesPerChannel, int channels)
    {
        const int bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = channels * bytesPerSample;
        const int sampleRate = 44_100;
        var byteRate = sampleRate * blockAlign;
        var dataBytes = sampleFramesPerChannel * blockAlign;

        const int fmtChunkSize = 16;
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

        for (var i = 0; i < sampleFramesPerChannel * channels; i++)
        {
            var sampleValue = (short)(5000 * Math.Sin((2 * Math.PI * 440 * i) / sampleRate));
            stream.Write(BitConverter.GetBytes(sampleValue));
        }

        return stream.ToArray();
    }

    private static byte[] CreateDeterministicPayload(int length, int seed)
    {
        var payload = new byte[length];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)((seed + (i * 13)) % 256);
        }

        return payload;
    }

    private static async Task TamperCarrierAsync(string sourcePath, string targetPath)
    {
        var bytes = await File.ReadAllBytesAsync(sourcePath);
        var index = Math.Max(0, bytes.Length / 2);
        bytes[index] ^= 0x01;
        await File.WriteAllBytesAsync(targetPath, bytes);
    }

    private static string GetFormatId(CarrierType type)
        => type switch
        {
            CarrierType.Png => "png-lsb-v1",
            CarrierType.Bmp => "bmp-lsb-v1",
            CarrierType.Wav => "wav-lsb-v1",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported test carrier type.")
        };

    private static string GetExtension(CarrierType type)
        => type.ToString().ToLowerInvariant();

    public enum CarrierType
    {
        Png,
        Bmp,
        Wav
    }

    private sealed class TempSandbox : IDisposable
    {
        public TempSandbox()
        {
            Root = Path.Combine(Path.GetTempPath(), $"stegoforge-matrix-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class ProbeCarrierFormatHandler : ICarrierFormatHandler
    {
        public int IoInvocationCount { get; private set; }

        public string Format => "probe-lsb-v1";

        public bool Supports(Stream carrierStream)
        {
            IoInvocationCount++;
            return true;
        }

        public Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default)
        {
            IoInvocationCount++;
            throw new InvalidOperationException("Probe handler should not be called.");
        }

        public Task EmbedAsync(Stream carrierStream, Stream outputStream, byte[] payload, CancellationToken cancellationToken = default)
        {
            IoInvocationCount++;
            throw new InvalidOperationException("Probe handler should not be called.");
        }

        public Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default)
        {
            IoInvocationCount++;
            throw new InvalidOperationException("Probe handler should not be called.");
        }

        public Task<CarrierInfoResponse> GetInfoAsync(Stream carrierStream, CancellationToken cancellationToken = default)
        {
            IoInvocationCount++;
            throw new InvalidOperationException("Probe handler should not be called.");
        }
    }
}
