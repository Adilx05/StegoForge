using System.Diagnostics;
using StegoForge.Application.Formats;
using StegoForge.Application.Payload;
using StegoForge.Application.Policies;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;
using StegoForge.Core.Payload;

namespace StegoForge.Application.Extract;

public sealed class ExtractService(
    CarrierFormatResolver formatResolver,
    PayloadOrchestrationService orchestrationService,
    IPayloadEnvelopeSerializer envelopeSerializer,
    OperationPolicyGate policyGate) : IExtractService
{
    private const string ProviderId = "stegoforge.application.extract-service";

    public async Task<ExtractResponse> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var started = Stopwatch.StartNew();
        policyGate.ValidateExtractRequest(request);

        await using var carrierStream = File.OpenRead(request.CarrierPath);
        var handler = formatResolver.Resolve(carrierStream);
        carrierStream.Position = 0;

        var envelopeBytes = await handler.ExtractAsync(carrierStream, cancellationToken).ConfigureAwait(false);
        var envelope = envelopeSerializer.Deserialize(envelopeBytes);

        var passphrase = policyGate.ResolvePassphrase(request.PasswordOptions);
        var payload = orchestrationService.ExtractPayload(envelope, request.ProcessingOptions, request.PasswordOptions, passphrase);

        var warnings = new List<string>();
        var resolvedOutputPath = ResolveOutputPath(request.OutputPath, envelope, warnings);
        policyGate.EnsureOutputPolicy(resolvedOutputPath, request.ProcessingOptions.OverwriteBehavior);

        await File.WriteAllBytesAsync(resolvedOutputPath, payload, cancellationToken).ConfigureAwait(false);

        started.Stop();

        var wasEncrypted = IsEncryptedEnvelope(envelope);
        var diagnostics = new OperationDiagnostics(
            warnings: warnings,
            notes:
            [
                $"Resolved carrier format: {handler.Format}.",
                $"Envelope bytes extracted: {envelopeBytes.LongLength}.",
                $"Compression descriptor: {envelope.Header.CompressionDescriptor}.",
                $"Encryption descriptor: {envelope.Header.EncryptionDescriptor}."
            ],
            duration: started.Elapsed,
            algorithmIdentifier: $"cmp:{envelope.Header.CompressionDescriptor}|enc:{envelope.Header.EncryptionDescriptor}",
            providerIdentifier: ProviderId);

        return new ExtractResponse(
            outputPath: request.OutputPath,
            resolvedOutputPath: resolvedOutputPath,
            carrierFormatId: handler.Format,
            payload: payload,
            wasCompressed: envelope.Flags.HasFlag(EnvelopeFlags.Compressed),
            wasEncrypted: wasEncrypted,
            originalFileName: envelope.Header.OriginalFileName,
            preservedOriginalFileName: !string.Equals(request.OutputPath, resolvedOutputPath, StringComparison.Ordinal),
            integrityVerificationResult: wasEncrypted ? IntegrityVerificationResult.Verified : IntegrityVerificationResult.NotApplicable,
            warnings: warnings,
            diagnostics: diagnostics);
    }

    private static string ResolveOutputPath(string requestedOutputPath, PayloadEnvelope envelope, List<string> warnings)
    {
        var originalFileName = envelope.Header.OriginalFileName;
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return requestedOutputPath;
        }

        if (!Directory.Exists(requestedOutputPath))
        {
            return requestedOutputPath;
        }

        if (originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            warnings.Add("Embedded original filename is invalid on this platform and was ignored.");
            return requestedOutputPath;
        }

        return Path.Combine(requestedOutputPath, originalFileName);
    }

    private static bool IsEncryptedEnvelope(PayloadEnvelope envelope)
        => envelope.Flags.HasFlag(EnvelopeFlags.Encrypted)
            || !string.Equals(envelope.Header.EncryptionDescriptor, "none", StringComparison.OrdinalIgnoreCase);
}
