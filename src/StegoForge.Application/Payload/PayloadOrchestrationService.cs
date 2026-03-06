using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;

namespace StegoForge.Application.Payload;

public sealed class PayloadOrchestrationService(ICompressionProvider compressionProvider, ICryptoProvider cryptoProvider)
{
    private const string NoCompressionDescriptor = "none";
    private const string NoEncryptionDescriptor = "none";

    public PayloadEnvelope CreateEnvelopeForEmbed(
        byte[] payload,
        ProcessingOptions processingOptions,
        PasswordOptions passwordOptions,
        string? passphrase,
        string? originalFileName = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(processingOptions);
        ArgumentNullException.ThrowIfNull(passwordOptions);

        if (payload.Length == 0)
        {
            throw new ArgumentException("Payload must contain at least one byte.", nameof(payload));
        }

        var (compressedPayload, wasCompressed, compressionDescriptor) = ApplyCompressionPolicy(payload, processingOptions);
        var (finalPayload, wasEncrypted, encryptionDescriptor, saltMetadata, nonceMetadata, integrityData) = ApplyEncryptionForEmbed(
            compressedPayload,
            processingOptions,
            passwordOptions,
            passphrase);

        var flags = EnvelopeFlags.None;
        if (wasCompressed)
        {
            flags |= EnvelopeFlags.Compressed;
        }

        if (wasEncrypted)
        {
            flags |= EnvelopeFlags.Encrypted;
        }

        if (!string.IsNullOrWhiteSpace(originalFileName) || saltMetadata is not null || nonceMetadata is not null)
        {
            flags |= EnvelopeFlags.MetadataPresent;
        }

        var header = new PayloadHeader(
            payload.LongLength,
            DateTimeOffset.UtcNow,
            compressionDescriptor,
            encryptionDescriptor,
            string.IsNullOrWhiteSpace(originalFileName) ? null : originalFileName,
            saltMetadata,
            nonceMetadata);

        return new PayloadEnvelope(EnvelopeVersion.V1, flags, header, finalPayload, integrityData);
    }

    public byte[] ExtractPayload(PayloadEnvelope envelope, ProcessingOptions processingOptions, PasswordOptions passwordOptions, string? passphrase)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(processingOptions);
        ArgumentNullException.ThrowIfNull(passwordOptions);

        var decrypted = ApplyDecryptionForExtract(envelope, passwordOptions, passphrase);

        return envelope.Flags.HasFlag(EnvelopeFlags.Compressed)
            ? compressionProvider.Decompress(new DecompressionRequest(decrypted, "extract:payload")).Data
            : decrypted;
    }

    private (byte[] Payload, bool WasEncrypted, string Descriptor, string? SaltMetadata, string? NonceMetadata, byte[] IntegrityData) ApplyEncryptionForEmbed(
        byte[] payload,
        ProcessingOptions processingOptions,
        PasswordOptions passwordOptions,
        string? passphrase)
    {
        var shouldEncrypt = processingOptions.EncryptionMode switch
        {
            EncryptionMode.None => false,
            EncryptionMode.Optional => !string.IsNullOrWhiteSpace(passphrase),
            EncryptionMode.Required => true,
            _ => throw new ArgumentOutOfRangeException(nameof(processingOptions), $"Unsupported encryption mode '{processingOptions.EncryptionMode}'.")
        };

        if (!shouldEncrypt)
        {
            return ([.. payload], false, NoEncryptionDescriptor, null, null, []);
        }

        EnsurePasswordPolicy(passwordOptions, passphrase, "embed encryption");
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new WrongPasswordException("A password is required for embed encryption.");
        }

        var request = new CryptoEncryptRequest(
            plaintext: payload,
            passphrase: passphrase,
            kdfOptions: processingOptions.EncryptionOptions.KdfOptions);

        var encrypted = cryptoProvider.Encrypt(request);

        var integrityData = new byte[encrypted.Nonce.Length + encrypted.Salt.Length + encrypted.AuthenticationTag.Length];
        Buffer.BlockCopy(encrypted.Nonce, 0, integrityData, 0, encrypted.Nonce.Length);
        Buffer.BlockCopy(encrypted.Salt, 0, integrityData, encrypted.Nonce.Length, encrypted.Salt.Length);
        Buffer.BlockCopy(encrypted.AuthenticationTag, 0, integrityData, encrypted.Nonce.Length + encrypted.Salt.Length, encrypted.AuthenticationTag.Length);

        return (
            encrypted.Ciphertext,
            true,
            BuildEncryptionDescriptor(encrypted.EncryptionAlgorithmId, encrypted.KdfAlgorithmId, processingOptions.EncryptionOptions.KeyLengthPolicy),
            BuildMetadataDescriptor(encrypted.Salt.Length, encrypted.KdfAlgorithmId),
            BuildMetadataDescriptor(encrypted.Nonce.Length, encrypted.EncryptionAlgorithmId),
            integrityData);
    }

    private byte[] ApplyDecryptionForExtract(PayloadEnvelope envelope, PasswordOptions passwordOptions, string? passphrase)
    {
        var flaggedEncrypted = envelope.Flags.HasFlag(EnvelopeFlags.Encrypted);
        var descriptorEncrypted = !string.Equals(envelope.Header.EncryptionDescriptor, NoEncryptionDescriptor, StringComparison.OrdinalIgnoreCase);
        if (!flaggedEncrypted && !descriptorEncrypted)
        {
            return [.. envelope.Payload];
        }

        EnsurePasswordPolicy(passwordOptions, passphrase, "extract decryption");

        var descriptor = ParseEncryptionDescriptor(envelope.Header.EncryptionDescriptor)
            ?? throw new InvalidHeaderException("Encrypted envelope is missing a valid encryption descriptor.");

        var nonceLength = ParseMetadataLength(envelope.Header.NonceMetadata, "nonce metadata");
        var saltLength = ParseMetadataLength(envelope.Header.SaltMetadata, "salt metadata");

        var tagLength = envelope.IntegrityData.Length - nonceLength - saltLength;
        if (tagLength <= 0)
        {
            throw new InvalidPayloadException("Encrypted envelope integrity data is malformed.");
        }

        var nonce = envelope.IntegrityData.AsSpan(0, nonceLength).ToArray();
        var salt = envelope.IntegrityData.AsSpan(nonceLength, saltLength).ToArray();
        var tag = envelope.IntegrityData.AsSpan(nonceLength + saltLength, tagLength).ToArray();

        var decryptRequest = new CryptoDecryptRequest(
            envelope.Payload,
            nonce,
            salt,
            tag,
            passphrase: passphrase,
            expectedEncryptionAlgorithmId: descriptor.EncryptionAlgorithmId,
            expectedKdfAlgorithmId: descriptor.KdfAlgorithmId);

        return cryptoProvider.Decrypt(decryptRequest).Plaintext;
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) ApplyCompressionPolicy(byte[] payload, ProcessingOptions processingOptions)
    {
        CompressionProviderContract.EnsureSupportedLevel(compressionProvider, processingOptions.CompressionLevel);

        return processingOptions.CompressionMode switch
        {
            CompressionMode.Disabled => ([.. payload], false, NoCompressionDescriptor),
            CompressionMode.Enabled => Compress(payload, processingOptions.CompressionLevel),
            CompressionMode.Automatic => CompressWhenSmaller(payload, processingOptions.CompressionLevel),
            _ => throw new ArgumentOutOfRangeException(nameof(processingOptions), $"Unsupported compression mode '{processingOptions.CompressionMode}'.")
        };
    }

    private static void EnsurePasswordPolicy(PasswordOptions passwordOptions, string? passphrase, string operationContext)
    {
        if (!Enum.IsDefined(passwordOptions.Requirement))
        {
            throw new InvalidArgumentsException($"Unsupported password requirement '{passwordOptions.Requirement}'.");
        }

        if (passwordOptions.Requirement == PasswordRequirement.Required && string.IsNullOrWhiteSpace(passphrase))
        {
            throw new WrongPasswordException($"A password is required for {operationContext}.");
        }
    }

    private static string BuildEncryptionDescriptor(string encryptionAlgorithmId, string kdfAlgorithmId, EncryptionKeyLengthPolicy keyLengthPolicy)
        => $"enc:{encryptionAlgorithmId};kdf:{kdfAlgorithmId};key-policy:{keyLengthPolicy}";

    private static string BuildMetadataDescriptor(int byteLength, string algorithmId)
        => $"len:{byteLength};alg:{algorithmId}";

    private static int ParseMetadataLength(string? metadata, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            throw new InvalidHeaderException($"Missing {fieldName}.");
        }

        foreach (var segment in metadata.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment.StartsWith("len:", StringComparison.OrdinalIgnoreCase) && int.TryParse(segment[4..], out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        throw new InvalidHeaderException($"Invalid {fieldName} value '{metadata}'.");
    }

    private static (string EncryptionAlgorithmId, string KdfAlgorithmId)? ParseEncryptionDescriptor(string descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor) || string.Equals(descriptor, NoEncryptionDescriptor, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? encryption = null;
        string? kdf = null;

        foreach (var segment in descriptor.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (segment.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
            {
                encryption = segment[4..];
            }
            else if (segment.StartsWith("kdf:", StringComparison.OrdinalIgnoreCase))
            {
                kdf = segment[4..];
            }
        }

        return string.IsNullOrWhiteSpace(encryption) || string.IsNullOrWhiteSpace(kdf) ? null : (encryption, kdf);
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) CompressWhenSmaller(byte[] payload, int level)
    {
        var compressed = compressionProvider.Compress(new CompressionRequest(payload, level, "embed:automatic"));
        if (compressed.CompressedData.Length >= payload.Length)
        {
            return ([.. payload], false, NoCompressionDescriptor);
        }

        return (compressed.CompressedData, true, compressionProvider.AlgorithmId);
    }

    private (byte[] Payload, bool WasCompressed, string Descriptor) Compress(byte[] payload, int level)
    {
        var compressed = compressionProvider.Compress(new CompressionRequest(payload, level, "embed:enabled"));
        return (compressed.CompressedData, true, compressionProvider.AlgorithmId);
    }
}
