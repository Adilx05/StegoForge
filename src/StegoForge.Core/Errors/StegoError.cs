namespace StegoForge.Core.Errors;

public sealed record StegoError(StegoErrorCode Code, string Message)
{
    public static StegoError FileNotFound(string message) => new(StegoErrorCode.FileNotFound, message);

    public static StegoError InvalidArguments(string message) => new(StegoErrorCode.InvalidArguments, message);

    public static StegoError CorruptedData(string message) => new(StegoErrorCode.CorruptedData, message);

    public static StegoError UnsupportedFormat(string message) => new(StegoErrorCode.UnsupportedFormat, message);

    public static StegoError WrongPassword(string message) => new(StegoErrorCode.WrongPassword, message);

    public static StegoError InvalidPayload(string message) => new(StegoErrorCode.InvalidPayload, message);

    public static StegoError InvalidHeader(string message) => new(StegoErrorCode.InvalidHeader, message);

    public static StegoError InsufficientCapacity(string message) => new(StegoErrorCode.InsufficientCapacity, message);

    public static StegoError OutputAlreadyExists(string message) => new(StegoErrorCode.OutputAlreadyExists, message);

    public static StegoError InternalProcessingFailure(string message) => new(StegoErrorCode.InternalProcessingFailure, message);
}
