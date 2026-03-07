using Microsoft.Extensions.DependencyInjection;
using StegoForge.Cli;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Cli;

public sealed class CommandPipelineExitCodeTests
{
    [Fact]
    public async Task EmbedCommand_ReturnsZeroOnSuccess()
    {
        var carrier = CreateTempFile();
        var payload = CreateTempFile("payload");
        var output = Path.Combine(Path.GetTempPath(), $"embed-out-{Guid.NewGuid():N}.png");
        var services = BuildServices(new FakeEmbedService(_ => Task.FromResult(new EmbedResponse(output, "png-lsb-v1", 7, 7))));

        var result = await RunWithCapturedStderrAsync(["embed", "--carrier", carrier, "--payload", payload, "--out", output], services);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public async Task EmbedCommand_ReturnsMappedFailureCodeAndStableStderrFormat()
    {
        var carrier = CreateTempFile();
        var payload = CreateTempFile("payload");
        var output = Path.Combine(Path.GetTempPath(), $"embed-out-{Guid.NewGuid():N}.png");
        var services = BuildServices(new FakeEmbedService(_ => Task.FromException<EmbedResponse>(new WrongPasswordException("Unable to decrypt."))));

        var result = await RunWithCapturedStderrAsync(["embed", "--carrier", carrier, "--payload", payload, "--out", output], services);

        Assert.Equal(8, result.ExitCode);
        Assert.StartsWith("ERROR [WrongPassword]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractCommand_ReturnsZeroOnSuccess()
    {
        var carrier = CreateTempFile();
        var output = Path.Combine(Path.GetTempPath(), $"extract-out-{Guid.NewGuid():N}.bin");
        var services = BuildServices(extractService: new FakeExtractService(_ => Task.FromResult(new ExtractResponse(output, output, "png-lsb-v1", [1], false, false))));

        var result = await RunWithCapturedStderrAsync(["extract", "--carrier", carrier, "--out", output], services);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task ExtractCommand_ReturnsMappedFailureCode()
    {
        var carrier = CreateTempFile();
        var output = Path.Combine(Path.GetTempPath(), $"extract-out-{Guid.NewGuid():N}.bin");
        var services = BuildServices(extractService: new FakeExtractService(_ => Task.FromException<ExtractResponse>(new InvalidPayloadException("Invalid envelope."))));

        var result = await RunWithCapturedStderrAsync(["extract", "--carrier", carrier, "--out", output], services);

        Assert.Equal(6, result.ExitCode);
        Assert.StartsWith("ERROR [InvalidPayload]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CapacityCommand_ReturnsZeroOnSuccess()
    {
        var carrier = CreateTempFile();
        var services = BuildServices(capacityService: new FakeCapacityService(_ => Task.FromResult(new CapacityResponse("png-lsb-v1", 10, 100, 100, 90, 2, true, 90))));

        var result = await RunWithCapturedStderrAsync(["capacity", "--carrier", carrier, "--payload", "10"], services);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CapacityCommand_ReturnsMappedFailureCode()
    {
        var carrier = CreateTempFile();
        var services = BuildServices(capacityService: new FakeCapacityService(_ => Task.FromException<CapacityResponse>(new InsufficientCapacityException(100, 10))));

        var result = await RunWithCapturedStderrAsync(["capacity", "--carrier", carrier, "--payload", "100"], services);

        Assert.Equal(9, result.ExitCode);
        Assert.StartsWith("ERROR [InsufficientCapacity]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfoCommand_ReturnsZeroOnSuccess()
    {
        var carrier = CreateTempFile();
        var response = new CarrierInfoResponse(
            "png-lsb-v1",
            new CarrierFormatDetails("png-lsb-v1", "PNG LSB", "1.0"),
            100,
            50,
            50,
            embeddedDataPresent: false,
            supportsEncryption: true,
            supportsCompression: true);
        var services = BuildServices(infoService: new FakeInfoService(_ => Task.FromResult(response)));

        var result = await RunWithCapturedStderrAsync(["info", "--carrier", carrier], services);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task InfoCommand_ReturnsMappedFailureCode()
    {
        var carrier = CreateTempFile();
        var services = BuildServices(infoService: new FakeInfoService(_ => Task.FromException<CarrierInfoResponse>(new UnsupportedFormatException("Unsupported."))));

        var result = await RunWithCapturedStderrAsync(["info", "--carrier", carrier], services);

        Assert.Equal(5, result.ExitCode);
        Assert.StartsWith("ERROR [UnsupportedFormat]", result.Stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VersionCommand_ReturnsZeroOnSuccess()
    {
        var result = await RunWithCapturedStderrAsync(["version"], BuildServices());
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Stderr);
    }

    [Fact]
    public async Task ParserError_MissingRequiredOptions_ReturnsInvalidArgumentsCodeAndStableMessageShape()
    {
        var errorWriter = new StringWriter();
        var exitCode = await CliApplication.RunAsync(["embed"], BuildServices(), errorWriter);

        Assert.Equal(3, exitCode);
        var stderr = errorWriter.ToString();
        Assert.StartsWith("ERROR [InvalidArguments]", stderr, StringComparison.Ordinal);
        Assert.Contains("--carrier", stderr, StringComparison.Ordinal);
        Assert.Contains("required", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParserError_InvalidEnumValue_ReturnsInvalidArgumentsCodeAndStableMessageShape()
    {
        var carrier = CreateTempFile();
        var errorWriter = new StringWriter();
        var exitCode = await CliApplication.RunAsync(["info", "--carrier", carrier, "--encrypt", "bad-value"], BuildServices(), errorWriter);

        Assert.Equal(3, exitCode);
        var stderr = errorWriter.ToString();
        Assert.StartsWith("ERROR [InvalidArguments]", stderr, StringComparison.Ordinal);
        Assert.Contains("Invalid value", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParserError_InvalidFileArgument_ReturnsInvalidArgumentsCodeAndStableMessageShape()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");

        var errorWriter = new StringWriter();
        var exitCode = await CliApplication.RunAsync(["info", "--carrier", missingPath], BuildServices(), errorWriter);

        Assert.Equal(3, exitCode);
        var stderr = errorWriter.ToString();
        Assert.StartsWith("ERROR [InvalidArguments]", stderr, StringComparison.Ordinal);
        Assert.Contains("does not exist", stderr, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempFile(string contents = "carrier")
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, contents);
        return path;
    }

    private static async Task<(int ExitCode, string Stderr)> RunWithCapturedStderrAsync(string[] args, ServiceProvider services)
    {
        var writer = new StringWriter();
        var original = Console.Error;
        Console.SetError(writer);
        try
        {
            var exitCode = await CliApplication.RunAsync(args, services);
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }

    private static ServiceProvider BuildServices(
        IEmbedService? embedService = null,
        IExtractService? extractService = null,
        ICapacityService? capacityService = null,
        IInfoService? infoService = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(embedService ?? new FakeEmbedService(_ => Task.FromResult(new EmbedResponse("out.bin", "png", 1, 1))));
        services.AddSingleton(extractService ?? new FakeExtractService(_ => Task.FromResult(new ExtractResponse("out.bin", "out.bin", "png", [1], false, false))));
        services.AddSingleton(capacityService ?? new FakeCapacityService(_ => Task.FromResult(new CapacityResponse("png", 1, 2, 2, 2, 0, true, 1))));
        services.AddSingleton(infoService ?? new FakeInfoService(_ => Task.FromResult(new CarrierInfoResponse("png", new CarrierFormatDetails("png", "PNG", "1"), 1, 1, 1, false, true, true))));
        return services.BuildServiceProvider();
    }

    private sealed class FakeEmbedService(Func<EmbedRequest, Task<EmbedResponse>> handler) : IEmbedService
    {
        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default) => handler(request);
    }

    private sealed class FakeExtractService(Func<ExtractRequest, Task<ExtractResponse>> handler) : IExtractService
    {
        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default) => handler(request);
    }

    private sealed class FakeCapacityService(Func<CapacityRequest, Task<CapacityResponse>> handler) : ICapacityService
    {
        public Task<CapacityResponse> GetCapacityAsync(CapacityRequest request, CancellationToken cancellationToken = default) => handler(request);
    }

    private sealed class FakeInfoService(Func<InfoRequest, Task<CarrierInfoResponse>> handler) : IInfoService
    {
        public Task<CarrierInfoResponse> GetInfoAsync(InfoRequest request, CancellationToken cancellationToken = default) => handler(request);
    }
}
