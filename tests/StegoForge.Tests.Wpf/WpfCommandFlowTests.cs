using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class WpfCommandFlowTests
{
    [WpfFact]
    public async Task EmbedCommand_InvokesEmbedService_AndUpdatesStatus()
    {
        using var fixture = new TempFileFixture();
        var embedService = new RecordingEmbedService();
        var vm = new EmbedViewModel(
            embedService,
            new NoOpCapacityService(),
            new NoOpInfoService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            new TestFileDialogService(),
            new AlwaysConfirmNotificationService());

        vm.CarrierPath = fixture.CarrierPath;
        vm.PayloadPath = fixture.PayloadPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.EmbedCommand.Execute(null);
        await WaitUntilAsync(() => embedService.CallCount == 1 && vm.ProgressText == "Completed");

        Assert.Equal(1, embedService.CallCount);
        Assert.Contains("Embed complete:", vm.ResultMessage, StringComparison.Ordinal);
        Assert.Equal("Embed completed.", vm.StatusMessage);
        Assert.Equal("Completed", vm.ProgressText);
    }

    [WpfFact]
    public async Task ExtractCommand_InvokesExtractService_AndUpdatesStatus()
    {
        using var fixture = new TempFileFixture();
        var extractService = new RecordingExtractService();
        var vm = new ExtractViewModel(
            extractService,
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            new TestFileDialogService(),
            new AlwaysConfirmNotificationService());

        vm.CarrierPath = fixture.CarrierPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.ExtractCommand.Execute(null);
        await WaitUntilAsync(() => extractService.CallCount == 1 && vm.ProgressText == "Completed");

        Assert.Equal(1, extractService.CallCount);
        Assert.Contains("Extract complete:", vm.ResultMessage, StringComparison.Ordinal);
        Assert.Equal("Extract completed.", vm.StatusMessage);
        Assert.Equal("Completed", vm.ProgressText);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < timeout)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Timed out waiting for command invocation to complete.");
    }

    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string PayloadPath { get; }
        public string OutputPath { get; }

        public TempFileFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-wpf-command-flow-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            CarrierPath = Path.Combine(RootPath, "carrier.bin");
            PayloadPath = Path.Combine(RootPath, "payload.bin");
            OutputPath = Path.Combine(RootPath, "output.bin");

            File.WriteAllBytes(CarrierPath, [1, 2, 3]);
            File.WriteAllBytes(PayloadPath, [4, 5, 6]);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    private sealed class TestFileDialogService : IFileDialogService
    {
        public string? SelectCarrierPath(string? initialPath = null) => null;
        public string? SelectPayloadPath(string? initialPath = null) => null;
        public string? SelectEmbedOutputPath(string? initialPath = null) => null;
        public string? SelectExtractOutputPath(string? initialPath = null) => null;
    }

    private sealed class AlwaysConfirmNotificationService : INotificationService
    {
        public void ShowError(string title, string message)
        {
        }

        public bool Confirm(string title, string message) => true;
    }

    private sealed class RecordingEmbedService : IEmbedService
    {
        public int CallCount { get; private set; }

        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EmbedResponse(request.OutputPath, "noop", request.Payload.LongLength, request.Payload.LongLength));
        }
    }

    private sealed class RecordingExtractService : IExtractService
    {
        public int CallCount { get; private set; }

        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new ExtractResponse(request.OutputPath, request.OutputPath, "noop", [1, 2], false, false));
        }
    }

    private sealed class NoOpCapacityService : ICapacityService
    {
        public Task<CapacityResponse> GetCapacityAsync(CapacityRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CapacityResponse("noop", request.PayloadSizeBytes, 10, 10, 10, 0, true, 0));
        }
    }

    private sealed class NoOpInfoService : IInfoService
    {
        public Task<CarrierInfoResponse> GetInfoAsync(InfoRequest request, CancellationToken cancellationToken = default)
        {
            var details = new CarrierFormatDetails("noop", "Noop", "1.0");
            return Task.FromResult(new CarrierInfoResponse("noop", details, 10, 10, 10, false, true, true));
        }
    }
}
