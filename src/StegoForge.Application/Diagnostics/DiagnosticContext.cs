namespace StegoForge.Application.Diagnostics;

public sealed record DiagnosticContext(string OperationType, string CarrierFormat, string CorrelationId)
{
    public static DiagnosticContext Create(string operationType, string? carrierFormat)
        => new(operationType, string.IsNullOrWhiteSpace(carrierFormat) ? "unknown" : carrierFormat, Guid.NewGuid().ToString("N"));
}
