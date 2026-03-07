using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using StegoForge.Application;
using StegoForge.Cli;
using Xunit;

namespace StegoForge.Tests.Cli;

public sealed class CliCommandIntegrationTests
{
    [Fact]
    public async Task Embed_Succeeds_WithPngCarrier()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreatePngCarrier("carrier.png", width: 96, height: 96);
        var payloadPath = workspace.CreatePayload("payload.bin", FixturePayload(256));
        var outputPath = workspace.Resolve("embedded.png");

        var result = await RunCliAsync(["embed", "--carrier", carrierPath, "--payload", payloadPath, "--out", outputPath]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
        Assert.Contains("Command: embed", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Carrier format: png-lsb-v1", result.Stdout, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task Embed_Fails_WhenCarrierCapacityIsInsufficient()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreateBmpCarrier("tiny.bmp", width: 8, height: 8);
        var payloadPath = workspace.CreatePayload("too-large.bin", FixturePayload(4_096));
        var outputPath = workspace.Resolve("should-not-exist.bmp");

        var result = await RunCliAsync(["embed", "--carrier", carrierPath, "--payload", payloadPath, "--out", outputPath]);

        Assert.Equal(9, result.ExitCode);
        Assert.Contains("ERROR [InsufficientCapacity]", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("Required", result.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public async Task Extract_Succeeds_ForEncryptedPayload_WithCorrectPassword()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreatePngCarrier("carrier.png", width: 128, height: 128);
        var payload = FixturePayload(512);
        var payloadPath = workspace.CreatePayload("payload.bin", payload);
        var embeddedPath = workspace.Resolve("embedded.png");
        var extractedPath = workspace.Resolve("extracted.bin");

        var embed = await RunCliAsync([
            "embed", "--carrier", carrierPath, "--payload", payloadPath, "--out", embeddedPath,
            "--encrypt", "required", "--password", "correct-password"]);

        Assert.Equal(0, embed.ExitCode);

        var extract = await RunCliAsync([
            "extract", "--carrier", embeddedPath, "--out", extractedPath,
            "--password", "correct-password"]);

        Assert.Equal(0, extract.ExitCode);
        Assert.Equal(string.Empty, extract.Stderr);
        Assert.Contains("Command: extract", extract.Stdout, StringComparison.Ordinal);
        Assert.Contains("Was encrypted: True", extract.Stdout, StringComparison.Ordinal);
        Assert.Equal(payload, await File.ReadAllBytesAsync(extractedPath));
    }

    [Fact]
    public async Task Extract_Fails_WithWrongPassword()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreatePngCarrier("carrier.png", width: 128, height: 128);
        var payloadPath = workspace.CreatePayload("payload.bin", FixturePayload(384));
        var embeddedPath = workspace.Resolve("embedded.png");
        var extractedPath = workspace.Resolve("wrong.bin");

        var embed = await RunCliAsync([
            "embed", "--carrier", carrierPath, "--payload", payloadPath, "--out", embeddedPath,
            "--encrypt", "required", "--password", "correct-password"]);

        Assert.Equal(0, embed.ExitCode);

        var extract = await RunCliAsync([
            "extract", "--carrier", embeddedPath, "--out", extractedPath,
            "--password", "wrong-password"]);

        Assert.Equal(8, extract.ExitCode);
        Assert.Contains("ERROR [WrongPassword]", extract.Stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(extractedPath));
    }

    [Fact]
    public async Task Capacity_Succeeds_ForWavCarrier()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreateWavCarrier("carrier.wav", sampleFramesPerChannel: 22_050, channels: 1);

        var result = await RunCliAsync(["capacity", "--carrier", carrierPath, "--payload", "512"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
        Assert.Contains("Command: capacity", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Can embed: True", result.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Capacity_ReportsOverflowDiagnostics_ForExcessivePayload()
    {
        using var workspace = new CliFixtureWorkspace();
        var carrierPath = workspace.CreateBmpCarrier("small.bmp", width: 24, height: 24);

        var result = await RunCliAsync(["capacity", "--carrier", carrierPath, "--payload", "999999"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
        Assert.Contains("Can embed: False", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Failure reason:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Constraints:", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("exceeds safe usable capacity", result.Stdout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Info_ReportsMetadataPresenceAndAbsence_AsJson()
    {
        using var workspace = new CliFixtureWorkspace();
        var cleanCarrierPath = workspace.CreateWavCarrier("clean.wav", sampleFramesPerChannel: 8_192, channels: 1);
        var pngCarrierPath = workspace.CreatePngCarrier("carrier.png", width: 128, height: 128);
        var payloadPath = workspace.CreatePayload("payload.bin", FixturePayload(320));
        var embeddedPath = workspace.Resolve("embedded.png");

        var embed = await RunCliAsync(["embed", "--carrier", pngCarrierPath, "--payload", payloadPath, "--out", embeddedPath]);
        Assert.Equal(0, embed.ExitCode);

        var absentInfo = await RunCliAsync(["info", "--carrier", cleanCarrierPath, "--json"]);
        var presentInfo = await RunCliAsync(["info", "--carrier", embeddedPath, "--json"]);

        Assert.Equal(0, absentInfo.ExitCode);
        Assert.Equal(0, presentInfo.ExitCode);

        using var absentDocument = JsonDocument.Parse(absentInfo.Stdout);
        using var presentDocument = JsonDocument.Parse(presentInfo.Stdout);

        Assert.False(absentDocument.RootElement.GetProperty("embeddedDataPresent").GetBoolean());
        Assert.Equal(JsonValueKind.Null, absentDocument.RootElement.GetProperty("payloadMetadata").ValueKind);

        Assert.True(presentDocument.RootElement.GetProperty("embeddedDataPresent").GetBoolean());
        Assert.Equal(JsonValueKind.Object, presentDocument.RootElement.GetProperty("payloadMetadata").ValueKind);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("help")]
    public async Task Help_IsDiscoverable_FromHelpEntrypoints(string argument)
    {
        var result = await RunCliAsync([argument]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("StegoForge CLI", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("embed", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("extract", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public async Task Version_Command_IsDiscoverable()
    {
        var result = await RunCliAsync(["version"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Command: version", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Name: StegoForge", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("Version:", result.Stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, result.Stderr);
    }

    private static byte[] FixturePayload(int byteCount)
    {
        var buffer = new byte[byteCount];
        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = (byte)((index * 31 + 17) % 256);
        }

        return buffer;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(string[] args)
    {
        using var services = new ServiceCollection()
            .AddStegoForgeApplicationServices()
            .BuildServiceProvider();

        var stdoutWriter = new StringWriter();
        var stderrWriter = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        Console.SetOut(stdoutWriter);
        Console.SetError(stderrWriter);

        try
        {
            var exitCode = await CliApplication.RunAsync(args, services);
            return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private sealed class CliFixtureWorkspace : IDisposable
    {
        private readonly string _root;

        public CliFixtureWorkspace()
        {
            _root = Path.Combine(Path.GetTempPath(), "StegoForge.CliTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public string Resolve(string relativePath) => Path.Combine(_root, relativePath);

        public string CreatePayload(string fileName, byte[] payload)
        {
            var path = Resolve(fileName);
            File.WriteAllBytes(path, payload);
            return path;
        }

        public string CreatePngCarrier(string fileName, int width, int height)
        {
            var path = Resolve(fileName);
            using var image = new Image<Rgb24>(width, height);
            for (var y = 0; y < image.Height; y++)
            {
                Span<Rgb24> row = image.GetPixelRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgb24((byte)((x * 3) % 255), (byte)((y * 5) % 255), (byte)((x + y) % 255));
                }
            }

            image.SaveAsPng(path, new PngEncoder());
            return path;
        }

        public string CreateBmpCarrier(string fileName, int width, int height)
        {
            var path = Resolve(fileName);
            using var image = new Image<Rgb24>(width, height);
            for (var y = 0; y < image.Height; y++)
            {
                Span<Rgb24> row = image.GetPixelRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgb24((byte)((x * 11 + y) % 255), (byte)((y * 7 + x) % 255), (byte)((x * 13 + y * 3) % 255));
                }
            }

            image.SaveAsBmp(path, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
            return path;
        }

        public string CreateWavCarrier(string fileName, int sampleFramesPerChannel, int channels)
        {
            var path = Resolve(fileName);
            var bytes = CreatePcm16Wav(sampleFramesPerChannel, channels);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }

        private static byte[] CreatePcm16Wav(int sampleFramesPerChannel, int channels)
        {
            const int sampleRate = 44_100;
            const short bitsPerSample = 16;
            var blockAlign = (short)(channels * bitsPerSample / 8);
            var byteRate = sampleRate * blockAlign;
            var sampleCount = sampleFramesPerChannel * channels;
            var dataSize = sampleCount * sizeof(short);

            using var stream = new MemoryStream(capacity: 44 + dataSize);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write("RIFF"u8.ToArray());
            writer.Write(36 + dataSize);
            writer.Write("WAVE"u8.ToArray());
            writer.Write("fmt "u8.ToArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write("data"u8.ToArray());
            writer.Write(dataSize);

            for (var i = 0; i < sampleCount; i++)
            {
                var value = (short)((i * 521) % short.MaxValue);
                writer.Write(value);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }
}
