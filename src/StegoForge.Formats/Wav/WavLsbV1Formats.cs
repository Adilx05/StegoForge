namespace StegoForge.Formats.Wav;

internal static class WavLsbV1Formats
{
    public const ushort PcmFormatTag = 1;
    public const ushort MonoChannelCount = 1;
    public const ushort StereoChannelCount = 2;
    public const ushort SupportedBitsPerSample = 16;
    public const int RiffWavePreambleBytes = 12;
    public const uint MinimumFmtChunkSizeBytes = 16;

    public static bool IsSupportedFormatTag(ushort formatTag)
        => formatTag == PcmFormatTag;

    public static bool IsSupportedBitsPerSample(ushort bitsPerSample)
        => bitsPerSample == SupportedBitsPerSample;

    public static bool IsSupportedChannelCount(ushort channels)
        => channels is MonoChannelCount or StereoChannelCount;

    public static bool IsFmtChunkSizeSupported(uint fmtChunkSize)
        => fmtChunkSize >= MinimumFmtChunkSizeBytes;

    public static bool IsDataChunkSizeAligned(int dataChunkSize, ushort blockAlign)
        => blockAlign != 0 && (dataChunkSize % blockAlign) == 0;

    public static string SupportedSetDescription
        => "RIFF/WAVE with fmt format tag 1 (PCM), 16-bit little-endian samples, and mono/stereo channel layouts.";

    public static string RequiredChunkDescription
        => "Required chunks: fmt (minimum 16-byte PCM payload) and data (size aligned to BlockAlign).";
}
