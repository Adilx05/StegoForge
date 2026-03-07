using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Wpf.Validation;
using StegoForge.Wpf.ViewModels;
using Xunit;

namespace StegoForge.Tests.Wpf;

public sealed class ViewModelValidationTests
{
    [Fact]
    public void EmbedViewModel_ReportsValidationMessage_ForMissingRequiredFields()
    {
        var vm = CreateEmbedViewModel();

        var carrierErrors = vm.GetErrors(nameof(EmbedViewModel.CarrierPath)).Cast<string>().ToArray();
        var payloadErrors = vm.GetErrors(nameof(EmbedViewModel.PayloadPath)).Cast<string>().ToArray();
        var outputErrors = vm.GetErrors(nameof(EmbedViewModel.OutputPath)).Cast<string>().ToArray();

        Assert.Contains("Carrier file is required.", carrierErrors);
        Assert.Contains("Payload file is required.", payloadErrors);
        Assert.Contains("Output file is required.", outputErrors);
    }

    [Fact]
    public void EmbedViewModel_CommandCanExecute_TransitionsWithValidationState()
    {
        using var fixture = new TempFileFixture(createOutputFile: true);
        var vm = CreateEmbedViewModel();

        Assert.False(vm.EmbedCommand.CanExecute(null));

        vm.CarrierPath = fixture.CarrierPath;
        vm.PayloadPath = fixture.PayloadPath;
        vm.OutputPath = fixture.OutputPath;

        Assert.False(vm.EmbedCommand.CanExecute(null));
        Assert.Contains(vm.GetErrors(nameof(EmbedViewModel.OutputPath)).Cast<string>(), static x => x.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        vm.AllowOverwrite = true;

        Assert.True(vm.EmbedCommand.CanExecute(null));
    }

    [Fact]
    public void ValidationService_MapsInvalidStates_ToExpectedDomainErrorCategories()
    {
        using var fixture = new TempFileFixture(createOutputFile: true);
        var service = new UiOperationPolicyValidator(new OperationPolicyValidator());

        var missingFields = service.ValidateEmbed("", "", "", requireEncryption: false, password: null, allowOverwrite: false);
        Assert.Contains(missingFields.Issues, static issue => issue.PropertyName == "CarrierPath" && issue.Code == StegoErrorCode.InvalidArguments);
        Assert.Contains(missingFields.Issues, static issue => issue.PropertyName == "PayloadPath" && issue.Code == StegoErrorCode.InvalidArguments);
        Assert.Contains(missingFields.Issues, static issue => issue.PropertyName == "OutputPath" && issue.Code == StegoErrorCode.InvalidArguments);

        var passwordRequired = service.ValidateExtract(fixture.CarrierPath, fixture.OutputPath, requireEncryption: true, password: null, allowOverwrite: true);
        Assert.Contains(passwordRequired.Issues, static issue => issue.Code == StegoErrorCode.InvalidArguments && issue.Message.Contains("password source", StringComparison.OrdinalIgnoreCase));

        var overwriteDenied = service.ValidateExtract(fixture.CarrierPath, fixture.OutputPath, requireEncryption: false, password: null, allowOverwrite: false);
        Assert.Contains(overwriteDenied.Issues, static issue => issue.PropertyName == "OutputPath" && issue.Code == StegoErrorCode.OutputAlreadyExists);
    }

    [Fact]
    public void ExtractViewModel_CommandCanExecute_TransitionsWithValidationState()
    {
        using var fixture = new TempFileFixture(createOutputFile: false);
        var vm = CreateExtractViewModel();

        Assert.False(vm.ExtractCommand.CanExecute(null));

        vm.CarrierPath = fixture.CarrierPath;
        vm.OutputPath = fixture.OutputPath;

        Assert.True(vm.ExtractCommand.CanExecute(null));

        vm.RequireEncryption = true;

        Assert.False(vm.ExtractCommand.CanExecute(null));
        Assert.Contains(vm.GetErrors(nameof(ExtractViewModel.OutputPath)).Cast<string>(), static x => x.Contains("password source", StringComparison.OrdinalIgnoreCase));

        vm.Password = "secret";

        Assert.True(vm.ExtractCommand.CanExecute(null));
    }

    private static EmbedViewModel CreateEmbedViewModel()
    {
        return new EmbedViewModel(
            new NoOpEmbedService(),
            new NoOpCapacityService(),
            new NoOpInfoService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()));
    }

    private static ExtractViewModel CreateExtractViewModel()
    {
        return new ExtractViewModel(
            new NoOpExtractService(),
            new UiOperationPolicyValidator(new OperationPolicyValidator()));
    }

    private sealed class TempFileFixture : IDisposable
    {
        public string RootPath { get; }
        public string CarrierPath { get; }
        public string PayloadPath { get; }
        public string OutputPath { get; }

        public TempFileFixture(bool createOutputFile)
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"stegoforge-wpf-validation-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);

            CarrierPath = Path.Combine(RootPath, "carrier.bin");
            PayloadPath = Path.Combine(RootPath, "payload.bin");
            OutputPath = Path.Combine(RootPath, "output.bin");

            File.WriteAllBytes(CarrierPath, [1, 2, 3]);
            File.WriteAllBytes(PayloadPath, [4, 5, 6]);

            if (createOutputFile)
            {
                File.WriteAllBytes(OutputPath, [7]);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
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
