using StegoForge.Application.Formats;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Formats.Bmp;

namespace StegoForge.Application.Capacity;

public sealed class CapacityService(CarrierFormatResolver formatResolver) : ICapacityService
{
    private static readonly BmpLsbCapacityCalculator CapacityCalculator = new();

    public async Task<CapacityResponse> GetCapacityAsync(CapacityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(request.CarrierPath))
        {
            throw new InvalidArgumentsException($"Carrier file '{request.CarrierPath}' was not found.");
        }

        await using var stream = File.OpenRead(request.CarrierPath);
        var handler = formatResolver.Resolve(stream);
        stream.Position = 0;

        var maxRawCapacity = await handler.GetCapacityAsync(stream, cancellationToken).ConfigureAwait(false);
        var estimate = CapacityCalculator.CalculateFromRaw(
            maxRawCapacity,
            reservedEnvelopeOverheadBytes: BmpLsbCapacityCalculator.DefaultReservedEnvelopeOverheadBytes,
            requestedPayloadBytes: request.PayloadSizeBytes);

        var canEmbed = estimate.CanEmbedRequestedPayload;
        var remainingBytes = estimate.SafeUsableBytes - request.PayloadSizeBytes;
        var failureReason = canEmbed
            ? null
            : $"Payload exceeds safe {handler.Format} capacity by {request.PayloadSizeBytes - estimate.SafeUsableBytes} byte(s).";

        var diagnostics = new OperationDiagnostics(
            notes:
            [
                $"Resolved carrier format: {handler.Format}.",
                "LSB channels used: 3 (RGB only).",
                "Safe capacity policy reserves 128 bytes for payload envelope overhead."
            ],
            providerIdentifier: nameof(CarrierFormatResolver));

        return new CapacityResponse(
            carrierFormatId: handler.Format,
            requestedPayloadSizeBytes: request.PayloadSizeBytes,
            availableCapacityBytes: estimate.SafeUsableBytes,
            maximumCapacityBytes: estimate.MaximumRawEmbeddableBytes,
            safeUsableCapacityBytes: estimate.SafeUsableBytes,
            estimatedOverheadBytes: estimate.ReservedEnvelopeOverheadBytes,
            canEmbed: canEmbed,
            remainingBytes: remainingBytes,
            failureReason: failureReason,
            constraintBreakdown: estimate.ConstraintDiagnostics,
            diagnostics: diagnostics);
    }
}
