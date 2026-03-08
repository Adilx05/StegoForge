using StegoForge.Application.Diagnostics;
using StegoForge.Core.Errors;

namespace StegoForge.Cli;

public static class CliErrorContract
{
    public static int GetExitCode(StegoErrorCode code)
    {
        return code switch
        {
            StegoErrorCode.FileNotFound => 2,
            StegoErrorCode.InvalidArguments => 3,
            StegoErrorCode.CorruptedData => 4,
            StegoErrorCode.UnsupportedFormat => 5,
            StegoErrorCode.InvalidPayload => 6,
            StegoErrorCode.InvalidHeader => 7,
            StegoErrorCode.WrongPassword => 8,
            StegoErrorCode.InsufficientCapacity => 9,
            StegoErrorCode.OutputAlreadyExists => 10,
            _ => 1
        };
    }


    public static string FormatError(StegoError error)
        => FormatError(error, DiagnosticContext.Create("unknown", "unknown"));

    public static string FormatError(StegoError error, DiagnosticContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return SanitizedErrorDiagnostics.From(error, diagnostics).ToCliText();
    }

    public static (int ExitCode, string Message) CreateFailure(StegoError error)
        => CreateFailure(error, DiagnosticContext.Create("unknown", "unknown"));

    public static (int ExitCode, string Message) CreateFailure(StegoError error, DiagnosticContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(error);
        return (GetExitCode(error.Code), FormatError(error, diagnostics));
    }

    public static (int ExitCode, string Message) CreateInvalidArgumentsFailure(string message)
        => CreateInvalidArgumentsFailure(message, DiagnosticContext.Create("unknown", "unknown"));

    public static (int ExitCode, string Message) CreateInvalidArgumentsFailure(string message, DiagnosticContext diagnostics)
        => CreateFailure(StegoError.InvalidArguments(message), diagnostics);

    public static (int ExitCode, string Message) CreateUnexpectedFailure()
        => CreateUnexpectedFailure(DiagnosticContext.Create("unknown", "unknown"));

    public static (int ExitCode, string Message) CreateUnexpectedFailure(DiagnosticContext diagnostics)
        => CreateFailure(StegoError.InternalProcessingFailure("An internal processing failure occurred. Retry with verbose diagnostics or contact support."), diagnostics);

    public static (int ExitCode, string Message) CreateFailureFromException(Exception exception)
        => CreateFailureFromException(exception, DiagnosticContext.Create("unknown", "unknown"));

    public static (int ExitCode, string Message) CreateFailureFromException(Exception exception, DiagnosticContext diagnostics)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var error = StegoErrorMapper.FromException(exception);
        return CreateFailure(error, diagnostics);
    }
}
