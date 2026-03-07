using System.Text.Json;
using StegoForge.Core.Errors;

namespace StegoForge.Cli.Output;

internal sealed class JsonOutputFormatter(TextWriter stdout, TextWriter stderr) : IOutputFormatter
{
    public async Task WriteSuccessAsync(ICommandOutput output, CancellationToken cancellationToken = default)
    {
        await stdout.WriteLineAsync(JsonSerializer.Serialize(output, JsonOptions)).ConfigureAwait(false);
    }

    public async Task WriteFailureAsync(CliCommandFailure failure, CancellationToken cancellationToken = default)
    {
        var payload = new CliErrorOutput(
            Type: "error",
            ExitCode: failure.ExitCode,
            Code: failure.Error.Code.ToString(),
            Message: failure.Error.Message);

        await stderr.WriteLineAsync(JsonSerializer.Serialize(payload, JsonOptions)).ConfigureAwait(false);
    }

    private sealed record CliErrorOutput(string Type, int ExitCode, string Code, string Message);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
