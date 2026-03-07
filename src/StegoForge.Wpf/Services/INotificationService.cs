namespace StegoForge.Wpf.Services;

public interface INotificationService
{
    void ShowError(string title, string message);

    bool Confirm(string title, string message);
}
