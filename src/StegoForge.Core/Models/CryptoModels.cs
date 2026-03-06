namespace StegoForge.Core.Models;

public enum KdfSaltLengthPolicy
{
    Fixed,
    ProviderDefault,
    Adaptive
}

public sealed record KdfOptions
{
    public string AlgorithmId { get; }
    public int IterationCount { get; }
    public int MemoryCostKiB { get; }
    public int Parallelism { get; }
    public int SaltLengthBytes { get; }
    public KdfSaltLengthPolicy SaltLengthPolicy { get; }

    public static KdfOptions Default { get; } = new();

    public KdfOptions(
        string algorithmId = "pbkdf2-sha256",
        int iterationCount = 200_000,
        int memoryCostKiB = 0,
        int parallelism = 1,
        int saltLengthBytes = 16,
        KdfSaltLengthPolicy saltLengthPolicy = KdfSaltLengthPolicy.Fixed)
    {
        if (string.IsNullOrWhiteSpace(algorithmId))
        {
            throw new ArgumentException("KDF algorithm identifier is required.", nameof(algorithmId));
        }

        if (iterationCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterationCount), "Iteration count must be greater than zero.");
        }

        if (memoryCostKiB < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryCostKiB), "Memory cost cannot be negative.");
        }

        if (parallelism <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parallelism), "Parallelism must be greater than zero.");
        }

        if (saltLengthBytes < 8 || saltLengthBytes > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(saltLengthBytes), "Salt length must be in range 8-1024 bytes.");
        }

        if (!Enum.IsDefined(saltLengthPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(saltLengthPolicy), "Invalid salt length policy.");
        }

        AlgorithmId = algorithmId;
        IterationCount = iterationCount;
        MemoryCostKiB = memoryCostKiB;
        Parallelism = parallelism;
        SaltLengthBytes = saltLengthBytes;
        SaltLengthPolicy = saltLengthPolicy;
    }
}

public sealed record EncryptionOptions
{
    public string KdfAlgorithmId { get; }
    public EncryptionKeyLengthPolicy KeyLengthPolicy { get; }
    public string EncryptionAlgorithmId { get; }
    public KdfOptions KdfOptions { get; }

    public static EncryptionOptions Default { get; } = new();

    public EncryptionOptions(
        string encryptionAlgorithmId = "aes-256-gcm",
        string kdfAlgorithmId = "pbkdf2-sha256",
        EncryptionKeyLengthPolicy keyLengthPolicy = EncryptionKeyLengthPolicy.ProviderDefault,
        KdfOptions? kdfOptions = null)
    {
        if (string.IsNullOrWhiteSpace(encryptionAlgorithmId))
        {
            throw new ArgumentException("Encryption algorithm identifier is required.", nameof(encryptionAlgorithmId));
        }

        if (string.IsNullOrWhiteSpace(kdfAlgorithmId))
        {
            throw new ArgumentException("KDF algorithm identifier is required.", nameof(kdfAlgorithmId));
        }

        if (!Enum.IsDefined(keyLengthPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(keyLengthPolicy), "Invalid key length policy.");
        }

        var resolvedKdfOptions = kdfOptions ?? KdfOptions.Default;
        if (!string.Equals(kdfAlgorithmId, resolvedKdfOptions.AlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("KDF algorithm identifier must match KDF options algorithm identifier.", nameof(kdfAlgorithmId));
        }

        KdfAlgorithmId = kdfAlgorithmId;
        KeyLengthPolicy = keyLengthPolicy;
        EncryptionAlgorithmId = encryptionAlgorithmId;
        KdfOptions = resolvedKdfOptions;
    }
}

public enum EncryptionKeyLengthPolicy
{
    ProviderDefault,
    Strict
}

public sealed record CryptoEncryptRequest
{
    public byte[] Plaintext { get; }
    public string? Passphrase { get; }
    public byte[]? KeyMaterial { get; }
    public KdfOptions KdfOptions { get; }
    public byte[] AdditionalAuthenticatedData { get; }

    public CryptoEncryptRequest(
        byte[] plaintext,
        string? passphrase = null,
        byte[]? keyMaterial = null,
        KdfOptions? kdfOptions = null,
        byte[]? additionalAuthenticatedData = null)
    {
        if (plaintext is null || plaintext.Length == 0)
        {
            throw new ArgumentException("Plaintext must contain at least one byte.", nameof(plaintext));
        }

        if (passphrase is not null && string.IsNullOrWhiteSpace(passphrase))
        {
            throw new ArgumentException("Passphrase cannot be whitespace when provided.", nameof(passphrase));
        }

        if (keyMaterial is not null && keyMaterial.Length == 0)
        {
            throw new ArgumentException("Key material cannot be empty when provided.", nameof(keyMaterial));
        }

        if (string.IsNullOrWhiteSpace(passphrase) && (keyMaterial is null || keyMaterial.Length == 0))
        {
            throw new ArgumentException("Either a passphrase or key material must be provided.");
        }

        Plaintext = [.. plaintext];
        Passphrase = passphrase;
        KeyMaterial = keyMaterial is null ? null : [.. keyMaterial];
        KdfOptions = kdfOptions ?? KdfOptions.Default;
        AdditionalAuthenticatedData = additionalAuthenticatedData is null ? [] : [.. additionalAuthenticatedData];
    }
}

public sealed record CryptoEncryptResult
{
    public byte[] Ciphertext { get; }
    public byte[] Nonce { get; }
    public byte[] Salt { get; }
    public byte[] AuthenticationTag { get; }
    public string EncryptionAlgorithmId { get; }
    public string KdfAlgorithmId { get; }

    public CryptoEncryptResult(
        byte[] ciphertext,
        byte[] nonce,
        byte[] salt,
        byte[] authenticationTag,
        string encryptionAlgorithmId,
        string kdfAlgorithmId)
    {
        if (ciphertext is null || ciphertext.Length == 0)
        {
            throw new ArgumentException("Ciphertext must contain at least one byte.", nameof(ciphertext));
        }

        if (nonce is null || nonce.Length == 0)
        {
            throw new ArgumentException("Nonce must contain at least one byte.", nameof(nonce));
        }

        if (salt is null || salt.Length == 0)
        {
            throw new ArgumentException("Salt must contain at least one byte.", nameof(salt));
        }

        if (authenticationTag is null || authenticationTag.Length == 0)
        {
            throw new ArgumentException("Authentication tag must contain at least one byte.", nameof(authenticationTag));
        }

        if (string.IsNullOrWhiteSpace(encryptionAlgorithmId))
        {
            throw new ArgumentException("Encryption algorithm identifier is required.", nameof(encryptionAlgorithmId));
        }

        if (string.IsNullOrWhiteSpace(kdfAlgorithmId))
        {
            throw new ArgumentException("KDF algorithm identifier is required.", nameof(kdfAlgorithmId));
        }

        Ciphertext = [.. ciphertext];
        Nonce = [.. nonce];
        Salt = [.. salt];
        AuthenticationTag = [.. authenticationTag];
        EncryptionAlgorithmId = encryptionAlgorithmId;
        KdfAlgorithmId = kdfAlgorithmId;
    }
}

public sealed record CryptoDecryptRequest
{
    public byte[] Ciphertext { get; }
    public byte[] Nonce { get; }
    public byte[] Salt { get; }
    public byte[] AuthenticationTag { get; }
    public string? Passphrase { get; }
    public byte[]? KeyMaterial { get; }
    public string? ExpectedEncryptionAlgorithmId { get; }
    public string? ExpectedKdfAlgorithmId { get; }
    public byte[] AdditionalAuthenticatedData { get; }

    public CryptoDecryptRequest(
        byte[] ciphertext,
        byte[] nonce,
        byte[] salt,
        byte[] authenticationTag,
        string? passphrase = null,
        byte[]? keyMaterial = null,
        string? expectedEncryptionAlgorithmId = null,
        string? expectedKdfAlgorithmId = null,
        byte[]? additionalAuthenticatedData = null)
    {
        if (ciphertext is null || ciphertext.Length == 0)
        {
            throw new ArgumentException("Ciphertext must contain at least one byte.", nameof(ciphertext));
        }

        if (nonce is null || nonce.Length == 0)
        {
            throw new ArgumentException("Nonce must contain at least one byte.", nameof(nonce));
        }

        if (salt is null || salt.Length == 0)
        {
            throw new ArgumentException("Salt must contain at least one byte.", nameof(salt));
        }

        if (authenticationTag is null || authenticationTag.Length == 0)
        {
            throw new ArgumentException("Authentication tag must contain at least one byte.", nameof(authenticationTag));
        }

        if (passphrase is not null && string.IsNullOrWhiteSpace(passphrase))
        {
            throw new ArgumentException("Passphrase cannot be whitespace when provided.", nameof(passphrase));
        }

        if (keyMaterial is not null && keyMaterial.Length == 0)
        {
            throw new ArgumentException("Key material cannot be empty when provided.", nameof(keyMaterial));
        }

        if (string.IsNullOrWhiteSpace(passphrase) && (keyMaterial is null || keyMaterial.Length == 0))
        {
            throw new ArgumentException("Either a passphrase or key material must be provided.");
        }

        if (expectedEncryptionAlgorithmId is not null && string.IsNullOrWhiteSpace(expectedEncryptionAlgorithmId))
        {
            throw new ArgumentException("Expected encryption algorithm identifier cannot be whitespace when provided.", nameof(expectedEncryptionAlgorithmId));
        }

        if (expectedKdfAlgorithmId is not null && string.IsNullOrWhiteSpace(expectedKdfAlgorithmId))
        {
            throw new ArgumentException("Expected KDF algorithm identifier cannot be whitespace when provided.", nameof(expectedKdfAlgorithmId));
        }

        Ciphertext = [.. ciphertext];
        Nonce = [.. nonce];
        Salt = [.. salt];
        AuthenticationTag = [.. authenticationTag];
        Passphrase = passphrase;
        KeyMaterial = keyMaterial is null ? null : [.. keyMaterial];
        ExpectedEncryptionAlgorithmId = expectedEncryptionAlgorithmId;
        ExpectedKdfAlgorithmId = expectedKdfAlgorithmId;
        AdditionalAuthenticatedData = additionalAuthenticatedData is null ? [] : [.. additionalAuthenticatedData];
    }
}

public sealed record CryptoDecryptResult
{
    public byte[] Plaintext { get; }
    public OperationDiagnostics Diagnostics { get; }

    public CryptoDecryptResult(byte[] plaintext, OperationDiagnostics? diagnostics = null)
    {
        if (plaintext is null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        Plaintext = [.. plaintext];
        Diagnostics = diagnostics ?? OperationDiagnostics.Empty;
    }
}
