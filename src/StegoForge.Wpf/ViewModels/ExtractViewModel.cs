using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Wpf.Services;
using StegoForge.Wpf.Validation;

namespace StegoForge.Wpf.ViewModels;

public sealed class ExtractViewModel : OperationViewModelBase
{
    private readonly IExtractService _extractService;
    private readonly UiOperationPolicyValidator _validationService;
    private readonly INotificationService _notificationService;
    private readonly AsyncRelayCommand _extractCommand;

    private string _carrierPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _password = string.Empty;
    private bool _requireEncryption;
    private bool _allowOverwrite;
    private string _resultMessage = "Extract workflow is ready.";

    public ExtractViewModel(
        IExtractService extractService,
        UiOperationPolicyValidator validationService,
        INotificationService notificationService)
    {
        ArgumentNullException.ThrowIfNull(extractService);
        ArgumentNullException.ThrowIfNull(validationService);
        ArgumentNullException.ThrowIfNull(notificationService);

        _extractService = extractService;
        _validationService = validationService;
        _notificationService = notificationService;

        _extractCommand = new AsyncRelayCommand(ExtractAsync, () => !HasErrors && !IsBusy);
        ExtractCommand = _extractCommand;

        ErrorsChanged += (_, _) => _extractCommand.RaiseCanExecuteChanged();
        PropertyChanged += (_, args) =>
        {
            if (string.Equals(args.PropertyName, nameof(IsBusy), StringComparison.Ordinal))
            {
                _extractCommand.RaiseCanExecuteChanged();
            }
        };

        Validate();
    }

    public event EventHandler<string>? StatusChanged;

    public ICommand ExtractCommand { get; }

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

    private async Task ExtractAsync()
    {
        ResetOperationState("Extracting payload...");
        if (!_notificationService.Confirm("Confirm extract", "Proceed with extract operation?"))
        {
            StatusMessage = "Extract cancelled.";
            ProgressText = "Cancelled";
            ResultMessage = "Extract cancelled by user.";
            StatusChanged?.Invoke(this, StatusMessage);
            return;
        }

        IsBusy = true;
        ProgressText = "Preparing request";
        StatusChanged?.Invoke(this, StatusMessage);

        try
        {
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

            ProgressText = "Running extraction";
            var request = new ExtractRequest(CarrierPath, OutputPath, processingOptions, passwordOptions);
            var response = await _extractService.ExtractAsync(request).ConfigureAwait(true);

            ResultMessage = $"Extract complete: {response.PayloadSizeBytes} B to '{response.ResolvedOutputPath}', format={response.CarrierFormatId}.";
            StatusMessage = "Extract completed.";
            ProgressText = "Completed";
            StatusChanged?.Invoke(this, StatusMessage);
        }
        catch (Exception ex)
        {
            var mapped = StegoErrorMapper.FromException(ex);
            SetMappedError(mapped);
            ResultMessage = $"Extract failed ({mapped.Code}): {mapped.Message}";
            StatusMessage = "Extract failed.";
            ProgressText = "Failed";
            _notificationService.ShowError("Extract failed", ResultMessage);
            StatusChanged?.Invoke(this, StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Validate()
    {
        var result = _validationService.ValidateExtract(CarrierPath, OutputPath, RequireEncryption, Password, AllowOverwrite);
        SetErrors(nameof(CarrierPath), result.Issues.Where(static x => x.PropertyName == nameof(CarrierPath)).Select(static x => x.Message));
        SetErrors(nameof(OutputPath), result.Issues.Where(static x => x.PropertyName == nameof(OutputPath)).Select(static x => x.Message));
        SetErrors(nameof(Password), result.Issues.Where(static x => x.PropertyName == nameof(Password)).Select(static x => x.Message));
    }
}
