using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface ICryptoProvider
{
    /// <summary>
    /// Encrypts plaintext using the configured provider implementation.
    /// </summary>
    /// <param name="request">Encryption request containing plaintext, credentials, KDF options, and optional AAD.</param>
    /// <returns>A structured encryption result containing ciphertext and all crypto metadata required for decryption.</returns>
    /// <exception cref="WrongPasswordException">Thrown when passphrase-derived key validation fails during a provider preflight check.</exception>
    /// <exception cref="InvalidPayloadException">Thrown when request payload material is malformed for the provider (e.g., invalid key/nonce constraints).</exception>
    /// <exception cref="InternalProcessingException">Thrown when an unexpected provider failure occurs.</exception>
    CryptoEncryptResult Encrypt(CryptoEncryptRequest request);

    /// <summary>
    /// Decrypts a ciphertext package produced by a compatible crypto provider.
    /// </summary>
    /// <param name="request">Decryption request containing ciphertext package, credentials, expected algorithm identifiers, and optional AAD.</param>
    /// <returns>A structured decryption result containing plaintext and diagnostics.</returns>
    /// <exception cref="WrongPasswordException">Thrown when passphrase is incorrect or AEAD authentication fails.</exception>
    /// <exception cref="InvalidPayloadException">Thrown when ciphertext package data is malformed, including invalid nonce/tag/salt encodings.</exception>
    /// <exception cref="InternalProcessingException">Thrown when an unexpected provider failure occurs.</exception>
    CryptoDecryptResult Decrypt(CryptoDecryptRequest request);
}
