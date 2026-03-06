using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Crypto.AesGcm;
using Xunit;

namespace StegoForge.Tests.Unit.Crypto;

public sealed class AesGcmCryptoProviderTests
{
    private static readonly AesGcmCryptoProvider Provider = new();

    [Fact]
    public void EncryptAndDecrypt_RoundTripsPayload_WithExpectedMetadata()
    {
        var plaintext = Enumerable.Range(0, 1024).Select(static value => (byte)(value % 251)).ToArray();
        var aad = new byte[] { 0x10, 0x20, 0x30 };
        var encryptRequest = new CryptoEncryptRequest(
            plaintext,
            passphrase: "correct horse battery staple",
            kdfOptions: KdfOptions.Default,
            additionalAuthenticatedData: aad);

        var encrypted = Provider.Encrypt(encryptRequest);

        Assert.Equal(AesGcmCryptoProvider.EncryptionAlgorithmId, encrypted.EncryptionAlgorithmId);
        Assert.Equal(AesGcmCryptoProvider.KdfAlgorithmId, encrypted.KdfAlgorithmId);
        Assert.Equal(AesGcmCryptoProvider.NonceLengthBytes, encrypted.Nonce.Length);
        Assert.Equal(AesGcmCryptoProvider.SaltLengthBytes, encrypted.Salt.Length);
        Assert.Equal(AesGcmCryptoProvider.TagLengthBytes, encrypted.AuthenticationTag.Length);

        var decrypted = Provider.Decrypt(new CryptoDecryptRequest(
            encrypted.Ciphertext,
            encrypted.Nonce,
            encrypted.Salt,
            encrypted.AuthenticationTag,
            passphrase: "correct horse battery staple",
            expectedEncryptionAlgorithmId: AesGcmCryptoProvider.EncryptionAlgorithmId,
            expectedKdfAlgorithmId: AesGcmCryptoProvider.KdfAlgorithmId,
            additionalAuthenticatedData: aad));

        Assert.Equal(plaintext, decrypted.Plaintext);
        Assert.Contains("salt-bytes=16", decrypted.Diagnostics.Notes);
        Assert.Contains("nonce-bytes=12", decrypted.Diagnostics.Notes);
        Assert.Contains("tag-bytes=16", decrypted.Diagnostics.Notes);
    }

    [Fact]
    public void Decrypt_ThrowsInvalidPayloadException_WhenNonceLengthIsInvalid()
    {
        var encrypted = Provider.Encrypt(new CryptoEncryptRequest([1, 2, 3], passphrase: "p@ssword123"));

        var exception = Assert.Throws<InvalidPayloadException>(() =>
            Provider.Decrypt(new CryptoDecryptRequest(
                encrypted.Ciphertext,
                nonce: [.. encrypted.Nonce, 0x00],
                encrypted.Salt,
                encrypted.AuthenticationTag,
                passphrase: "p@ssword123")));

        Assert.Equal("Invalid nonce length. Expected 12 bytes.", exception.Message);
    }

    [Fact]
    public void Decrypt_ThrowsInvalidPayloadException_WhenExpectedAlgorithmIdentifierMismatches()
    {
        var encrypted = Provider.Encrypt(new CryptoEncryptRequest([1, 2, 3], passphrase: "p@ssword123"));

        var exception = Assert.Throws<InvalidPayloadException>(() =>
            Provider.Decrypt(new CryptoDecryptRequest(
                encrypted.Ciphertext,
                encrypted.Nonce,
                encrypted.Salt,
                encrypted.AuthenticationTag,
                passphrase: "p@ssword123",
                expectedEncryptionAlgorithmId: "chacha20-poly1305")));

        Assert.Equal("Unexpected encryption algorithm 'chacha20-poly1305'. Expected 'aes-256-gcm'.", exception.Message);
    }

    [Fact]
    public void Encrypt_ThrowsInvalidPayloadException_WhenKeyMaterialLengthIsInvalid()
    {
        var exception = Assert.Throws<InvalidPayloadException>(() =>
            Provider.Encrypt(new CryptoEncryptRequest([1, 2, 3], keyMaterial: [1, 2, 3])));

        Assert.Equal("AES-256-GCM requires exactly 32 bytes of key material.", exception.Message);
    }
}
