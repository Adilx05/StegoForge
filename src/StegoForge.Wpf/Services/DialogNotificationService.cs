using System.Windows;
using StegoForge.Application.Diagnostics;

namespace StegoForge.Wpf.Services;

public sealed class DialogNotificationService : INotificationService
{
    public void ShowError(string title, string message)
    {
        var safeMessage = SecurityLoggingPolicy.SanitizeMessage(message);
        MessageBox.Show(safeMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool Confirm(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
