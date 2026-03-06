namespace StegoForge.Core.Errors;

public static class StegoErrorMapper
{
    public static StegoError FromException(Exception exception)
    {
        return exception switch
        {
            StegoForgeException stegoException => new StegoError(stegoException.Code, stegoException.Message),
            _ => StegoError.InternalProcessingFailure(exception.Message)
        };
    }
}
