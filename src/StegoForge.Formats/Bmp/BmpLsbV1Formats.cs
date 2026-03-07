namespace StegoForge.Formats.Bmp;

internal static class BmpLsbV1Formats
{
    public const ushort Bgr24BitsPerPixel = 24;
    public const ushort Bgra32BitsPerPixel = 32;
    public const uint BiRgbCompression = 0;
    public const uint BiBitFieldsCompression = 3;

    public static bool IsSupportedBitsPerPixel(ushort bitsPerPixel)
        => bitsPerPixel is Bgr24BitsPerPixel or Bgra32BitsPerPixel;

    public static bool IsSupportedCompression(ushort bitsPerPixel, uint compression)
        => bitsPerPixel switch
        {
            Bgr24BitsPerPixel => compression == BiRgbCompression,
            Bgra32BitsPerPixel => compression is BiRgbCompression or BiBitFieldsCompression,
            _ => false
        };

    public static string SupportedSetDescription
        => "24-bit BGR (BI_RGB/uncompressed) or 32-bit BGRA (BI_RGB or BI_BITFIELDS, uncompressed pixel layout)";
}
