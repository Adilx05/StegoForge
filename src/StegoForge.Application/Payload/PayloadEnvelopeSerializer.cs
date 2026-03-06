using System.Buffers.Binary;
using System.Text;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Payload;

namespace StegoForge.Application.Payload;

public sealed class PayloadEnvelopeSerializer : IPayloadEnvelopeSerializer
{
    private const byte HeaderSchemaV1 = 0x01;
    private static readonly Encoding Utf8Strict = new UTF8Encoding(false, true);

    public PayloadEnvelope Deserialize(byte[] envelopeBytes)
    {
        if (envelopeBytes is null)
        {
            throw new InvalidPayloadException("Envelope bytes cannot be null.");
        }

        var reader = new EnvelopeReader(envelopeBytes);

        var magic = reader.ReadBytes(EnvelopeVersion.MagicBytes.Length, "magic");
        if (!magic.AsSpan().SequenceEqual(EnvelopeVersion.MagicBytes))
        {
            throw new InvalidHeaderException("Invalid envelope magic.");
        }

        var version = reader.ReadByte("version");
        if (!EnvelopeVersion.IsCompatible(version))
        {
            throw new InvalidHeaderException($"Unsupported envelope version '{version}'.");
        }

        var flags = (EnvelopeFlags)reader.ReadByte("flags");
        if ((flags & EnvelopeFlags.ReservedHighBitsMask) != 0)
        {
            throw new InvalidHeaderException("Envelope flags contain reserved high bits.");
        }

        var headerLength = reader.ReadUInt16LittleEndian("header length");
        var headerBytes = reader.ReadBytes(headerLength, "header block");
        var header = ParseHeader(headerBytes);

        var payloadLength = reader.ReadUInt64LittleEndian("payload length");
        if (payloadLength > int.MaxValue)
        {
            throw new InvalidPayloadException("Payload length exceeds supported in-memory bounds.");
        }

        var payload = reader.ReadBytes((int)payloadLength, "payload block");

        var integrityLength = reader.ReadUInt16LittleEndian("integrity length");
        var integrityData = reader.ReadBytes(integrityLength, "integrity block");

        if (!reader.IsConsumed)
        {
            throw new InvalidPayloadException("Envelope contains unexpected trailing bytes.");
        }

        var metadataPresent = header.SaltMetadata is not null || header.NonceMetadata is not null || header.OriginalFileName is not null;
        var flagMetadataPresent = flags.HasFlag(EnvelopeFlags.MetadataPresent);
        if (metadataPresent != flagMetadataPresent)
        {
            throw new InvalidHeaderException("Envelope metadata flag does not match serialized header metadata.");
        }

        return new PayloadEnvelope(version, flags, header, payload, integrityData, magic);
    }

    public byte[] Serialize(PayloadEnvelope envelope)
    {
        if (envelope is null)
        {
            throw new InvalidPayloadException("Envelope model cannot be null.");
        }

        if (!envelope.Magic.AsSpan().SequenceEqual(EnvelopeVersion.MagicBytes))
        {
            throw new InvalidHeaderException("Envelope magic must match the canonical SGF1 marker.");
        }

        if (!EnvelopeVersion.IsCompatible(envelope.Version))
        {
            throw new InvalidHeaderException($"Unsupported envelope version '{envelope.Version}'.");
        }

        if ((envelope.Flags & EnvelopeFlags.ReservedHighBitsMask) != 0)
        {
            throw new InvalidHeaderException("Envelope flags contain reserved high bits.");
        }

        var headerBytes = SerializeHeader(envelope.Header);
        if (headerBytes.Length > ushort.MaxValue)
        {
            throw new InvalidHeaderException("Serialized header length exceeds 16-bit envelope limit.");
        }

        var metadataPresent = envelope.Header.SaltMetadata is not null || envelope.Header.NonceMetadata is not null || envelope.Header.OriginalFileName is not null;
        var flagMetadataPresent = envelope.Flags.HasFlag(EnvelopeFlags.MetadataPresent);
        if (metadataPresent != flagMetadataPresent)
        {
            throw new InvalidHeaderException("Envelope metadata flag must match presence of optional metadata fields.");
        }

        var payload = envelope.Payload;
        var integrityData = envelope.IntegrityData;

        if (integrityData.Length > ushort.MaxValue)
        {
            throw new InvalidPayloadException("Integrity data length exceeds 16-bit envelope limit.");
        }

        var output = new byte[4 + 1 + 1 + 2 + headerBytes.Length + 8 + payload.Length + 2 + integrityData.Length];
        var offset = 0;

        envelope.Magic.AsSpan().CopyTo(output.AsSpan(offset, 4));
        offset += 4;

        output[offset++] = envelope.Version;
        output[offset++] = (byte)envelope.Flags;

        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(offset, 2), checked((ushort)headerBytes.Length));
        offset += 2;

        headerBytes.CopyTo(output.AsSpan(offset, headerBytes.Length));
        offset += headerBytes.Length;

        BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(offset, 8), checked((ulong)payload.Length));
        offset += 8;

        payload.CopyTo(output.AsSpan(offset, payload.Length));
        offset += payload.Length;

        BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(offset, 2), checked((ushort)integrityData.Length));
        offset += 2;

        integrityData.CopyTo(output.AsSpan(offset, integrityData.Length));

        return output;
    }

    private static byte[] SerializeHeader(PayloadHeader header)
    {
        var buffer = new List<byte>(128) { HeaderSchemaV1 };

        WriteOptionalString(buffer, header.OriginalFileName);

        Span<byte> int64Buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(int64Buffer, header.OriginalSizeBytes);
        AddSpan(buffer, int64Buffer);

        var createdUtcEpochMs = header.CreatedUtc.ToUnixTimeMilliseconds();
        BinaryPrimitives.WriteInt64LittleEndian(int64Buffer, createdUtcEpochMs);
        AddSpan(buffer, int64Buffer);

        WriteRequiredString(buffer, header.CompressionDescriptor, "compression descriptor");
        WriteRequiredString(buffer, header.EncryptionDescriptor, "encryption descriptor");
        WriteOptionalString(buffer, header.SaltMetadata);
        WriteOptionalString(buffer, header.NonceMetadata);

        return [.. buffer];
    }

    private static PayloadHeader ParseHeader(byte[] headerBytes)
    {
        var reader = new EnvelopeReader(headerBytes);

        var schema = reader.ReadByte("header schema");
        if (schema != HeaderSchemaV1)
        {
            throw new InvalidHeaderException($"Invalid header schema '{schema}'.");
        }

        var originalFileName = ReadOptionalString(reader, "original file name");
        var originalSizeBytes = reader.ReadInt64LittleEndian("original size");
        if (originalSizeBytes < 0)
        {
            throw new InvalidHeaderException("Header original size cannot be negative.");
        }

        var createdUtcEpochMs = reader.ReadInt64LittleEndian("created timestamp");

        DateTimeOffset createdUtc;
        try
        {
            createdUtc = DateTimeOffset.FromUnixTimeMilliseconds(createdUtcEpochMs);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidHeaderException($"Header timestamp is out of range: {ex.Message}");
        }

        var compressionDescriptor = ReadRequiredString(reader, "compression descriptor");
        var encryptionDescriptor = ReadRequiredString(reader, "encryption descriptor");
        var saltMetadata = ReadOptionalString(reader, "salt metadata");
        var nonceMetadata = ReadOptionalString(reader, "nonce metadata");

        if (!reader.IsConsumed)
        {
            throw new InvalidHeaderException("Header contains unexpected trailing bytes.");
        }

        return new PayloadHeader(
            originalSizeBytes,
            createdUtc,
            compressionDescriptor,
            encryptionDescriptor,
            originalFileName,
            saltMetadata,
            nonceMetadata);
    }

    private static void WriteRequiredString(List<byte> buffer, string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidHeaderException($"Header {fieldName} is required.");
        }

        WriteStringBytes(buffer, value);
    }

    private static string ReadRequiredString(EnvelopeReader reader, string fieldName)
    {
        var value = ReadStringBytes(reader, fieldName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidHeaderException($"Header {fieldName} is required.");
        }

        return value;
    }

    private static void WriteOptionalString(List<byte> buffer, string? value)
    {
        if (value is null)
        {
            buffer.Add(0);
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidHeaderException("Optional header strings cannot be whitespace.");
        }

        buffer.Add(1);
        WriteStringBytes(buffer, value);
    }

    private static string? ReadOptionalString(EnvelopeReader reader, string fieldName)
    {
        var marker = reader.ReadByte($"{fieldName} marker");
        return marker switch
        {
            0 => null,
            1 => ReadStringBytes(reader, fieldName),
            _ => throw new InvalidHeaderException($"Invalid optional field marker for {fieldName}.")
        };
    }

    private static void WriteStringBytes(List<byte> buffer, string value)
    {
        var bytes = Utf8Strict.GetBytes(value);
        if (bytes.Length > ushort.MaxValue)
        {
            throw new InvalidHeaderException("Header string length exceeds 16-bit field limit.");
        }

        Span<byte> lengthBuffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(lengthBuffer, checked((ushort)bytes.Length));
        AddSpan(buffer, lengthBuffer);
        buffer.AddRange(bytes);
    }

    private static string ReadStringBytes(EnvelopeReader reader, string fieldName)
    {
        var byteLength = reader.ReadUInt16LittleEndian($"{fieldName} length");
        var bytes = reader.ReadBytes(byteLength, fieldName);

        try
        {
            return Utf8Strict.GetString(bytes);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidHeaderException($"Invalid UTF-8 data in header {fieldName}: {ex.Message}");
        }
    }

    private static void AddSpan(List<byte> target, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            target.Add(value);
        }
    }

    private sealed class EnvelopeReader(byte[] source)
    {
        private int _offset;

        public bool IsConsumed => _offset == source.Length;

        public byte ReadByte(string fieldName)
        {
            EnsureAvailable(1, fieldName);
            return source[_offset++];
        }

        public byte[] ReadBytes(int length, string fieldName)
        {
            EnsureAvailable(length, fieldName);
            var data = source.AsSpan(_offset, length).ToArray();
            _offset += length;
            return data;
        }

        public ushort ReadUInt16LittleEndian(string fieldName)
        {
            EnsureAvailable(2, fieldName);
            var value = BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan(_offset, 2));
            _offset += 2;
            return value;
        }

        public ulong ReadUInt64LittleEndian(string fieldName)
        {
            EnsureAvailable(8, fieldName);
            var value = BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(_offset, 8));
            _offset += 8;
            return value;
        }

        public long ReadInt64LittleEndian(string fieldName)
        {
            EnsureAvailable(8, fieldName);
            var value = BinaryPrimitives.ReadInt64LittleEndian(source.AsSpan(_offset, 8));
            _offset += 8;
            return value;
        }

        private void EnsureAvailable(int requestedLength, string fieldName)
        {
            if (requestedLength < 0)
            {
                throw new InvalidPayloadException($"Invalid negative read length for {fieldName}.");
            }

            if (source.Length - _offset < requestedLength)
            {
                throw new InvalidPayloadException($"Envelope is truncated while reading {fieldName}.");
            }
        }
    }
}
