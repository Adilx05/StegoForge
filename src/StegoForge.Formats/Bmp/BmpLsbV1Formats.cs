namespace StegoForge.Formats.Bmp;

internal static class BmpLsbV1Formats
{
    public const ushort Bgr24BitsPerPixel = 24;
    public const ushort Bgra32BitsPerPixel = 32;
    public const uint UncompressedCompression = 0;

    public static readonly ushort[] SupportedBitsPerPixel = [Bgr24BitsPerPixel, Bgra32BitsPerPixel];

    public static bool IsSupportedBitsPerPixel(ushort bitsPerPixel)
        => bitsPerPixel is Bgr24BitsPerPixel or Bgra32BitsPerPixel;

    public static bool IsSupportedCompression(uint compression)
        => compression == UncompressedCompression;

    public static string SupportedSetDescription
        => "24-bit BGR (BI_RGB/uncompressed) or 32-bit BGRA (BI_RGB/uncompressed)";
}
