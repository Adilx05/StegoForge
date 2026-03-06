using System.Security.Cryptography;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Crypto.AesGcm;

public sealed class AesGcmCryptoProvider : ICryptoProvider
{
    public const string EncryptionAlgorithmId = "aes-256-gcm";
    public const string KdfAlgorithmId = "pbkdf2-sha256";

    public const int KeyLengthBytes = 32;
    public const int NonceLengthBytes = 12;
    public const int TagLengthBytes = 16;
    public const int SaltLengthBytes = 16;

    public CryptoEncryptResult Encrypt(CryptoEncryptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var saltLength = ResolveSaltLength(request.KdfOptions);
            var salt = new byte[saltLength];
            RandomNumberGenerator.Fill(salt);

            var nonce = new byte[NonceLengthBytes];
            RandomNumberGenerator.Fill(nonce);

            var key = ResolveKeyMaterial(request.Passphrase, request.KeyMaterial, request.KdfOptions, salt);
            var ciphertext = new byte[request.Plaintext.Length];
            var authenticationTag = new byte[TagLengthBytes];

            using var aesGcm = new System.Security.Cryptography.AesGcm(key, TagLengthBytes);
            aesGcm.Encrypt(nonce, request.Plaintext, ciphertext, authenticationTag, request.AdditionalAuthenticatedData);

            CryptographicOperations.ZeroMemory(key);

            return new CryptoEncryptResult(
                ciphertext,
                nonce,
                salt,
                authenticationTag,
                EncryptionAlgorithmId,
                KdfAlgorithmId);
        }
        catch (Exception exception) when (exception is not InvalidPayloadException and not WrongPasswordException and not InternalProcessingException)
        {
            throw new InternalProcessingException("Encryption failed unexpectedly.", exception);
        }
    }

    public CryptoDecryptResult Decrypt(CryptoDecryptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateDecryptRequest(request);

        try
        {
            var key = ResolveKeyMaterial(request.Passphrase, request.KeyMaterial, new KdfOptions(), request.Salt);
            var plaintext = new byte[request.Ciphertext.Length];

            using var aesGcm = new System.Security.Cryptography.AesGcm(key, TagLengthBytes);
            aesGcm.Decrypt(request.Nonce, request.Ciphertext, request.AuthenticationTag, plaintext, request.AdditionalAuthenticatedData);

            CryptographicOperations.ZeroMemory(key);

            var diagnostics = new OperationDiagnostics(notes:
            [
                $"encryption={EncryptionAlgorithmId}",
                $"kdf={KdfAlgorithmId}",
                $"salt-bytes={request.Salt.Length}",
                $"nonce-bytes={request.Nonce.Length}",
                $"tag-bytes={request.AuthenticationTag.Length}"
            ]);

            return new CryptoDecryptResult(plaintext, diagnostics);
        }
        catch (CryptographicException exception)
        {
            throw new WrongPasswordException($"Unable to authenticate and decrypt payload. {exception.Message}");
        }
        catch (Exception exception) when (exception is not InvalidPayloadException and not WrongPasswordException and not InternalProcessingException)
        {
            throw new InternalProcessingException("Decryption failed unexpectedly.", exception);
        }
    }

    private static byte[] ResolveKeyMaterial(string? passphrase, byte[]? keyMaterial, KdfOptions options, byte[] salt)
    {
        if (!string.IsNullOrWhiteSpace(passphrase))
        {
            EnsureSupportedKdf(options.AlgorithmId);
            var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, options.IterationCount, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeyLengthBytes);
        }

        if (keyMaterial is null || keyMaterial.Length == 0)
        {
            throw new InvalidPayloadException("Either passphrase or key material must be provided.");
        }

        if (keyMaterial.Length != KeyLengthBytes)
        {
            throw new InvalidPayloadException($"AES-256-GCM requires exactly {KeyLengthBytes} bytes of key material.");
        }

        return [.. keyMaterial];
    }

    private static int ResolveSaltLength(KdfOptions options)
    {
        EnsureSupportedKdf(options.AlgorithmId);

        if (options.SaltLengthBytes != SaltLengthBytes)
        {
            throw new InvalidPayloadException($"{KdfAlgorithmId} requires {SaltLengthBytes}-byte salt for this provider.");
        }

        return options.SaltLengthBytes;
    }

    private static void ValidateDecryptRequest(CryptoDecryptRequest request)
    {
        if (request.Ciphertext.Length == 0)
        {
            throw new InvalidPayloadException("Ciphertext must contain at least one byte.");
        }

        if (request.Nonce.Length != NonceLengthBytes)
        {
            throw new InvalidPayloadException($"Invalid nonce length. Expected {NonceLengthBytes} bytes.");
        }

        if (request.AuthenticationTag.Length != TagLengthBytes)
        {
            throw new InvalidPayloadException($"Invalid authentication tag length. Expected {TagLengthBytes} bytes.");
        }

        if (request.Salt.Length != SaltLengthBytes)
        {
            throw new InvalidPayloadException($"Invalid salt length. Expected {SaltLengthBytes} bytes.");
        }

        if (request.ExpectedEncryptionAlgorithmId is not null &&
            !string.Equals(request.ExpectedEncryptionAlgorithmId, EncryptionAlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidPayloadException(
                $"Unexpected encryption algorithm '{request.ExpectedEncryptionAlgorithmId}'. Expected '{EncryptionAlgorithmId}'.");
        }

        if (request.ExpectedKdfAlgorithmId is not null &&
            !string.Equals(request.ExpectedKdfAlgorithmId, KdfAlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidPayloadException(
                $"Unexpected KDF algorithm '{request.ExpectedKdfAlgorithmId}'. Expected '{KdfAlgorithmId}'.");
        }
    }

    private static void EnsureSupportedKdf(string algorithmId)
    {
        if (!string.Equals(algorithmId, KdfAlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidPayloadException($"Unsupported KDF algorithm '{algorithmId}'. Expected '{KdfAlgorithmId}'.");
        }
    }
}
