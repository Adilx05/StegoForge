using StegoForge.Core.Models;

namespace StegoForge.Core.Abstractions;

public interface IExtractService
{
    Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default);
}
