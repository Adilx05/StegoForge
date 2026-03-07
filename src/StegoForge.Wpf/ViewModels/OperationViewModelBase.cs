using StegoForge.Application.Diagnostics;
using StegoForge.Core.Errors;

namespace StegoForge.Wpf.ViewModels;

public abstract class OperationViewModelBase : ViewModelBase
{
    private bool _isBusy;
    private string _progressText = "Idle";
    private string _statusMessage = "Ready.";
    private string? _lastErrorCode;
    private string? _lastErrorMessage;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public string ProgressText
    {
        get => _progressText;
        protected set => SetProperty(ref _progressText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        protected set => SetProperty(ref _statusMessage, value);
    }

    public string? LastErrorCode
    {
        get => _lastErrorCode;
        protected set => SetProperty(ref _lastErrorCode, value);
    }

    public string? LastErrorMessage
    {
        get => _lastErrorMessage;
        protected set => SetProperty(ref _lastErrorMessage, value);
    }

    protected void ResetOperationState(string readyMessage)
    {
        LastErrorCode = null;
        LastErrorMessage = null;
        StatusMessage = readyMessage;
        ProgressText = "Idle";
    }

    protected SanitizedErrorDiagnostics SetMappedError(StegoError error, DiagnosticContext diagnostics)
    {
        var sanitized = SanitizedErrorDiagnostics.From(error, diagnostics);
        LastErrorCode = sanitized.ErrorCode;
        LastErrorMessage = sanitized.ToWpfMessage();
        return sanitized;
    }
}
