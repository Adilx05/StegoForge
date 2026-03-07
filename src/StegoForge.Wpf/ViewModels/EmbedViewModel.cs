using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;

namespace StegoForge.Wpf.ViewModels;

public sealed class EmbedViewModel : OperationViewModelBase
{
    private readonly IEmbedService _embedService;
    private readonly ICapacityService _capacityService;
    private readonly IInfoService _infoService;
    private readonly UiOperationPolicyValidator _validationService;
    private readonly INotificationService _notificationService;

    private readonly AsyncRelayCommand _checkCapacityCommand;
    private readonly AsyncRelayCommand _getInfoCommand;
    private readonly AsyncRelayCommand _embedCommand;

    private string _carrierPath = string.Empty;
    private string _payloadPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _password = string.Empty;
    private bool _requireEncryption;
    private bool _allowOverwrite;
    private string _resultMessage = "Embed workflow is ready.";

    public EmbedViewModel(
        IEmbedService embedService,
        ICapacityService capacityService,
        IInfoService infoService,
        UiOperationPolicyValidator validationService,
        INotificationService notificationService)
    {
        ArgumentNullException.ThrowIfNull(embedService);
        ArgumentNullException.ThrowIfNull(capacityService);
        ArgumentNullException.ThrowIfNull(infoService);
        ArgumentNullException.ThrowIfNull(validationService);
        ArgumentNullException.ThrowIfNull(notificationService);

        _embedService = embedService;
        _capacityService = capacityService;
        _infoService = infoService;
        _validationService = validationService;
        _notificationService = notificationService;

        _checkCapacityCommand = new AsyncRelayCommand(CheckCapacityAsync, () => !HasErrors);
        _getInfoCommand = new AsyncRelayCommand(GetInfoAsync, () => !HasErrors);
        _embedCommand = new AsyncRelayCommand(EmbedAsync, () => !HasErrors && !IsBusy);

        CheckCapacityCommand = _checkCapacityCommand;
        GetInfoCommand = _getInfoCommand;
        EmbedCommand = _embedCommand;

        ErrorsChanged += (_, _) => RaiseCommandCanExecuteChanged();
        PropertyChanged += (_, args) =>
        {
            if (string.Equals(args.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
            {
                RaiseCommandCanExecuteChanged();
            }
        };
        Validate();
    }

    public event EventHandler<string>? StatusChanged;

    public ICommand CheckCapacityCommand { get; }

    public ICommand GetInfoCommand { get; }

    public ICommand EmbedCommand { get; }

    public string CarrierPath
    {
        get => _carrierPath;
        set
        {
            if (SetProperty(ref _carrierPath, value))
            {
                Validate();
            }
        }
    }

    public string PayloadPath
    {
        get => _payloadPath;
        set
        {
            if (SetProperty(ref _payloadPath, value))
            {
                Validate();
            }
        }
    }

    public string OutputPath
    {
        get => _outputPath;
        set
        {
            if (SetProperty(ref _outputPath, value))
            {
                Validate();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                Validate();
            }
        }
    }

    public bool RequireEncryption
    {
        get => _requireEncryption;
        set
        {
            if (SetProperty(ref _requireEncryption, value))
            {
                Validate();
            }
        }
    }

    public bool AllowOverwrite
    {
        get => _allowOverwrite;
        set
        {
            if (SetProperty(ref _allowOverwrite, value))
            {
                Validate();
            }
        }
    }

    public string ResultMessage
    {
        get => _resultMessage;
        private set => SetProperty(ref _resultMessage, value);
    }

    private async Task CheckCapacityAsync()
    {
        try
        {
            var payloadLength = File.Exists(PayloadPath) ? new FileInfo(PayloadPath).Length : 0;
            var request = new CapacityRequest(CarrierPath, payloadLength);
            var response = await _capacityService.GetCapacityAsync(request).ConfigureAwait(true);

            ResultMessage = $"Capacity ({response.CarrierFormatId}): available={response.AvailableCapacityBytes} B, max={response.MaximumCapacityBytes} B, canEmbed={response.CanEmbed}";
            StatusChanged?.Invoke(this, "Capacity check completed.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Capacity check failed: {ex.Message}";
            StatusChanged?.Invoke(this, "Capacity check failed.");
        }
    }

    private async Task GetInfoAsync()
    {
        try
        {
            var request = new InfoRequest(CarrierPath);
            var response = await _infoService.GetInfoAsync(request).ConfigureAwait(true);

            ResultMessage = $"Carrier {response.FormatDetails.DisplayName} ({response.FormatId}), size={response.CarrierSizeBytes} B, embeddedData={response.EmbeddedDataPresent}";
            StatusChanged?.Invoke(this, "Carrier info loaded.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Info lookup failed: {ex.Message}";
            StatusChanged?.Invoke(this, "Carrier info failed.");
        }
    }

    private async Task EmbedAsync()
    {
        ResetOperationState("Embedding payload...");
        if (!_notificationService.Confirm("Confirm embed", "Proceed with embed operation?"))
        {
            StatusMessage = "Embed cancelled.";
            ProgressText = "Cancelled";
            ResultMessage = "Embed cancelled by user.";
            StatusChanged?.Invoke(this, StatusMessage);
            return;
        }

        IsBusy = true;
        ProgressText = "Preparing payload";
        StatusMessage = "Reading payload from disk.";
        StatusChanged?.Invoke(this, StatusMessage);

        try
        {
            var payload = File.ReadAllBytes(PayloadPath);
            ProgressText = "Submitting embed request";
            StatusMessage = "Embedding payload into carrier.";
            StatusChanged?.Invoke(this, StatusMessage);

            var passwordOptions = string.IsNullOrWhiteSpace(Password)
                ? new PasswordOptions(
                    requirement: RequireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                    sourceHint: PasswordSourceHint.None,
                    sourceReference: null)
                : new PasswordOptions(
                    requirement: RequireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                    sourceHint: PasswordSourceHint.Prompt,
                    sourceReference: Password);

            var processingOptions = new ProcessingOptions(
                encryptionMode: RequireEncryption ? EncryptionMode.Required : EncryptionMode.Optional,
                overwriteBehavior: AllowOverwrite ? OverwriteBehavior.Allow : OverwriteBehavior.Disallow);

            var request = new EmbedRequest(CarrierPath, OutputPath, payload, processingOptions, passwordOptions);
            var response = await _embedService.EmbedAsync(request).ConfigureAwait(true);

            ResultMessage = $"Embed complete: {response.BytesEmbedded} B written to '{response.OutputPath}' via {response.CarrierFormatId}.";
            StatusMessage = "Embed completed.";
            ProgressText = "Completed";
            StatusChanged?.Invoke(this, StatusMessage);
        }
        catch (Exception ex)
        {
            var mapped = StegoErrorMapper.FromException(ex);
            SetMappedError(mapped);
            ResultMessage = $"Embed failed ({mapped.Code}): {mapped.Message}";
            StatusMessage = "Embed failed.";
            ProgressText = "Failed";
            _notificationService.ShowError("Embed failed", ResultMessage);
            StatusChanged?.Invoke(this, StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Validate()
    {
        var result = _validationService.ValidateEmbed(CarrierPath, PayloadPath, OutputPath, RequireEncryption, Password, AllowOverwrite);
        ApplyValidationResult(result);
    }

    private void ApplyValidationResult(OperationValidationResult result)
    {
        SetErrors(nameof(CarrierPath), result.Issues.Where(static x => x.PropertyName == nameof(CarrierPath)).Select(static x => x.Message));
        SetErrors(nameof(PayloadPath), result.Issues.Where(static x => x.PropertyName == nameof(PayloadPath)).Select(static x => x.Message));
        SetErrors(nameof(OutputPath), result.Issues.Where(static x => x.PropertyName == nameof(OutputPath)).Select(static x => x.Message));
        SetErrors(nameof(Password), result.Issues.Where(static x => x.PropertyName == nameof(Password)).Select(static x => x.Message));
    }

    private void RaiseCommandCanExecuteChanged()
    {
        _checkCapacityCommand.RaiseCanExecuteChanged();
        _getInfoCommand.RaiseCanExecuteChanged();
        _embedCommand.RaiseCanExecuteChanged();
    }
}
