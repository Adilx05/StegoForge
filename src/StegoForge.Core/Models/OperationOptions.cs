namespace StegoForge.Core.Models;

public enum CompressionMode
{
    Disabled,
    Automatic,
    Enabled
}

public enum EncryptionMode
{
    None,
    Optional,
    Required
}

public enum OverwriteBehavior
{
    Disallow,
    Allow
}

public enum VerbosityMode
{
    Quiet,
    Normal,
    Detailed
}

public enum PasswordRequirement
{
    Optional,
    Required
}

public enum PasswordSourceHint
{
    None,
    Prompt,
    EnvironmentVariable,
    SecureStore
}

public sealed record ProcessingOptions
{
    public CompressionMode CompressionMode { get; }
    public int CompressionLevel { get; }
    public EncryptionMode EncryptionMode { get; }
    public OverwriteBehavior OverwriteBehavior { get; }
    public VerbosityMode VerbosityMode { get; }
    public EncryptionOptions EncryptionOptions { get; }

    public static ProcessingOptions Default { get; } = new();

    public ProcessingOptions(
        CompressionMode compressionMode = CompressionMode.Automatic,
        int compressionLevel = 5,
        EncryptionMode encryptionMode = EncryptionMode.Optional,
        OverwriteBehavior overwriteBehavior = OverwriteBehavior.Disallow,
        VerbosityMode verbosityMode = VerbosityMode.Normal,
        EncryptionOptions? encryptionOptions = null)
    {
        if (compressionLevel is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(compressionLevel), "Compression level must be in range 0-9.");
        }

        CompressionMode = compressionMode;
        CompressionLevel = compressionLevel;
        EncryptionMode = encryptionMode;
        OverwriteBehavior = overwriteBehavior;
        VerbosityMode = verbosityMode;
        EncryptionOptions = encryptionOptions ?? EncryptionOptions.Default;
    }
}

public sealed record PasswordOptions
{
    public PasswordRequirement Requirement { get; }
    public PasswordSourceHint SourceHint { get; }
    public string? SourceReference { get; }

    public static PasswordOptions Optional { get; } = new();

    public PasswordOptions(
        PasswordRequirement requirement = PasswordRequirement.Optional,
        PasswordSourceHint sourceHint = PasswordSourceHint.None,
        string? sourceReference = null)
    {
        if (sourceReference is not null && string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new ArgumentException("Source reference cannot be empty when provided.", nameof(sourceReference));
        }

        Requirement = requirement;
        SourceHint = sourceHint;
        SourceReference = sourceReference;
    }
}

public sealed record OperationDiagnostics
{
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Notes { get; }
    public TimeSpan Duration { get; }
    public string? AlgorithmIdentifier { get; }
    public string? ProviderIdentifier { get; }

    public static OperationDiagnostics Empty { get; } = new([], [], TimeSpan.Zero);

    public OperationDiagnostics(
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? notes = null,
        TimeSpan duration = default,
        string? algorithmIdentifier = null,
        string? providerIdentifier = null)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");
        }

        Warnings = warnings ?? [];
        Notes = notes ?? [];
        Duration = duration;
        AlgorithmIdentifier = string.IsNullOrWhiteSpace(algorithmIdentifier) ? null : algorithmIdentifier;
        ProviderIdentifier = string.IsNullOrWhiteSpace(providerIdentifier) ? null : providerIdentifier;
    }
}
