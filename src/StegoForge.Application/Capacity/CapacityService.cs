using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using StegoForge.Formats.Png;

namespace StegoForge.Application.Capacity;

public sealed class CapacityService(PngLsbCapacityAnalyzer pngAnalyzer) : ICapacityService
{
    public async Task<CapacityResponse> GetCapacityAsync(CapacityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(request.CarrierPath))
        {
            throw new InvalidArgumentsException($"Carrier file '{request.CarrierPath}' was not found.");
        }

        await using var stream = File.OpenRead(request.CarrierPath);

        var analysis = await pngAnalyzer.AnalyzeAsync(
            stream,
            requestedPayloadBytes: request.PayloadSizeBytes,
            reservedEnvelopeOverheadBytes: PngLsbCapacityCalculator.DefaultReservedEnvelopeOverheadBytes,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var canEmbed = analysis.Estimate.CanEmbedRequestedPayload;
        var remainingBytes = analysis.Estimate.SafeUsableBytes - request.PayloadSizeBytes;
        var failureReason = canEmbed
            ? null
            : $"Payload exceeds safe PNG capacity by {request.PayloadSizeBytes - analysis.Estimate.SafeUsableBytes} byte(s).";

        var diagnostics = new OperationDiagnostics(
            notes:
            [
                $"PNG dimensions: {analysis.Width}x{analysis.Height}.",
                $"LSB channels used: {analysis.ChannelsUsed} (RGB only).",
                $"PNG color type: {analysis.ColorType}."
            ],
            providerIdentifier: nameof(PngLsbCapacityAnalyzer));

        return new CapacityResponse(
            carrierFormatId: "png-lsb-v1",
            requestedPayloadSizeBytes: request.PayloadSizeBytes,
            availableCapacityBytes: analysis.Estimate.SafeUsableBytes,
            maximumCapacityBytes: analysis.Estimate.MaximumRawEmbeddableBytes,
            safeUsableCapacityBytes: analysis.Estimate.SafeUsableBytes,
            estimatedOverheadBytes: analysis.Estimate.ReservedEnvelopeOverheadBytes,
            canEmbed: canEmbed,
            remainingBytes: remainingBytes,
            failureReason: failureReason,
            constraintBreakdown: analysis.Estimate.ConstraintDiagnostics,
            diagnostics: diagnostics);
    }
}
