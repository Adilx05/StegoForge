using StegoForge.Core.Errors;

namespace StegoForge.Cli.Output;

internal interface IOutputFormatter
{
    Task WriteSuccessAsync(ICommandOutput output, CancellationToken cancellationToken = default);

    Task WriteFailureAsync(CliCommandFailure failure, CancellationToken cancellationToken = default);
}

internal interface ICommandOutput
{
    IReadOnlyList<string> ToTextLines();
}

internal sealed record CliCommandFailure(int ExitCode, StegoError Error);
