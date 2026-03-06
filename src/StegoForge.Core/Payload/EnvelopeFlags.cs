namespace StegoForge.Core.Payload;

/// <summary>
/// Flags encoded in the envelope's single-byte flags field.
/// </summary>
/// <remarks>
/// Bits 0..2 are defined for envelope v1. Bits 3..7 are reserved for future versions and must be preserved when possible.
/// </remarks>
[Flags]
public enum EnvelopeFlags : byte
{
    /// <summary>
    /// No optional envelope behaviors are enabled.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bit 0: payload bytes are compressed.
    /// </summary>
    Compressed = 1 << 0,

    /// <summary>
    /// Bit 1: payload bytes are encrypted.
    /// </summary>
    Encrypted = 1 << 1,

    /// <summary>
    /// Bit 2: optional metadata fields are present in the header.
    /// </summary>
    MetadataPresent = 1 << 2,

    /// <summary>
    /// Bit mask for all currently reserved high bits (3..7).
    /// </summary>
    ReservedHighBitsMask = 0b1111_1000
}
