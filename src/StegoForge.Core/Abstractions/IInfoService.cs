using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface IInfoService
{
    Task<CarrierInfoResponse> GetInfoAsync(InfoRequest request, CancellationToken cancellationToken = default);
}
