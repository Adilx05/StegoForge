namespace StegoForge.Core.Payload;

/// <summary>
/// Declares payload envelope wire versions and compatibility checks.
/// </summary>
public static class EnvelopeVersion
{
    /// <summary>
    /// Fixed 4-byte magic value (<c>SGF1</c>) that prefixes every serialized payload envelope.
    /// </summary>
    public static ReadOnlySpan<byte> MagicBytes => "SGF1"u8;

    /// <summary>
    /// Envelope format version 1.
    /// </summary>
    public const byte V1 = 0x01;

    /// <summary>
    /// Latest envelope format version supported by this build.
    /// </summary>
    public const byte LatestSupported = V1;

    /// <summary>
    /// Returns <see langword="true"/> when the supplied version is supported for decoding.
    /// </summary>
    /// <param name="version">The 1-byte envelope version value from the wire format.</param>
    public static bool IsCompatible(byte version) => version == V1;
}
