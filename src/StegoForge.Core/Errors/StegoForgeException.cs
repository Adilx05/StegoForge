namespace StegoForge.Core.Errors;

public abstract class StegoForgeException : Exception
{
    protected StegoForgeException(StegoErrorCode code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public StegoErrorCode Code { get; }
}
