using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Wpf.ViewModels;

public sealed class EmbedViewModel : ViewModelBase
{
    private readonly IEmbedService _embedService;
    private readonly ICapacityService _capacityService;
    private readonly IInfoService _infoService;

    private string _carrierPath = string.Empty;
    private string _payloadPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _resultMessage = "Embed workflow is ready.";

    public EmbedViewModel(IEmbedService embedService, ICapacityService capacityService, IInfoService infoService)
    {
        _embedService = embedService;
        _capacityService = capacityService;
        _infoService = infoService;

        CheckCapacityCommand = new AsyncRelayCommand(CheckCapacityAsync);
        GetInfoCommand = new AsyncRelayCommand(GetInfoAsync);
        EmbedCommand = new AsyncRelayCommand(EmbedAsync);
    }

    public event EventHandler<string>? StatusChanged;

    public ICommand CheckCapacityCommand { get; }

    public ICommand GetInfoCommand { get; }

    public ICommand EmbedCommand { get; }

    public string CarrierPath
    {
        get => _carrierPath;
        set => SetProperty(ref _carrierPath, value);
    }

    public string PayloadPath
    {
        get => _payloadPath;
        set => SetProperty(ref _payloadPath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
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
        try
        {
            var payload = File.ReadAllBytes(PayloadPath);
            var request = new EmbedRequest(CarrierPath, OutputPath, payload);
            var response = await _embedService.EmbedAsync(request).ConfigureAwait(true);

            ResultMessage = $"Embed complete: {response.BytesEmbedded} B written to '{response.OutputPath}' via {response.CarrierFormatId}.";
            StatusChanged?.Invoke(this, "Embed completed.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Embed failed: {ex.Message}";
            StatusChanged?.Invoke(this, "Embed failed.");
        }
    }
}
