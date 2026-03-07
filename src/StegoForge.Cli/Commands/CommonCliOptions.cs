using System.CommandLine;

namespace StegoForge.Cli.Commands;

internal static class CommonCliOptions
{
    public static Option<string> CarrierPathOption()
    {
        var option = new Option<string>("--carrier", "Path to the carrier file.");
        option.AddAlias("-c");
        return option;
    }

    public static Option<string> PayloadPathOption()
    {
        var option = new Option<string>("--payload", "Path to the payload file.");
        option.AddAlias("-p");
        return option;
    }

    public static Option<string> OutputPathOption()
    {
        var option = new Option<string>("--out", "Path to the output file or output directory.");
        option.AddAlias("-o");
        return option;
    }

    public static Option<string> EncryptOption()
        => new("--encrypt", "Encryption mode: none|optional|required.");

    public static Option<string> CompressOption()
        => new("--compress", "Compression mode: off|auto|on.");

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
}
