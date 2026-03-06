namespace StegoForge.Core.Abstractions;

public interface ICryptoProvider
{
    byte[] Encrypt(byte[] data, string password);

    byte[] Decrypt(byte[] data, string password);
}
