namespace StegoForge.Core.Abstractions;

public interface ICompressionProvider
{
    byte[] Compress(byte[] data);

    byte[] Decompress(byte[] data);
}
