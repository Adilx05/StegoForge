using StegoForge.Core.Payload;

namespace StegoForge.Core.Abstractions;

/// <summary>
/// Serializes and deserializes <see cref="PayloadEnvelope"/> instances to and from the on-wire binary envelope format.
/// </summary>
public interface IPayloadEnvelopeSerializer
{
    byte[] Serialize(PayloadEnvelope envelope);

    PayloadEnvelope Deserialize(byte[] envelopeBytes);
}
