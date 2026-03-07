using System;
using System.Threading.Tasks;
using System.Windows.Input;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Wpf.ViewModels;

public sealed class ExtractViewModel : ViewModelBase
{
    private readonly IExtractService _extractService;

    private string _carrierPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _password = string.Empty;
    private string _resultMessage = "Extract workflow is ready.";

    public ExtractViewModel(IExtractService extractService)
    {
        _extractService = extractService;
        ExtractCommand = new AsyncRelayCommand(ExtractAsync);
    }

    public event EventHandler<string>? StatusChanged;

    public ICommand ExtractCommand { get; }

    public string CarrierPath
    {
        get => _carrierPath;
        set => SetProperty(ref _carrierPath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
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
                ? PasswordOptions.Optional
                : new PasswordOptions(PasswordRequirement.Required, PasswordSourceHint.Prompt, Password);

            var request = new ExtractRequest(CarrierPath, OutputPath, passwordOptions: passwordOptions);
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
}
