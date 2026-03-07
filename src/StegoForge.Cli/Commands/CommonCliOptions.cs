using System.CommandLine;
using System.CommandLine.Parsing;

namespace StegoForge.Cli.Commands;

internal static class CommonCliOptions
{
    public static Option<string> CarrierPathOption()
    {
        var option = new Option<string>("--carrier", "Path to the carrier file.");
        option.AddAlias("-c");
        option.Required = true;
        option.Validators.Add(result => ValidateExistingFilePath(result, "Carrier file"));
        return option;
    }

    public static Option<string> PayloadPathOption()
    {
        var option = new Option<string>("--payload", "Path to the payload file.");
        option.AddAlias("-p");
        option.Required = true;
        option.Validators.Add(result => ValidateExistingFilePath(result, "Payload file"));
        return option;
    }

    public static Option<string> OutputPathOption()
    {
        var option = new Option<string>("--out", "Path to the output file or output directory.");
        option.AddAlias("-o");
        option.Required = true;
        return option;
    }

    public static Option<string> EncryptOption()
    {
        var option = new Option<string>("--encrypt", "Encryption mode: none|optional|required.");
        option.Validators.Add(result => ValidateAllowedValue(result, ["none", "optional", "required"]));
        return option;
    }

    public static Option<string> CompressOption()
    {
        var option = new Option<string>("--compress", "Compression mode: off|auto|on.");
        option.Validators.Add(result => ValidateAllowedValue(result, ["off", "none", "disabled", "auto", "automatic", "on", "enabled"]));
        return option;
    }

    public static Option<string?> PasswordOption()
        => new("--password", "Password value used for encryption/decryption.");

    public static Option<bool> JsonOption()
        => new("--json", "Emit machine-readable JSON output.");

    public static Option<bool> QuietOption()
    {
        var option = new Option<bool>("--quiet", "Suppress non-error output.");
        option.AddAlias("-q");
        return option;
    }

    public static Option<bool> VerboseOption()
    {
        var option = new Option<bool>("--verbose", "Emit detailed output.");
        option.AddAlias("-v");
        return option;
    }

    private static void ValidateExistingFilePath(OptionResult result, string label)
    {
        if (result.Tokens.Count == 0)
        {
            return;
        }

        var value = result.Tokens[0].Value;
        if (!File.Exists(value))
        {
            result.AddError($"{label} does not exist: '{value}'.");
        }
    }

    private static void ValidateAllowedValue(OptionResult result, IReadOnlyCollection<string> allowedValues)
    {
        if (result.Tokens.Count == 0)
        {
            return;
        }

        var value = result.Tokens[0].Value;
        if (!allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            result.AddError($"Invalid value '{value}' for option '{result.Option.Name}'. Allowed values: {string.Join("|", allowedValues)}.");
        }
    }
}
