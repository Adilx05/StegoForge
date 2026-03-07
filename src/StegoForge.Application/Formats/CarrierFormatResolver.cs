using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;

namespace StegoForge.Application.Formats;

public sealed class CarrierFormatResolver(IEnumerable<ICarrierFormatHandler> handlers)
{
    private readonly IReadOnlyList<ICarrierFormatHandler> _handlers = handlers?.ToArray()
        ?? throw new ArgumentNullException(nameof(handlers));

    public ICarrierFormatHandler Resolve(Stream carrierStream)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);

        foreach (var handler in _handlers)
        {
            if (handler.Supports(carrierStream))
            {
                return handler;
            }
        }

        throw new UnsupportedFormatException("Carrier format is not supported by any registered format handler.");
    }
}
