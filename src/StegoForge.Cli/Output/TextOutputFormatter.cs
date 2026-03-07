using StegoForge.Cli;

namespace StegoForge.Cli.Output;

internal sealed class TextOutputFormatter(TextWriter stdout, TextWriter stderr) : IOutputFormatter
{
    public async Task WriteSuccessAsync(ICommandOutput output, CancellationToken cancellationToken = default)
    {
        foreach (var line in output.ToTextLines())
        {
            await stdout.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    public async Task WriteFailureAsync(CliCommandFailure failure, CancellationToken cancellationToken = default)
    {
        await stderr.WriteLineAsync(CliErrorContract.FormatError(failure.Error)).ConfigureAwait(false);
    }
}
