using StegoForge.Application.Payload;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class PayloadEnvelopeSerializerTests
{
    private const int HeaderLengthOffset = 6;
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesAllEnvelopeFields()
    {
        var envelope = CreateFixtureEnvelope();

        var bytes = _serializer.Serialize(envelope);
        var deserialized = _serializer.Deserialize(bytes);

        Assert.Equal(envelope.Version, deserialized.Version);
        Assert.Equal(envelope.Flags, deserialized.Flags);
        Assert.Equal(envelope.Header, deserialized.Header);
        Assert.Equal(envelope.Payload, deserialized.Payload);
        Assert.Equal(envelope.IntegrityData, deserialized.IntegrityData);
    }

    [Fact]
    public void Serialize_KnownFixtureEnvelope_ProducesExpectedGoldenBytes()
    {
        var envelope = CreateFixtureEnvelope();

        var serialized = _serializer.Serialize(envelope);

        Assert.Equal(KnownFixtureEnvelopeBytes(), serialized);
    }

    [Fact]
    public void Deserialize_KnownFixtureEnvelope_ProducesExpectedValues()
    {
        var bytes = KnownFixtureEnvelopeBytes();

        var envelope = _serializer.Deserialize(bytes);

        Assert.Equal(EnvelopeVersion.V1, envelope.Version);
        Assert.Equal(EnvelopeFlags.Compressed | EnvelopeFlags.Encrypted | EnvelopeFlags.MetadataPresent, envelope.Flags);
        Assert.Equal(new PayloadHeader(
            originalSizeBytes: 5,
            createdUtc: DateTimeOffset.FromUnixTimeMilliseconds(1234),
            compressionDescriptor: "none",
            encryptionDescriptor: "aes-gcm",
            originalFileName: "a.bin",
            saltMetadata: "salt",
            nonceMetadata: "nonce"), envelope.Header);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30 }, envelope.Payload);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, envelope.IntegrityData);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(18)]
    [InlineData(71)]
    [InlineData(72)]
    public void Deserialize_TruncationAtStructuralBoundaries_ThrowsInvalidPayload(int truncatedLength)
    {
        var fixtureBytes = KnownFixtureEnvelopeBytes();
        var truncated = fixtureBytes[..truncatedLength];

        var ex = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(truncated));

        AssertMappedCode(ex, StegoErrorCode.InvalidPayload);
    }

    [Fact]
    public void Deserialize_CorruptMagic_ThrowsInvalidHeader()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        bytes[0] = (byte)'X';

        var ex = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidHeader);
    }

    [Fact]
    public void Deserialize_CorruptVersion_ThrowsInvalidHeader()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        bytes[4] = 0x7F;

        var ex = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidHeader);
    }

    [Fact]
    public void Deserialize_CorruptHeaderLengthPrefix_ThrowsInvalidPayload()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        bytes[HeaderLengthOffset] = 0xFF;
        bytes[HeaderLengthOffset + 1] = 0xFF;

        var ex = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidPayload);
    }

    [Fact]
    public void Deserialize_CorruptPayloadLengthPrefix_ThrowsInvalidPayload()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        var payloadLengthOffset = 8 + GetHeaderLength(bytes);

        // int.MaxValue + 1 to force the explicit in-memory bounds check.
        var tooLargeLength = BitConverter.GetBytes((ulong)int.MaxValue + 1UL);
        tooLargeLength.CopyTo(bytes, payloadLengthOffset);

        var ex = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidPayload);
    }

    [Fact]
    public void Deserialize_CorruptIntegrityLengthPrefix_ThrowsInvalidPayload()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        var integrityLengthOffset = 8 + GetHeaderLength(bytes) + 8 + 3;

        bytes[integrityLengthOffset] = 0xFF;
        bytes[integrityLengthOffset + 1] = 0xFF;

        var ex = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidPayload);
    }

    [Fact]
    public void Deserialize_DeclaredHeaderLengthBeyondConfiguredLimit_ThrowsWithoutLargeAllocation()
    {
        var serializer = new PayloadEnvelopeSerializer(new ProcessingLimits(maxHeaderBytes: 32, maxPayloadBytes: 2048, maxEnvelopeBytes: 4096));
        var bytes = KnownFixtureEnvelopeBytes();
        bytes[HeaderLengthOffset] = 0x40;
        bytes[HeaderLengthOffset + 1] = 0x00;

        var exception = Assert.Throws<InvalidPayloadException>(() => serializer.Deserialize(bytes));

        Assert.Contains("Header length exceeds configured limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_PayloadBeyondConfiguredLimit_ThrowsInvalidPayload()
    {
        var serializer = new PayloadEnvelopeSerializer(new ProcessingLimits(maxPayloadBytes: 8, maxHeaderBytes: 128, maxEnvelopeBytes: 256));
        var envelope = CreateFixtureEnvelope() with { Payload = Enumerable.Repeat((byte)0xAB, 16).ToArray() };

        var exception = Assert.Throws<InvalidPayloadException>(() => serializer.Serialize(envelope));

        Assert.Contains("Payload length exceeds configured limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_MutatedIntegrityBytes_IsAcceptedAsOpaqueData()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        var integrityDataOffset = 8 + GetHeaderLength(bytes) + 8 + 3 + 2;
        bytes[integrityDataOffset] ^= 0xFF;

        var envelope = _serializer.Deserialize(bytes);

        Assert.Equal(new byte[] { 0x55, 0xBB }, envelope.IntegrityData);
    }

    [Fact]
    public void Deserialize_ReservedUnknownFlagBits_AreRejectedByV1Policy()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        bytes[5] = 0b1000_0111;

        var ex = Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(bytes));

        AssertMappedCode(ex, StegoErrorCode.InvalidHeader);
    }

    [Fact]
    public void Serialize_ReservedUnknownFlagBits_AreRejectedByV1Policy()
    {
        var envelope = new PayloadEnvelope(
            version: EnvelopeVersion.V1,
            flags: EnvelopeFlags.Compressed | (EnvelopeFlags)0b1000_0000,
            header: new PayloadHeader(1, DateTimeOffset.UnixEpoch, "none", "none"),
            payload: [0x01],
            integrityData: [0x02]);

        var ex = Assert.Throws<InvalidHeaderException>(() => _serializer.Serialize(envelope));

        AssertMappedCode(ex, StegoErrorCode.InvalidHeader);
    }

    private static void AssertMappedCode(Exception exception, StegoErrorCode expectedCode)
    {
        var stegoException = Assert.IsAssignableFrom<StegoForgeException>(exception);
        Assert.Equal(expectedCode, stegoException.Code);
        Assert.Equal(expectedCode, StegoErrorMapper.FromException(exception).Code);
    }

    private static int GetHeaderLength(byte[] bytes)
    {
        return bytes[HeaderLengthOffset] | (bytes[HeaderLengthOffset + 1] << 8);
    }

    private static PayloadEnvelope CreateFixtureEnvelope()
    {
        return new PayloadEnvelope(
            version: EnvelopeVersion.V1,
            flags: EnvelopeFlags.Compressed | EnvelopeFlags.Encrypted | EnvelopeFlags.MetadataPresent,
            header: new PayloadHeader(
                originalSizeBytes: 5,
                createdUtc: DateTimeOffset.FromUnixTimeMilliseconds(1234),
                compressionDescriptor: "none",
                encryptionDescriptor: "aes-gcm",
                originalFileName: "a.bin",
                saltMetadata: "salt",
                nonceMetadata: "nonce"),
            payload: [0x10, 0x20, 0x30],
            integrityData: [0xAA, 0xBB]);
    }

    private static byte[] KnownFixtureEnvelopeBytes()
    {
        return
        [
            0x53, 0x47, 0x46, 0x31,
            0x01,
            0x07,
            0x37, 0x00,
            0x01,
            0x01, 0x05, 0x00, 0x61, 0x2E, 0x62, 0x69, 0x6E,
            0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xD2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x6E, 0x6F, 0x6E, 0x65,
            0x07, 0x00, 0x61, 0x65, 0x73, 0x2D, 0x67, 0x63, 0x6D,
            0x01, 0x04, 0x00, 0x73, 0x61, 0x6C, 0x74,
            0x01, 0x05, 0x00, 0x6E, 0x6F, 0x6E, 0x63, 0x65,
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x10, 0x20, 0x30,
            0x02, 0x00,
            0xAA, 0xBB
        ];
    }
}
