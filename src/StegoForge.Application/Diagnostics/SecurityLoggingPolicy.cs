using System.Text.RegularExpressions;

namespace StegoForge.Application.Diagnostics;

public static partial class SecurityLoggingPolicy
{
    public const string RedactionToken = "<redacted>";

    public static IReadOnlySet<string> SafeFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "operationType",
        "carrierFormat",
        "errorCode",
        "correlationId",
        "timestampUtc"
    };

    public static IReadOnlySet<string> RedactedFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passphrase",
        "plaintextPayloadBytes",
        "payloadBytes",
        "derivedKey",
        "kdfOutput",
        "encryptionKey"
    };

    public static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Operation failed. See diagnostics context for follow-up.";
        }

        var sanitized = message;
        sanitized = SensitiveAssignmentRegex().Replace(sanitized, static m => $"{m.Groups[1].Value}={RedactionToken}");
        sanitized = ByteArrayRegex().Replace(sanitized, RedactionToken);
        sanitized = DerivedKeyRegex().Replace(sanitized, RedactionToken);

        return sanitized;
    }

    [GeneratedRegex(@"(?i)\b(password|passphrase|plaintext(?:[_\s-]?payload)?(?:[_\s-]?bytes)?|payload(?:[_\s-]?bytes)?|derived(?:[_\s-]?key)?|kdf(?:[_\s-]?output)?|encryption(?:[_\s-]?key)?)\b\s*[:=]\s*([^,;\s]+)")]
    private static partial Regex SensitiveAssignmentRegex();

    [GeneratedRegex(@"\[(?:\s*\d+\s*,?)+\s*\]")]
    private static partial Regex ByteArrayRegex();

    [GeneratedRegex(@"(?i)(?:0x)?[a-f0-9]{32,}")]
    private static partial Regex DerivedKeyRegex();
}
