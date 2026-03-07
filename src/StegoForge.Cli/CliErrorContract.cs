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
    {
        ArgumentNullException.ThrowIfNull(error);
        return $"ERROR [{error.Code}] {error.Message}";
    }

    public static (int ExitCode, string Message) CreateFailure(StegoError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return (GetExitCode(error.Code), FormatError(error));
    }

    public static (int ExitCode, string Message) CreateInvalidArgumentsFailure(string message)
        => CreateFailure(StegoError.InvalidArguments(message));

    public static (int ExitCode, string Message) CreateUnexpectedFailure()
        => CreateFailure(StegoError.InternalProcessingFailure("An internal processing failure occurred. Retry with verbose diagnostics or contact support."));

    public static (int ExitCode, string Message) CreateFailureFromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var error = StegoErrorMapper.FromException(exception);
        return CreateFailure(error);
    }
}
