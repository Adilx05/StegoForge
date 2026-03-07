using System.Diagnostics;
using StegoForge.Application.Formats;
using StegoForge.Application.Policies;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Application.Info;

public sealed class InfoService(
    CarrierFormatResolver formatResolver,
    IPayloadEnvelopeSerializer envelopeSerializer,
    OperationPolicyGate policyGate) : IInfoService
{
    private const string ProviderId = "stegoforge.application.info-service";

    public async Task<CarrierInfoResponse> GetInfoAsync(InfoRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var started = Stopwatch.StartNew();
        policyGate.ValidateInfoRequest(request);

        await using var stream = File.OpenRead(request.CarrierPath);
        var handler = formatResolver.Resolve(stream);
        stream.Position = 0;

        var handlerInfo = await handler.GetInfoAsync(stream, cancellationToken).ConfigureAwait(false);

        var warnings = new List<string>();
        var notes = handlerInfo.Diagnostics.Notes.ToList();

        var embeddedDataPresent = handlerInfo.EmbeddedDataPresent;
        var payloadMetadata = handlerInfo.PayloadMetadata;
        var protection = handlerInfo.ProtectionDescriptors;
        string algorithmId = $"cmp:{handlerInfo.ProtectionDescriptors.CompressionDescriptor}|enc:{handlerInfo.ProtectionDescriptors.EncryptionDescriptor}";

        stream.Position = 0;
        try
        {
            var envelopeBytes = await handler.ExtractAsync(stream, cancellationToken).ConfigureAwait(false);
            var envelope = envelopeSerializer.Deserialize(envelopeBytes);
            embeddedDataPresent = true;
            payloadMetadata = new PayloadMetadataSummary(
                originalFileName: envelope.Header.OriginalFileName,
                originalSizeBytes: envelope.Header.OriginalSizeBytes,
                createdUtc: envelope.Header.CreatedUtc,
                headerVersion: (int)envelope.Version);
            protection = new PayloadProtectionDescriptors(
                compressionDescriptor: envelope.Header.CompressionDescriptor,
                encryptionDescriptor: envelope.Header.EncryptionDescriptor,
                integrityDescriptor: envelope.IntegrityData.Length > 0 ? "tagged" : "none");
            algorithmId = $"cmp:{envelope.Header.CompressionDescriptor}|enc:{envelope.Header.EncryptionDescriptor}";
            notes.Add($"Parsed payload envelope schema v{(int)envelope.Version}.");
        }
        catch (StegoForgeException exception) when (exception is CorruptedDataException or InvalidHeaderException or InvalidPayloadException)
        {
            warnings.Add("Embedded payload metadata could not be parsed. The carrier may not contain a valid StegoForge payload envelope.");
        }

        started.Stop();

        var diagnostics = new OperationDiagnostics(
            warnings: warnings.Count == 0 ? handlerInfo.Diagnostics.Warnings : [.. handlerInfo.Diagnostics.Warnings, .. warnings],
            notes: [.. notes, $"Resolved carrier format: {handler.Format}."],
            duration: started.Elapsed,
            algorithmIdentifier: algorithmId,
            providerIdentifier: ProviderId);

        return new CarrierInfoResponse(
            formatId: handlerInfo.FormatId,
            formatDetails: handlerInfo.FormatDetails,
            carrierSizeBytes: handlerInfo.CarrierSizeBytes,
            estimatedCapacityBytes: handlerInfo.EstimatedCapacityBytes,
            availableCapacityBytes: handlerInfo.AvailableCapacityBytes,
            embeddedDataPresent: embeddedDataPresent,
            supportsEncryption: handlerInfo.SupportsEncryption,
            supportsCompression: handlerInfo.SupportsCompression,
            payloadMetadata: payloadMetadata,
            protectionDescriptors: protection,
            diagnostics: diagnostics);
    }
}
