namespace StegoForge.Wpf.Services;

public interface IFileDialogService
{
    string? SelectCarrierPath(string? initialPath = null);

    string? SelectPayloadPath(string? initialPath = null);

    string? SelectEmbedOutputPath(string? initialPath = null);

    string? SelectExtractOutputPath(string? initialPath = null);
}
