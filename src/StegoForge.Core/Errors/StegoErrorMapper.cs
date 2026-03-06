namespace StegoForge.Core.Errors;

public static class StegoErrorMapper
{
    private const string UnknownFailureMessage = "An internal processing failure occurred. Retry with verbose diagnostics or contact support.";

    public static StegoError FromException(Exception exception)
    {
        return exception switch
        {
            FileNotFoundStegoException typed => StegoError.FileNotFound(typed.Message),
            InvalidArgumentsException typed => StegoError.InvalidArguments(typed.Message),
            CorruptedDataException typed => StegoError.CorruptedData(typed.Message),
            UnsupportedFormatException typed => StegoError.UnsupportedFormat(typed.Message),
            InvalidPayloadException typed => StegoError.InvalidPayload(typed.Message),
            InvalidHeaderException typed => StegoError.InvalidHeader(typed.Message),
            WrongPasswordException typed => StegoError.WrongPassword(typed.Message),
            InsufficientCapacityException typed => StegoError.InsufficientCapacity(typed.Message),
            OutputExistsException typed => StegoError.OutputAlreadyExists(typed.Message),
            InternalProcessingException typed => StegoError.InternalProcessingFailure(typed.Message),
            StegoForgeException stegoException => new StegoError(stegoException.Code, stegoException.Message),
            _ => StegoError.InternalProcessingFailure(UnknownFailureMessage)
        };
    }
}
