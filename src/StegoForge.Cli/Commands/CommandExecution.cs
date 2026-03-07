using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Cli.Output;

namespace StegoForge.Cli.Commands;

internal static class CommandExecution
{
    public static async Task<int> ExecuteAsync(Func<CancellationToken, Task<ICommandOutput>> action, bool json, CancellationToken cancellationToken = default)
    {
        var formatter = CreateOutputFormatter(json);

        try
        {
            var result = await action(cancellationToken).ConfigureAwait(false);

            await formatter.WriteSuccessAsync(result, cancellationToken).ConfigureAwait(false);

            return 0;
        }
        catch (ArgumentException exception)
        {
            var error = StegoError.InvalidArguments(exception.Message);
            return await WriteFailureAsync(formatter, error, cancellationToken).ConfigureAwait(false);
        }
        catch (StegoForgeException exception)
        {
            var error = StegoErrorMapper.FromException(exception);
            return await WriteFailureAsync(formatter, error, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            var error = StegoError.InternalProcessingFailure("An internal processing failure occurred. Retry with verbose diagnostics or contact support.");
            return await WriteFailureAsync(formatter, error, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<int> WriteFailureAsync(IOutputFormatter formatter, StegoError error, CancellationToken cancellationToken)
    {
        var failure = new CliCommandFailure(CliErrorContract.GetExitCode(error.Code), error);
        await formatter.WriteFailureAsync(failure, cancellationToken).ConfigureAwait(false);
        return failure.ExitCode;
    }

    private static IOutputFormatter CreateOutputFormatter(bool json)
        => json
            ? new JsonOutputFormatter(Console.Out, Console.Error)
            : new TextOutputFormatter(Console.Out, Console.Error);

    public static ProcessingOptions BuildProcessingOptions(string compress, string encrypt, bool quiet, bool verbose)
    {
        if (quiet && verbose)
        {
            throw new ArgumentException("--quiet and --verbose cannot be used together.");
        }

        return new ProcessingOptions(
            compressionMode: ParseCompressionMode(compress),
            encryptionMode: ParseEncryptionMode(encrypt),
            verbosityMode: ResolveVerbosity(quiet, verbose));
    }

    public static PasswordOptions BuildPasswordOptions(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return PasswordOptions.Optional;
        }

        return new PasswordOptions(
            requirement: PasswordRequirement.Required,
            sourceHint: PasswordSourceHint.Prompt,
            sourceReference: password);
    }

    private static CompressionMode ParseCompressionMode(string value)
        => value.ToLowerInvariant() switch
        {
            "off" or "none" or "disabled" => CompressionMode.Disabled,
            "auto" or "automatic" => CompressionMode.Automatic,
            "on" or "enabled" => CompressionMode.Enabled,
            _ => throw new ArgumentException("Invalid --compress value. Use off|auto|on.")
        };

    private static EncryptionMode ParseEncryptionMode(string value)
        => value.ToLowerInvariant() switch
        {
            "off" or "none" => EncryptionMode.None,
            "optional" => EncryptionMode.Optional,
            "required" => EncryptionMode.Required,
            _ => throw new ArgumentException("Invalid --encrypt value. Use none|optional|required.")
        };

    private static VerbosityMode ResolveVerbosity(bool quiet, bool verbose)
        => quiet ? VerbosityMode.Quiet : verbose ? VerbosityMode.Detailed : VerbosityMode.Normal;

}
