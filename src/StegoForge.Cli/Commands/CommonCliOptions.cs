using System.CommandLine;

namespace StegoForge.Cli.Commands;

internal static class CommonCliOptions
{
    public static Option<string> CarrierPathOption() =>
        new(["--carrier", "-c"], "Path to the carrier file.") { IsRequired = true };

    public static Option<string> PayloadPathOption() =>
        new(["--payload", "-p"], "Path to the payload file.") { IsRequired = true };

    public static Option<string> OutputPathOption() =>
        new(["--out", "-o"], "Path to the output file or output directory.") { IsRequired = true };

    public static Option<string> EncryptOption() =>
        new("--encrypt", () => "optional", "Encryption mode: none|optional|required.");

    public static Option<string> CompressOption() =>
        new("--compress", () => "auto", "Compression mode: off|auto|on.");

    public static Option<string?> PasswordOption() =>
        new(["--password"], "Password value used for encryption/decryption.");

    public static Option<bool> JsonOption() =>
        new(["--json"], "Emit machine-readable JSON output.");

    public static Option<bool> QuietOption() =>
        new(["--quiet", "-q"], "Suppress non-error output.");

    public static Option<bool> VerboseOption() =>
        new(["--verbose", "-v"], "Emit detailed output.");
}
