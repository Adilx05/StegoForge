using System.Diagnostics;
using StegoForge.Application.Formats;
using StegoForge.Application.Payload;
using StegoForge.Application.Validation;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Application.Embed;

public sealed class EmbedService(
    CarrierFormatResolver formatResolver,
    PayloadOrchestrationService orchestrationService,
    IPayloadEnvelopeSerializer envelopeSerializer,
    OperationPolicyValidator policyGate) : IEmbedService
{
    private const string ProviderId = "stegoforge.application.embed-service";

    public async Task<EmbedResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var started = Stopwatch.StartNew();
        policyGate.ValidateEmbedRequest(request);

        var passphrase = policyGate.ResolvePassphrase(request.PasswordOptions);
        var envelope = orchestrationService.CreateEnvelopeForEmbed(
            request.Payload,
            request.ProcessingOptions,
            request.PasswordOptions,
            passphrase,
            originalFileName: null);

        var envelopeBytes = envelopeSerializer.Serialize(envelope);

        await using var carrierStream = File.OpenRead(request.CarrierPath);
        var handler = formatResolver.Resolve(carrierStream);
        carrierStream.Position = 0;

        await using var outputStream = File.Create(request.OutputPath);
        await handler.EmbedAsync(carrierStream, outputStream, envelopeBytes, cancellationToken).ConfigureAwait(false);

        started.Stop();

        var diagnostics = new OperationDiagnostics(
            notes:
            [
                $"Resolved carrier format: {handler.Format}.",
                $"Payload envelope bytes written: {envelopeBytes.LongLength}.",
                $"Compression descriptor: {envelope.Header.CompressionDescriptor}.",
                $"Encryption descriptor: {envelope.Header.EncryptionDescriptor}."
            ],
            duration: started.Elapsed,
            algorithmIdentifier: $"cmp:{envelope.Header.CompressionDescriptor}|enc:{envelope.Header.EncryptionDescriptor}",
            providerIdentifier: ProviderId);

        return new EmbedResponse(
            outputPath: request.OutputPath,
            carrierFormatId: handler.Format,
            payloadSizeBytes: request.Payload.LongLength,
            bytesEmbedded: envelopeBytes.LongLength,
            diagnostics: diagnostics);
    }
}
