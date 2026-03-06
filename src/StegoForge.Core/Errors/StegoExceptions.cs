namespace StegoForge.Core.Errors;

public sealed class FileNotFoundStegoException(string path)
    : StegoForgeException(StegoErrorCode.FileNotFound, $"File not found: {path}")
{
    public string Path { get; } = path;
}

public sealed class InvalidArgumentsException(string message)
    : StegoForgeException(StegoErrorCode.InvalidArguments, message);

public sealed class CorruptedDataException(string message)
    : StegoForgeException(StegoErrorCode.CorruptedData, message);

public sealed class UnsupportedFormatException(string message)
    : StegoForgeException(StegoErrorCode.UnsupportedFormat, message);

public sealed class WrongPasswordException(string message)
    : StegoForgeException(StegoErrorCode.WrongPassword, message);

public sealed class InvalidPayloadException(string message)
    : StegoForgeException(StegoErrorCode.InvalidPayload, message);

public sealed class InvalidHeaderException(string message)
    : StegoForgeException(StegoErrorCode.InvalidHeader, message);

public sealed class InsufficientCapacityException(long requiredBytes, long availableBytes)
    : StegoForgeException(
        StegoErrorCode.InsufficientCapacity,
        $"Insufficient capacity. Required {requiredBytes} bytes, available {availableBytes} bytes.")
{
    public long RequiredBytes { get; } = requiredBytes;

    public long AvailableBytes { get; } = availableBytes;
}

public sealed class OutputExistsException(string path)
    : StegoForgeException(StegoErrorCode.OutputAlreadyExists, $"Output path already exists: {path}")
{
    public string Path { get; } = path;
}

public sealed class InternalProcessingException(string message, Exception? innerException = null)
    : StegoForgeException(StegoErrorCode.InternalProcessingFailure, message, innerException);
