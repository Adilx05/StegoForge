using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface ICapacityService
{
    Task<CapacityResponse> GetCapacityAsync(CapacityRequest request, CancellationToken cancellationToken = default);
}
