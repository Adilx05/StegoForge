using StegoForge.Core.Errors;

namespace StegoForge.Application.Diagnostics;

public sealed record SanitizedErrorDiagnostics(
    string OperationType,
    string CarrierFormat,
    string ErrorCode,
    string CorrelationId,
    string Message)
{
    public static SanitizedErrorDiagnostics From(StegoError error, DiagnosticContext context)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        return new SanitizedErrorDiagnostics(
            context.OperationType,
            context.CarrierFormat,
            error.Code.ToString(),
            context.CorrelationId,
            SecurityLoggingPolicy.SanitizeMessage(error.Message));
    }

    public string ToCliText()
        => $"ERROR [{ErrorCode}] {Message} (operation={OperationType}, carrierFormat={CarrierFormat}, correlationId={CorrelationId})";

    public string ToWpfMessage()
        => $"{Message}\n\nError code: {ErrorCode}\nOperation: {OperationType}\nCarrier format: {CarrierFormat}\nCorrelation ID: {CorrelationId}";
}
