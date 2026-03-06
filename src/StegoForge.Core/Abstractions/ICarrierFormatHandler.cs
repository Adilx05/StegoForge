using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface ICarrierFormatHandler
{
    string Format { get; }

    bool Supports(Stream carrierStream);

    Task<long> GetCapacityAsync(Stream carrierStream, CancellationToken cancellationToken = default);

    Task EmbedAsync(Stream carrierStream, Stream outputStream, byte[] payload, CancellationToken cancellationToken = default);

    Task<byte[]> ExtractAsync(Stream carrierStream, CancellationToken cancellationToken = default);

    Task<CarrierInfoResponse> GetInfoAsync(Stream carrierStream, CancellationToken cancellationToken = default);
}
