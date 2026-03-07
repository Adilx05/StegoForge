namespace StegoForge.Wpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _statusMessage = "Ready.";

    public MainWindowViewModel(EmbedViewModel embed, ExtractViewModel extract)
    {
        Embed = embed;
        Extract = extract;

        Embed.StatusChanged += OnStatusChanged;
        Extract.StatusChanged += OnStatusChanged;
    }

    public EmbedViewModel Embed { get; }

    public ExtractViewModel Extract { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private void OnStatusChanged(object? sender, string message)
    {
        StatusMessage = message;
    }
}
