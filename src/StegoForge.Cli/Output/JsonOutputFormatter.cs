using System.Text.Json;

namespace StegoForge.Cli.Output;

internal sealed class JsonOutputFormatter(TextWriter stdout, TextWriter stderr) : IOutputFormatter
{
    public async Task WriteSuccessAsync(ICommandOutput output, CancellationToken cancellationToken = default)
    {
        await stdout.WriteLineAsync(JsonSerializer.Serialize(output, output.GetType(), JsonOptions)).ConfigureAwait(false);
    }

    public async Task WriteFailureAsync(CliCommandFailure failure, CancellationToken cancellationToken = default)
    {
        var payload = new CliErrorOutput(
            Type: "error",
            ExitCode: failure.ExitCode,
            Code: failure.Diagnostics.ErrorCode,
            Message: failure.Diagnostics.Message,
            OperationType: failure.Diagnostics.OperationType,
            CarrierFormat: failure.Diagnostics.CarrierFormat,
            CorrelationId: failure.Diagnostics.CorrelationId);

        await stderr.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions)).ConfigureAwait(false);
    }

    private sealed record CliErrorOutput(
        string Type,
        int ExitCode,
        string Code,
        string Message,
        string OperationType,
        string CarrierFormat,
        string CorrelationId);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
