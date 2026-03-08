using System.Buffers.Binary;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Formats.Wav;

public sealed class WavLsbFormatHandler : ICarrierFormatHandler
{
    private static readonly WavLsbCapacityCalculator CapacityCalculator = new();
    private static readonly CarrierFormatDetails Details = new("wav-lsb-v1", "WAV LSB (v1)", "1.0.0");
    private readonly ProcessingLimits _limits;

    public WavLsbFormatHandler(ProcessingLimits? limits = null)
    {
        _limits = limits ?? ProcessingLimits.SafeDefaults;
    }

    public string Format => Details.FormatId;

    public bool Supports(Stream carrierStream)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);

        using var buffer = CreateSeekableCopy(carrierStream);
        return TryParseSupportedCarrier(buffer, out _);
    }

    public async Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var wavData = ParseRequiredCarrier(buffer);

        return CapacityCalculator.CalculateFromSampleCount(wavData.SampleCount).MaximumRawEmbeddableBytes;
    }

    public async Task EmbedAsync(Stream carrierStream, Stream outputStream, byte[] payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (payload is null || payload.Length == 0)
        {
            throw new InvalidArgumentsException("Payload must contain at least one byte.");
        }

        if (payload.Length > _limits.MaxEnvelopeBytes)
        {
            throw new InvalidArgumentsException($"Payload envelope exceeds configured limit of {_limits.MaxEnvelopeBytes} bytes.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var wavData = ParseRequiredCarrier(buffer);

        var maxPayloadBytes = CapacityCalculator.CalculateFromSampleCount(wavData.SampleCount).MaximumRawEmbeddableBytes;
        if (payload.Length > maxPayloadBytes)
        {
            throw new InsufficientCapacityException(payload.Length, maxPayloadBytes);
        }

        var framedPayload = new byte[WavLsbCapacityCalculator.PayloadLengthPrefixBytes + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(framedPayload.AsSpan(0, WavLsbCapacityCalculator.PayloadLengthPrefixBytes), payload.Length);
        payload.CopyTo(framedPayload, WavLsbCapacityCalculator.PayloadLengthPrefixBytes);

        EmbedBitsInSamples(wavData, framedPayload, cancellationToken);

        await outputStream.WriteAsync(wavData.FileBytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var wavData = ParseRequiredCarrier(buffer);

        var sampleReader = ReadSampleLsbBits(wavData, cancellationToken).GetEnumerator();
        try
        {
            var header = ReadBytes(sampleReader, WavLsbCapacityCalculator.PayloadLengthPrefixBytes);
            var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
            if (payloadLength < 0)
            {
                throw new CorruptedDataException("Embedded payload length is invalid.");
            }

            var maxPayloadBytes = CapacityCalculator.CalculateFromSampleCount(wavData.SampleCount).MaximumRawEmbeddableBytes;
            if (payloadLength > maxPayloadBytes)
            {
                throw new CorruptedDataException("Embedded payload length exceeds carrier capacity.");
            }

            if (payloadLength > _limits.MaxEnvelopeBytes)
            {
                throw new CorruptedDataException($"Embedded payload length exceeds configured limit of {_limits.MaxEnvelopeBytes} bytes.");
            }

            return ReadBytes(sampleReader, payloadLength);
        }
        finally
        {
            sampleReader.Dispose();
        }
    }

    public async Task<CarrierInfoResponse> GetInfoAsync(Stream carrierStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = await CreateSeekableCopyAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var wavData = ParseRequiredCarrier(buffer);

        var maxPayloadBytes = CapacityCalculator.CalculateFromSampleCount(wavData.SampleCount).MaximumRawEmbeddableBytes;
        var diagnostics = new OperationDiagnostics(
            warnings: [],
            notes:
            [
                "Channel strategy: one least-significant bit per 16-bit PCM sample.",
                "Supported WAV set for wav-lsb-v1: " + WavLsbV1Formats.SupportedSetDescription,
                WavLsbV1Formats.RequiredChunkDescription,
                "Structural chunks are preserved verbatim; only data-chunk sample LSBs are modified.",
                $"Carrier details: channels={wavData.Channels}, sample-rate-hz={wavData.SampleRate}, bits-per-sample={wavData.BitsPerSample}, sample-count={wavData.SampleCount}."
            ]);

        return new CarrierInfoResponse(
            formatId: Format,
            formatDetails: Details,
            carrierSizeBytes: wavData.FileBytes.LongLength,
            estimatedCapacityBytes: maxPayloadBytes,
            availableCapacityBytes: maxPayloadBytes,
            embeddedDataPresent: false,
            supportsEncryption: true,
            supportsCompression: true,
            diagnostics: diagnostics);
    }

    private static byte[] ReadBytes(IEnumerator<byte> bitReader, int length)
    {
        var output = new byte[length];
        for (var i = 0; i < length; i++)
        {
            var value = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                if (!bitReader.MoveNext())
                {
                    throw new CorruptedDataException("Carrier did not contain enough data for embedded payload.");
                }

                value = (value << 1) | bitReader.Current;
            }

            output[i] = (byte)value;
        }

        return output;
    }

    private static IEnumerable<byte> ReadSampleLsbBits(WavCarrierData wavData, CancellationToken cancellationToken)
    {
        for (var sampleIndex = 0L; sampleIndex < wavData.SampleCount; sampleIndex++)
        {
            if ((sampleIndex & 0xFFF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var lowByteOffset = checked((int)(wavData.DataChunkOffset + (sampleIndex * 2)));
            yield return (byte)(wavData.FileBytes[lowByteOffset] & 1);
        }
    }

    private static void EmbedBitsInSamples(WavCarrierData wavData, byte[] payload, CancellationToken cancellationToken)
    {
        var bitCount = checked(payload.Length * 8);
        var sampleCapacityBits = wavData.SampleCount;

        if (bitCount > sampleCapacityBits)
        {
            throw new CorruptedDataException("Carrier capacity was exhausted before payload embedding completed.");
        }

        for (var bitIndex = 0; bitIndex < bitCount; bitIndex++)
        {
            if ((bitIndex & 0xFFF) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var sourceByte = payload[bitIndex / 8];
            var sourceBit = (sourceByte >> (7 - (bitIndex % 8))) & 1;
            var lowByteOffset = checked((int)(wavData.DataChunkOffset + (bitIndex * 2L)));
            wavData.FileBytes[lowByteOffset] = (byte)((wavData.FileBytes[lowByteOffset] & 0b1111_1110) | sourceBit);
        }
    }

    private static WavCarrierData ParseRequiredCarrier(Stream stream)
    {
        if (!TryParseSupportedCarrier(stream, out var carrierData, out var validationError))
        {
            throw validationError!;
        }

        return carrierData;
    }

    private static bool TryParseSupportedCarrier(Stream stream, out WavCarrierData carrierData)
        => TryParseSupportedCarrier(stream, out carrierData, out _);

    private static bool TryParseSupportedCarrier(Stream stream, out WavCarrierData carrierData, out StegoForgeException? validationError)
    {
        stream.Position = 0;
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            fileBytes = ms.ToArray();
        }

        if (fileBytes.Length < WavLsbV1Formats.RiffWavePreambleBytes)
        {
            carrierData = default;
            validationError = new InvalidHeaderException("WAV header is truncated; expected RIFF/WAVE preamble.");
            return false;
        }

        if (!HasAscii(fileBytes, 0, "RIFF") || !HasAscii(fileBytes, 8, "WAVE"))
        {
            carrierData = default;
            validationError = new UnsupportedFormatException("Carrier must be a RIFF/WAVE file for wav-lsb-v1.");
            return false;
        }

        var declaredRiffPayloadSize = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(4, 4));
        var declaredFileLength = declaredRiffPayloadSize + 8UL;
        if (declaredFileLength > (ulong)fileBytes.Length)
        {
            carrierData = default;
            validationError = new InvalidHeaderException("WAV RIFF size exceeds available file length.");
            return false;
        }

        FmtChunkData? fmt = null;
        var dataChunkOffset = -1;
        var dataChunkSize = 0;

        var cursor = 12;
        while (cursor + 8 <= fileBytes.Length)
        {
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(cursor + 4, 4));
            var chunkDataOffset = cursor + 8;
            var chunkDataEnd = chunkDataOffset + (long)chunkSize;

            if (chunkDataEnd > fileBytes.Length)
            {
                carrierData = default;
                validationError = new InvalidHeaderException("WAV chunk size exceeds file length.");
                return false;
            }

            if (HasAscii(fileBytes, cursor, "fmt "))
            {
                if (!WavLsbV1Formats.IsFmtChunkSizeSupported(chunkSize))
                {
                    carrierData = default;
                    validationError = new InvalidHeaderException("WAV fmt chunk is truncated; expected at least 16 bytes.");
                    return false;
                }

                var fmtSpan = fileBytes.AsSpan(chunkDataOffset, (int)chunkSize);
                fmt = new FmtChunkData(
                    AudioFormat: BinaryPrimitives.ReadUInt16LittleEndian(fmtSpan[0..2]),
                    Channels: BinaryPrimitives.ReadUInt16LittleEndian(fmtSpan[2..4]),
                    SampleRate: BinaryPrimitives.ReadUInt32LittleEndian(fmtSpan[4..8]),
                    ByteRate: BinaryPrimitives.ReadUInt32LittleEndian(fmtSpan[8..12]),
                    BlockAlign: BinaryPrimitives.ReadUInt16LittleEndian(fmtSpan[12..14]),
                    BitsPerSample: BinaryPrimitives.ReadUInt16LittleEndian(fmtSpan[14..16]));
            }
            else if (HasAscii(fileBytes, cursor, "data") && dataChunkOffset < 0)
            {
                dataChunkOffset = chunkDataOffset;
                dataChunkSize = (int)chunkSize;
            }

            var padding = chunkSize % 2;
            cursor = checked((int)(chunkDataEnd + padding));
        }

        if (fmt is null)
        {
            carrierData = default;
            validationError = new InvalidHeaderException("WAV file is missing required fmt chunk.");
            return false;
        }

        if (dataChunkOffset < 0)
        {
            carrierData = default;
            validationError = new InvalidHeaderException("WAV file is missing required data chunk.");
            return false;
        }

        var fmtData = fmt.Value;

        if (!WavLsbV1Formats.IsSupportedFormatTag(fmtData.AudioFormat))
        {
            carrierData = default;
            validationError = new UnsupportedFormatException($"Unsupported WAV format tag for wav-lsb-v1: detected {fmtData.AudioFormat}; supported set is {WavLsbV1Formats.SupportedSetDescription}");
            return false;
        }

        if (!WavLsbV1Formats.IsSupportedBitsPerSample(fmtData.BitsPerSample))
        {
            carrierData = default;
            validationError = new UnsupportedFormatException($"Unsupported WAV bit depth for wav-lsb-v1: detected {fmtData.BitsPerSample}-bit; supported set is {WavLsbV1Formats.SupportedSetDescription}");
            return false;
        }

        if (!WavLsbV1Formats.IsSupportedChannelCount(fmtData.Channels))
        {
            carrierData = default;
            validationError = new UnsupportedFormatException($"Unsupported WAV channel count for wav-lsb-v1: detected {fmtData.Channels}; supported set is {WavLsbV1Formats.SupportedSetDescription}");
            return false;
        }

        var expectedBlockAlign = (ushort)(fmtData.Channels * (fmtData.BitsPerSample / 8));
        if (fmtData.BlockAlign != expectedBlockAlign)
        {
            carrierData = default;
            validationError = new InvalidHeaderException($"WAV block alignment is invalid; expected {expectedBlockAlign}, detected {fmtData.BlockAlign}.");
            return false;
        }

        var expectedByteRate = fmtData.SampleRate * fmtData.BlockAlign;
        if (fmtData.ByteRate != expectedByteRate)
        {
            carrierData = default;
            validationError = new InvalidHeaderException($"WAV byte rate is invalid; expected {expectedByteRate}, detected {fmtData.ByteRate}.");
            return false;
        }

        if (!WavLsbV1Formats.IsDataChunkSizeAligned(dataChunkSize, fmtData.BlockAlign))
        {
            carrierData = default;
            validationError = new InvalidHeaderException("WAV data chunk size is not aligned to sample frame size.");
            return false;
        }

        var sampleCount = dataChunkSize / 2L;
        carrierData = new WavCarrierData(fileBytes, dataChunkOffset, dataChunkSize, sampleCount, fmtData.Channels, fmtData.SampleRate, fmtData.BitsPerSample);
        validationError = null;
        return true;
    }

    private static bool HasAscii(byte[] buffer, int offset, string text)
    {
        if (offset < 0 || (offset + text.Length) > buffer.Length)
        {
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            if (buffer[offset + i] != text[i])
            {
                return false;
            }
        }

        return true;
    }

    private static MemoryStream CreateSeekableCopy(Stream stream)
    {
        stream.Position = 0;
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    private async Task<MemoryStream> CreateSeekableCopyAsync(Stream stream, CancellationToken cancellationToken)
    {
        EnsureCarrierSizeWithinLimit(stream);
        stream.Position = 0;
        var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
        copy.Position = 0;
        return copy;
    }

    private void EnsureCarrierSizeWithinLimit(Stream source)
    {
        if (_limits.MaxCarrierSizeBytes is null)
        {
            return;
        }

        if (source.Length > _limits.MaxCarrierSizeBytes.Value)
        {
            throw new InvalidArgumentsException($"Carrier size exceeds configured limit of {_limits.MaxCarrierSizeBytes.Value} bytes.");
        }
    }

    private readonly record struct FmtChunkData(
        ushort AudioFormat,
        ushort Channels,
        uint SampleRate,
        uint ByteRate,
        ushort BlockAlign,
        ushort BitsPerSample);

    private readonly record struct WavCarrierData(
        byte[] FileBytes,
        int DataChunkOffset,
        int DataChunkSize,
        long SampleCount,
        ushort Channels,
        uint SampleRate,
        ushort BitsPerSample);
}
