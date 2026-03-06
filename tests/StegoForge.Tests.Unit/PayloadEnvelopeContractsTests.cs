using StegoForge.Core.Payload;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class PayloadEnvelopeContractsTests
{
    [Fact]
    public void EnvelopeVersion_DeclaresStableV1Contract()
    {
        Assert.Equal((byte)0x01, EnvelopeVersion.V1);
        Assert.True(EnvelopeVersion.IsCompatible(EnvelopeVersion.V1));
        Assert.False(EnvelopeVersion.IsCompatible(0x02));
        Assert.Equal("SGF1"u8.ToArray(), EnvelopeVersion.MagicBytes.ToArray());
    }

    [Fact]
    public void PayloadEnvelope_CopiesInputBuffers_ForImmutability()
    {
        var payload = new byte[] { 1, 2, 3 };
        var integrity = new byte[] { 4, 5 };
        var header = new PayloadHeader(3, DateTimeOffset.UnixEpoch, "none", "none");

        var envelope = new PayloadEnvelope(EnvelopeVersion.V1, EnvelopeFlags.None, header, payload, integrity);

        payload[0] = 99;
        integrity[0] = 42;

        Assert.Equal(new byte[] { 1, 2, 3 }, envelope.Payload);
        Assert.Equal(new byte[] { 4, 5 }, envelope.IntegrityData);
    }

    [Fact]
    public void PayloadHeader_RejectsInvalidDescriptors()
    {
        Assert.Throws<ArgumentException>(() => new PayloadHeader(1, DateTimeOffset.UtcNow, " ", "none"));
        Assert.Throws<ArgumentException>(() => new PayloadHeader(1, DateTimeOffset.UtcNow, "none", ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadHeader(-1, DateTimeOffset.UtcNow, "none", "none"));
    }
}
