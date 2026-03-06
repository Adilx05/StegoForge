using StegoForge.Application.Payload;
using StegoForge.Core.Errors;
using StegoForge.Core.Payload;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class PayloadEnvelopeSerializerTests
{
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public void SerializeAndDeserialize_RoundTripsEnvelope()
    {
        var header = new PayloadHeader(
            3,
            DateTimeOffset.UnixEpoch,
            "none",
            "none",
            originalFileName: "sample.txt",
            saltMetadata: "salt:b64",
            nonceMetadata: "nonce:b64");

        var envelope = new PayloadEnvelope(
            EnvelopeVersion.V1,
            EnvelopeFlags.MetadataPresent,
            header,
            payload: [1, 2, 3],
            integrityData: [4, 5, 6]);

        var bytes = _serializer.Serialize(envelope);
        var deserialized = _serializer.Deserialize(bytes);

        Assert.Equal(envelope.Magic, deserialized.Magic);
        Assert.Equal(envelope.Version, deserialized.Version);
        Assert.Equal(envelope.Flags, deserialized.Flags);
        Assert.Equal(envelope.Payload, deserialized.Payload);
        Assert.Equal(envelope.IntegrityData, deserialized.IntegrityData);
        Assert.Equal(envelope.Header, deserialized.Header);
    }

    [Fact]
    public void Deserialize_MapsBadMagicToInvalidHeader()
    {
        var envelope = CreateValidEnvelopeBytes();
        envelope[0] = (byte)'X';

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidHeader, exception.Code);
    }

    [Fact]
    public void Deserialize_MapsUnsupportedVersionToInvalidHeader()
    {
        var envelope = CreateValidEnvelopeBytes();
        envelope[4] = 0x02;

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidHeader, exception.Code);
    }

    [Fact]
    public void Deserialize_MapsReservedFlagsToInvalidHeader()
    {
        var envelope = CreateValidEnvelopeBytes();
        envelope[5] = 0b1000_0000;

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidHeader, exception.Code);
    }

    [Fact]
    public void Deserialize_MapsTruncationToInvalidPayload()
    {
        var envelope = CreateValidEnvelopeBytes();
        var truncated = envelope[..^1];

        var exception = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(truncated));

        Assert.Equal(StegoErrorCode.InvalidPayload, exception.Code);
    }

    [Fact]
    public void Deserialize_RejectsDeclaredLengthBeyondAvailableBytes()
    {
        var envelope = CreateValidEnvelopeBytes();
        var headerLength = envelope[6] | (envelope[7] << 8);
        var payloadLengthOffset = 8 + headerLength;
        envelope[payloadLengthOffset] = 10;

        var exception = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidPayload, exception.Code);
    }

    [Fact]
    public void Deserialize_RejectsTrailingBytes()
    {
        var withTrailingBytes = CreateValidEnvelopeBytes().Concat(new byte[] { 0xFF, 0x00 }).ToArray();

        var exception = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(withTrailingBytes));

        Assert.Equal(StegoErrorCode.InvalidPayload, exception.Code);
    }

    [Fact]
    public void Deserialize_RejectsInvalidHeaderSchema()
    {
        var envelope = CreateValidEnvelopeBytes();
        // Header starts at offset 8.
        envelope[8] = 0x02;

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidHeader, exception.Code);
    }

    [Fact]
    public void Serialize_RejectsMetadataFlagMismatch()
    {
        var header = new PayloadHeader(1, DateTimeOffset.UtcNow, "none", "none", originalFileName: "x.txt");
        var envelope = new PayloadEnvelope(EnvelopeVersion.V1, EnvelopeFlags.None, header, [1], []);

        var exception = Assert.Throws<InvalidHeaderException>(() => _serializer.Serialize(envelope));

        Assert.Equal(StegoErrorCode.InvalidHeader, exception.Code);
    }

    private byte[] CreateValidEnvelopeBytes()
    {
        var header = new PayloadHeader(3, DateTimeOffset.UnixEpoch, "none", "none");
        var envelope = new PayloadEnvelope(EnvelopeVersion.V1, EnvelopeFlags.None, header, [1, 2, 3], [4, 5]);
        return _serializer.Serialize(envelope);
    }
}
