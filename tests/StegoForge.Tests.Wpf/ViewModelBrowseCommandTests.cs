using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class ViewModelBrowseCommandTests
{
    [Fact]
    public void EmbedBrowseCommands_UpdateExpectedPathProperties()
    {
        using var fixture = new TempFileFixture();
        var initialCarrier = Path.Combine(fixture.RootPath, "initial-carrier.bin");
        var initialPayload = Path.Combine(fixture.RootPath, "initial-payload.bin");
        var initialOutput = Path.Combine(fixture.RootPath, "initial-output.bin");

        var dialog = new StubFileDialogService
        {
            CarrierPathToReturn = fixture.CarrierPath,
            PayloadPathToReturn = fixture.PayloadPath,
            EmbedOutputPathToReturn = fixture.OutputPath,
        };
        var vm = CreateEmbedViewModel(dialog)
        {
            CarrierPath = initialCarrier,
            PayloadPath = initialPayload,
            OutputPath = initialOutput,
        };

        vm.BrowseCarrierCommand.Execute(null);
        vm.BrowsePayloadCommand.Execute(null);
        vm.BrowseOutputCommand.Execute(null);

        Assert.Equal(fixture.CarrierPath, vm.CarrierPath);
        Assert.Equal(fixture.PayloadPath, vm.PayloadPath);
        Assert.Equal(fixture.OutputPath, vm.OutputPath);
        Assert.Equal(initialCarrier, dialog.LastCarrierInitialPath);
        Assert.Equal(initialPayload, dialog.LastPayloadInitialPath);
        Assert.Equal(initialOutput, dialog.LastEmbedOutputInitialPath);
    }

    [Fact]
    public void ExtractBrowseCommands_UpdateExpectedPathProperties()
    {
        using var fixture = new TempFileFixture();
        var initialCarrier = Path.Combine(fixture.RootPath, "initial-carrier.bin");
        var initialOutput = Path.Combine(fixture.RootPath, "initial-output.bin");

        var dialog = new StubFileDialogService
        {
            CarrierPathToReturn = fixture.CarrierPath,
            ExtractOutputPathToReturn = fixture.OutputPath,
        };
        var vm = CreateExtractViewModel(dialog)
        {
            CarrierPath = initialCarrier,
            OutputPath = initialOutput,
            AllowOverwrite = true,
        };

        vm.BrowseCarrierCommand.Execute(null);
        vm.BrowseOutputCommand.Execute(null);

        Assert.Equal(fixture.CarrierPath, vm.CarrierPath);
        Assert.Equal(fixture.OutputPath, vm.OutputPath);
        Assert.Equal(initialCarrier, dialog.LastCarrierInitialPath);
        Assert.Equal(initialOutput, dialog.LastExtractOutputInitialPath);
    }

    [Fact]
    public void DroppedPaths_UpdateProperties_AndValidationState()
    {
        using var fixture = new TempFileFixture();
        var vm = CreateEmbedViewModel(new StubFileDialogService());

        Assert.False(vm.EmbedCommand.CanExecute(null));

        Assert.True(vm.TryApplyDroppedCarrierPath(fixture.CarrierPath));
        Assert.True(vm.TryApplyDroppedPayloadPath(fixture.PayloadPath));
        Assert.True(vm.TryApplyDroppedOutputPath(fixture.OutputPath));

        Assert.Equal(fixture.CarrierPath, vm.CarrierPath);
        Assert.Equal(fixture.PayloadPath, vm.PayloadPath);
        Assert.Equal(fixture.OutputPath, vm.OutputPath);
        Assert.True(vm.EmbedCommand.CanExecute(null));
    }

    private static EmbedViewModel CreateEmbedViewModel(IFileDialogService dialogService)
    {
        return new EmbedViewModel(
            new NoOpEmbedService(),
            new NoOpCapacityService(),
            new NoOpInfoService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            dialogService,
            new AlwaysConfirmNotificationService());
    }

    private static ExtractViewModel CreateExtractViewModel(IFileDialogService dialogService)
    {
        return new ExtractViewModel(
            new NoOpExtractService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            dialogService,
            new AlwaysConfirmNotificationService());
    }

    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string PayloadPath { get; }
        public string OutputPath { get; }

        public TempFileFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-wpf-browse-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            CarrierPath = Path.Combine(RootPath, "carrier.bin");
            PayloadPath = Path.Combine(RootPath, "payload.bin");
            OutputPath = Path.Combine(RootPath, "output.bin");

            File.WriteAllBytes(CarrierPath, [1]);
            File.WriteAllBytes(PayloadPath, [2]);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? CarrierPathToReturn { get; init; }
        public string? PayloadPathToReturn { get; init; }
        public string? EmbedOutputPathToReturn { get; init; }
        public string? ExtractOutputPathToReturn { get; init; }

        public string? LastCarrierInitialPath { get; private set; }
        public string? LastPayloadInitialPath { get; private set; }
        public string? LastEmbedOutputInitialPath { get; private set; }
        public string? LastExtractOutputInitialPath { get; private set; }

        public string? SelectCarrierPath(string? initialPath = null)
        {
            LastCarrierInitialPath = initialPath;
            return CarrierPathToReturn;
        }

        public string? SelectPayloadPath(string? initialPath = null)
        {
            LastPayloadInitialPath = initialPath;
            return PayloadPathToReturn;
        }

        public string? SelectEmbedOutputPath(string? initialPath = null)
        {
            LastEmbedOutputInitialPath = initialPath;
            return EmbedOutputPathToReturn;
        }

        public string? SelectExtractOutputPath(string? initialPath = null)
        {
            LastExtractOutputInitialPath = initialPath;
            return ExtractOutputPathToReturn;
        }
    }

    private sealed class AlwaysConfirmNotificationService : INotificationService
    {
        public void ShowError(string title, string message)
        {
        }

        public bool Confirm(string title, string message)
        {
            return true;
        }
    }

    private sealed class NoOpEmbedService : IEmbedService
    {
        public Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EmbedResponse(request.OutputPath, "noop", request.Payload.LongLength, request.Payload.LongLength));
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

    private sealed class NoOpExtractService : IExtractService
    {
        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExtractResponse(request.OutputPath, request.OutputPath, "noop", [1], false, false));
        }
    }
}
