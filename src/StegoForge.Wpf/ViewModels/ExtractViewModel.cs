using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Wpf.Validation;

namespace StegoForge.Wpf.ViewModels;

public sealed class ExtractViewModel : ViewModelBase
{
    private readonly IExtractService _extractService;
    private readonly UiOperationPolicyValidator _validationService;
    private readonly AsyncRelayCommand _extractCommand;

    private string _carrierPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _password = string.Empty;
    private bool _requireEncryption;
    private bool _allowOverwrite;
    private string _resultMessage = "Extract workflow is ready.";

    public ExtractViewModel(IExtractService extractService, UiOperationPolicyValidator validationService)
    {
        ArgumentNullException.ThrowIfNull(extractService);
        ArgumentNullException.ThrowIfNull(validationService);

        _extractService = extractService;
        _validationService = validationService;

        _extractCommand = new AsyncRelayCommand(ExtractAsync, () => !HasErrors);
        ExtractCommand = _extractCommand;

        ErrorsChanged += (_, _) => _extractCommand.RaiseCanExecuteChanged();
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

            var request = new ExtractRequest(CarrierPath, OutputPath, processingOptions, passwordOptions);
            var response = await _extractService.ExtractAsync(request).ConfigureAwait(true);

            ResultMessage = $"Extract complete: {response.PayloadSizeBytes} B to '{response.ResolvedOutputPath}', format={response.CarrierFormatId}.";
            StatusChanged?.Invoke(this, "Extract completed.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Extract failed: {ex.Message}";
            StatusChanged?.Invoke(this, "Extract failed.");
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
