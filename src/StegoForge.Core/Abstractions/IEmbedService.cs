using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface IEmbedService
{
    Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default);
}
