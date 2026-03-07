using System.Buffers.Binary;
using StegoForge.Application.Payload;
using StegoForge.Core.Errors;
using StegoForge.Core.Payload;
using Xunit;

namespace StegoForge.Tests.Unit.Hardening;

public sealed class PayloadEnvelopeSerializerFuzzTests
{
    private const int Seed = 702_311;
    private readonly PayloadEnvelopeSerializer _serializer = new();

    [Fact]
    public void Deserialize_FuzzedRandomByteArrays_OnlyThrowsTypedStegoExceptions()
    {
        var random = new Random(Seed);

        for (var i = 0; i < 256; i++)
        {
            var length = random.Next(0, 512);
            var bytes = new byte[length];
            random.NextBytes(bytes);

            try
            {
                _serializer.Deserialize(bytes);
            }
            catch (StegoForgeException exception)
            {
                Assert.True(
                    exception.Code is StegoErrorCode.InvalidHeader or StegoErrorCode.InvalidPayload,
                    $"Unexpected error code during fuzz deserialize: {exception.Code}");
                Assert.Equal(exception.Code, StegoErrorMapper.FromException(exception).Code);
            }
        }
    }

    [Fact]
    public void Deserialize_TruncatedAtEveryStructuralBoundary_ThrowsInvalidPayload()
    {
        var bytes = KnownFixtureEnvelopeBytes();
        var headerLength = ReadUInt16(bytes, offset: 6);
        var payloadLengthOffset = 8 + headerLength;
        var payloadLength = checked((int)ReadUInt64(bytes, payloadLengthOffset));
        var integrityLengthOffset = payloadLengthOffset + 8 + payloadLength;

        var boundaries = new[]
        {
            1,
            4,
            5,
            6,
            8,
            8 + headerLength,
            payloadLengthOffset,
            payloadLengthOffset + 8,
            integrityLengthOffset,
            integrityLengthOffset + 2,
            bytes.Length - 1
        };

        foreach (var length in boundaries.Distinct())
        {
            var truncated = bytes[..Math.Clamp(length, 0, bytes.Length - 1)];
            var exception = Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(truncated));
            AssertMappedCode(exception, StegoErrorCode.InvalidPayload);
        }
    }

    [Fact]
    public void Deserialize_CorruptedLengthPrefixes_MapToDeterministicCodes()
    {
        var bytes = KnownFixtureEnvelopeBytes();

        var corruptedHeaderLength = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt16LittleEndian(corruptedHeaderLength.AsSpan(6, 2), ushort.MaxValue);
        AssertMappedCode(
            Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(corruptedHeaderLength)),
            StegoErrorCode.InvalidPayload);

        var headerLength = ReadUInt16(bytes, 6);
        var payloadLengthOffset = 8 + headerLength;
        var corruptedPayloadLength = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt64LittleEndian(corruptedPayloadLength.AsSpan(payloadLengthOffset, 8), ulong.MaxValue);
        AssertMappedCode(
            Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(corruptedPayloadLength)),
            StegoErrorCode.InvalidPayload);

        var payloadLength = checked((int)ReadUInt64(bytes, payloadLengthOffset));
        var integrityLengthOffset = payloadLengthOffset + 8 + payloadLength;
        var corruptedIntegrityLength = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt16LittleEndian(corruptedIntegrityLength.AsSpan(integrityLengthOffset, 2), ushort.MaxValue);
        AssertMappedCode(
            Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(corruptedIntegrityLength)),
            StegoErrorCode.InvalidPayload);

        var corruptedCompressionStringLength = (byte[])bytes.Clone();
        var compressionLengthOffset = 8 + 1 + (1 + 2 + 5) + 8 + 8;
        BinaryPrimitives.WriteUInt16LittleEndian(corruptedCompressionStringLength.AsSpan(compressionLengthOffset, 2), ushort.MaxValue);
        AssertMappedCode(
            Assert.Throws<InvalidPayloadException>(() => _serializer.Deserialize(corruptedCompressionStringLength)),
            StegoErrorCode.InvalidPayload);
    }

    [Fact]
    public void Deserialize_MutatedMetadataFlagAndHeaderCombinations_ThrowInvalidHeader()
    {
        var withMetadata = KnownFixtureEnvelopeBytes();
        withMetadata[5] = (byte)(withMetadata[5] & ~(byte)EnvelopeFlags.MetadataPresent);
        AssertMappedCode(
            Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(withMetadata)),
            StegoErrorCode.InvalidHeader);

        var withoutMetadata = CreateEnvelopeWithoutMetadata();
        var withoutMetadataBytes = _serializer.Serialize(withoutMetadata);
        withoutMetadataBytes[5] = (byte)(withoutMetadataBytes[5] | (byte)EnvelopeFlags.MetadataPresent);
        AssertMappedCode(
            Assert.Throws<InvalidHeaderException>(() => _serializer.Deserialize(withoutMetadataBytes)),
            StegoErrorCode.InvalidHeader);
    }

    private static ushort ReadUInt16(byte[] source, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan(offset, 2));

    private static ulong ReadUInt64(byte[] source, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(offset, 8));

    private static void AssertMappedCode(StegoForgeException exception, StegoErrorCode expectedCode)
    {
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedCode, StegoErrorMapper.FromException(exception).Code);
    }

    private static PayloadEnvelope CreateEnvelopeWithoutMetadata()
    {
        return new PayloadEnvelope(
            version: EnvelopeVersion.V1,
            flags: EnvelopeFlags.None,
            header: new PayloadHeader(
                originalSizeBytes: 3,
                createdUtc: DateTimeOffset.FromUnixTimeMilliseconds(1234),
                compressionDescriptor: "none",
                encryptionDescriptor: "none"),
            payload: [0x01, 0x02, 0x03],
            integrityData: [0xAA]);
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
