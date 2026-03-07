using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class ViewModelOperationStateTests
{
    [Fact]
    public async Task EmbedCommand_TracksBusyLifecycle_AndCompletionState()
    {
        using var fixture = new TempFileFixture();
        var embedService = new BlockingEmbedService();
        var vm = CreateEmbedViewModel(embedService);

        vm.CarrierPath = fixture.CarrierPath;
        vm.PayloadPath = fixture.PayloadPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.EmbedCommand.Execute(null);

        await WaitUntilAsync(static model => model.IsBusy, vm);
        Assert.Equal("Embedding payload...", vm.StatusMessage);
        Assert.Equal("Submitting embed request", vm.ProgressText);

        embedService.Release();
        await WaitUntilAsync(static model => !model.IsBusy, vm);

        Assert.Equal("Completed", vm.ProgressText);
        Assert.Equal("Embed completed.", vm.StatusMessage);
        Assert.Null(vm.LastErrorCode);
        Assert.Null(vm.LastErrorMessage);
    }

    [Fact]
    public async Task ExtractCommand_MapsErrors_UsingSharedErrorMapper()
    {
        using var fixture = new TempFileFixture();
        var vm = CreateExtractViewModel(new ThrowingExtractService(new FileNotFoundStegoException(fixture.CarrierPath)));

        vm.CarrierPath = fixture.CarrierPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.ExtractCommand.Execute(null);
        await WaitUntilAsync(static model => !model.IsBusy && model.LastErrorCode is not null, vm);

        Assert.Equal(StegoErrorCode.FileNotFound.ToString(), vm.LastErrorCode);
        Assert.Contains(fixture.CarrierPath, vm.LastErrorMessage, StringComparison.Ordinal);
        Assert.Contains("FileNotFound", vm.ResultMessage, StringComparison.Ordinal);
        Assert.Equal("Failed", vm.ProgressText);
    }

    [Fact]
    public async Task ExtractCommand_ResetsStatusBetweenOperations()
    {
        using var fixture = new TempFileFixture();
        var service = new SequencedExtractService(
            new OutputExistsException(fixture.OutputPath),
            new ExtractResponse(fixture.OutputPath, fixture.OutputPath, "noop", [1, 2, 3], false, false));
        var vm = CreateExtractViewModel(service);

        vm.CarrierPath = fixture.CarrierPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.ExtractCommand.Execute(null);
        await WaitUntilAsync(static model => !model.IsBusy && model.LastErrorCode is not null, vm);
        Assert.Equal(StegoErrorCode.OutputAlreadyExists.ToString(), vm.LastErrorCode);

        vm.ExtractCommand.Execute(null);
        await WaitUntilAsync(static model => !model.IsBusy && model.ProgressText == "Completed", vm);

        Assert.Null(vm.LastErrorCode);
        Assert.Null(vm.LastErrorMessage);
        Assert.Equal("Extract completed.", vm.StatusMessage);
    }

    private static async Task WaitUntilAsync(Func<OperationViewModelBase, bool> predicate, OperationViewModelBase vm)
    {
        var timeout = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < timeout)
        {
            if (predicate(vm))
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("Timed out while waiting for expected operation state.");
    }

    private static EmbedViewModel CreateEmbedViewModel(IEmbedService embedService)
    {
        return new EmbedViewModel(
            embedService,
            new NoOpCapacityService(),
            new NoOpInfoService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            new TestFileDialogService(),
            new TestNotificationService());
    }

    private static ExtractViewModel CreateExtractViewModel(IExtractService extractService)
    {
        return new ExtractViewModel(
            extractService,
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            new TestFileDialogService(),
            new TestNotificationService());
    }

    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string PayloadPath { get; }
        public string OutputPath { get; }

        public TempFileFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-wpf-state-tests-{Guid.NewGuid():N}");
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
        public string? SelectCarrierPath(string? initialPath = null)
        {
            return null;
        }

        public string? SelectPayloadPath(string? initialPath = null)
        {
            return null;
        }

        public string? SelectEmbedOutputPath(string? initialPath = null)
        {
            return null;
        }

        public string? SelectExtractOutputPath(string? initialPath = null)
        {
            return null;
        }
    }

    private sealed class TestNotificationService : INotificationService
    {
        public void ShowError(string title, string message)
        {
        }

        public bool Confirm(string title, string message)
        {
            return true;
        }
    }

    private sealed class BlockingEmbedService : IEmbedService
    {
        private readonly TaskCompletionSource<bool> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default)
        {
            return WaitAsync(request, cancellationToken);
        }

        public void Release()
        {
            _gate.TrySetResult(true);
        }

        private async Task<EmbedResponse> WaitAsync(EmbedRequest request, CancellationToken cancellationToken)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            return new EmbedResponse(request.OutputPath, "noop", request.Payload.LongLength, request.Payload.LongLength);
        }
    }

    private sealed class ThrowingExtractService(Exception exception) : IExtractService
    {
        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromException<ExtractResponse>(exception);
        }
    }

    private sealed class SequencedExtractService(Exception firstException, ExtractResponse nextResponse) : IExtractService
    {
        private int _invocationCount;

        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
        {
            _invocationCount++;
            if (_invocationCount == 1)
            {
                return Task.FromException<ExtractResponse>(firstException);
            }

            return Task.FromResult(nextResponse);
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
