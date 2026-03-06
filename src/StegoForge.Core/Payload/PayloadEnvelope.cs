namespace StegoForge.Core.Payload;

/// <summary>
/// Immutable top-level payload envelope model for carrier serialization.
/// </summary>
/// <remarks>
/// Serialization invariants for v1:
/// <list type="bullet">
/// <item><description>Magic is fixed to 4 bytes: <c>SGF1</c>.</description></item>
/// <item><description>Version is encoded as 1 byte.</description></item>
/// <item><description>Flags are encoded as 1 byte.</description></item>
/// <item><description>Header is length-prefixed with a 2-byte little-endian unsigned value.</description></item>
/// <item><description>Payload is length-prefixed with an 8-byte little-endian unsigned value.</description></item>
/// <item><description>All multi-byte integers in this envelope use deterministic little-endian byte order.</description></item>
/// </list>
/// </remarks>
public sealed record PayloadEnvelope
{
    public byte[] Magic { get; }
    public byte Version { get; }
    public EnvelopeFlags Flags { get; }
    public PayloadHeader Header { get; }
    public byte[] Payload { get; }
    public byte[] IntegrityData { get; }

    public PayloadEnvelope(
        byte version,
        EnvelopeFlags flags,
        PayloadHeader header,
        byte[] payload,
        byte[] integrityData,
        byte[]? magic = null)
    {
        if (!EnvelopeVersion.IsCompatible(version))
        {
            throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported envelope version '{version}'.");
        }

        if (header is null)
        {
            throw new ArgumentNullException(nameof(header));
        }

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (integrityData is null)
        {
            throw new ArgumentNullException(nameof(integrityData));
        }

        var resolvedMagic = magic ?? EnvelopeVersion.MagicBytes.ToArray();

        if (resolvedMagic.Length != EnvelopeVersion.MagicBytes.Length)
        {
            throw new ArgumentException("Envelope magic must be exactly 4 bytes.", nameof(magic));
        }

        Magic = [.. resolvedMagic];
        Version = version;
        Flags = flags;
        Header = header;
        Payload = [.. payload];
        IntegrityData = [.. integrityData];
    }
}
