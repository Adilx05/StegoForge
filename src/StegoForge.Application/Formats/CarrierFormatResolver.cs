using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;

namespace StegoForge.Application.Formats;

public sealed class CarrierFormatResolver(IEnumerable<ICarrierFormatHandler> handlers)
{
    private readonly IReadOnlyList<ICarrierFormatHandler> _handlers = (handlers ?? throw new ArgumentNullException(nameof(handlers)))
        .OrderBy(static handler => handler.Format, StringComparer.Ordinal)
        .ThenBy(static handler => handler.GetType().FullName, StringComparer.Ordinal)
        .ToArray();

    public CarrierFormatResolution Resolve(Stream carrierStream, string? carrierPath = null)
    {
        ArgumentNullException.ThrowIfNull(carrierStream);

        var preferredExtension = GetNormalizedExtension(carrierPath);
        var primaryCandidates = GetCandidates(carrierStream, preferredExtension, extensionMatched: true);
        if (primaryCandidates.Count > 0)
        {
            var selected = primaryCandidates[0];
            var notes = BuildSelectionNotes(selected, primaryCandidates, preferredExtension, fallbackUsed: false);
            return new CarrierFormatResolution(selected, notes);
        }

        var fallbackCandidates = GetCandidates(carrierStream, preferredExtension, extensionMatched: false);
        if (fallbackCandidates.Count > 0)
        {
            var selected = fallbackCandidates[0];
            var notes = BuildSelectionNotes(selected, fallbackCandidates, preferredExtension, fallbackUsed: preferredExtension is not null);
            return new CarrierFormatResolution(selected, notes);
        }

        var extensionText = preferredExtension is null ? "none" : $".{preferredExtension}";
        throw new UnsupportedFormatException(
            $"Carrier format is unsupported. No registered format handler accepted carrier bytes (extension hint: {extensionText}).");
    }

    private List<ICarrierFormatHandler> GetCandidates(Stream carrierStream, string? preferredExtension, bool extensionMatched)
    {
        var matches = new List<ICarrierFormatHandler>();

        foreach (var handler in _handlers)
        {
            if (preferredExtension is not null && IsExtensionEligible(handler, preferredExtension) != extensionMatched)
            {
                continue;
            }

            if (preferredExtension is null && extensionMatched)
            {
                continue;
            }

            carrierStream.Position = 0;
            if (handler.Supports(carrierStream))
            {
                matches.Add(handler);
            }
        }

        carrierStream.Position = 0;
        return matches;
    }

    private static List<string> BuildSelectionNotes(
        ICarrierFormatHandler selected,
        IReadOnlyList<ICarrierFormatHandler> candidates,
        string? preferredExtension,
        bool fallbackUsed)
    {
        var notes = new List<string>();

        if (fallbackUsed)
        {
            notes.Add(
                $"Resolver fallback: no extension-eligible handler accepted extension '.{preferredExtension}', selected '{selected.Format}' by signature.");
        }

        if (candidates.Count > 1)
        {
            notes.Add(
                $"Resolver precedence: multiple handlers matched ({string.Join(", ", candidates.Select(static candidate => candidate.Format))}); selected '{selected.Format}' by deterministic ordering.");
        }

        return notes;
    }

    private static bool IsExtensionEligible(ICarrierFormatHandler handler, string extension)
        => string.Equals(GetPrimaryFormatToken(handler.Format), extension, StringComparison.OrdinalIgnoreCase);

    private static string? GetNormalizedExtension(string? carrierPath)
    {
        if (string.IsNullOrWhiteSpace(carrierPath))
        {
            return null;
        }

        var extension = Path.GetExtension(carrierPath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return extension.TrimStart('.').ToLowerInvariant();
    }

    private static string GetPrimaryFormatToken(string format)
    {
        var index = format.IndexOf('-', StringComparison.Ordinal);
        return index >= 0 ? format[..index] : format;
    }
}

public sealed record CarrierFormatResolution(ICarrierFormatHandler Handler, IReadOnlyList<string> Notes);
