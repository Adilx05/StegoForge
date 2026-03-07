using System.CommandLine.Invocation;
using System.Text.Json;
using StegoForge.Core.Models;

namespace StegoForge.Cli.Commands;

internal static class CommandExecution
{
    public static async Task<int> ExecuteAsync(Func<CancellationToken, Task<object>> action, bool json, InvocationContext context)
    {
        try
        {
            var result = await action(context.GetCancellationToken()).ConfigureAwait(false);

            if (json)
            {
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions)).ConfigureAwait(false);
            }

            return 0;
        }
        catch (Exception exception)
        {
            var failure = CliErrorContract.CreateFailureFromException(exception);
            await Console.Error.WriteLineAsync(failure.Message).ConfigureAwait(false);
            return failure.ExitCode;
        }
    }

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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
