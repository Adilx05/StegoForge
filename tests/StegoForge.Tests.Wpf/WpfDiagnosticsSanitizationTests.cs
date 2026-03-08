using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class WpfDiagnosticsSanitizationTests
{
    [Fact]
    [Trait("Category", "Hardening")]
    [Trait("Surface", "WPF")]
    public async Task ExtractFailure_DoesNotExposeSecrets_InViewModelOrNotificationMessage()
    {
        using var fixture = new TempFileFixture();
        var notifications = new CapturingNotificationService();
        var vm = new ExtractViewModel(
            new ThrowingExtractService(new InvalidArgumentsException("password=super-secret plaintextPayloadBytes=[1,2,3] derivedKey=00112233445566778899aabbccddeeff")),
            new UiOperationPolicyValidator(new OperationPolicyValidator()),
            new TestFileDialogService(),
            notifications);

        vm.CarrierPath = fixture.CarrierPath;
        vm.OutputPath = fixture.OutputPath;
        vm.AllowOverwrite = true;

        vm.ExtractCommand.Execute(null);
        await WaitUntilAsync(static model => !model.IsBusy && model.LastErrorCode is not null, vm);

        Assert.DoesNotContain("super-secret", vm.LastErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("[1,2,3]", vm.LastErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("00112233445566778899aabbccddeeff", vm.LastErrorMessage, StringComparison.Ordinal);
        Assert.Contains("<redacted>", vm.LastErrorMessage, StringComparison.Ordinal);

        Assert.NotNull(notifications.LastMessage);
        Assert.DoesNotContain("super-secret", notifications.LastMessage, StringComparison.Ordinal);
        Assert.Contains("Correlation ID:", notifications.LastMessage, StringComparison.Ordinal);
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

    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string OutputPath { get; }

        public TempFileFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-wpf-sec-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            CarrierPath = Path.Combine(RootPath, "carrier.bin");
            OutputPath = Path.Combine(RootPath, "output.bin");

            File.WriteAllBytes(CarrierPath, [1, 2, 3]);
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

    private sealed class CapturingNotificationService : INotificationService
    {
        public string? LastMessage { get; private set; }

        public void ShowError(string title, string message)
        {
            LastMessage = message;
        }

        public bool Confirm(string title, string message) => true;
    }

    private sealed class ThrowingExtractService(Exception exception) : IExtractService
    {
        public Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
            => Task.FromException<ExtractResponse>(exception);
    }
}
