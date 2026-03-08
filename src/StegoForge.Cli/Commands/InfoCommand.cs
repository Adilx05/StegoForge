using System.CommandLine;
using StegoForge.Application.Diagnostics;
using StegoForge.Cli.Output;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Cli.Commands;

public sealed class InfoCommand(IInfoService infoService)
{
    public Command Build()
    {
        var command = new Command("info", "Inspect carrier metadata and embedded payload hints.\nExample: stegoforge info --carrier out.png --json");

        var carrierOption = CommonCliOptions.CarrierPathOption();
        var encryptOption = CommonCliOptions.EncryptOption();
        var compressOption = CommonCliOptions.CompressOption();
        var jsonOption = CommonCliOptions.JsonOption();
        var quietOption = CommonCliOptions.QuietOption();
        var verboseOption = CommonCliOptions.VerboseOption();

        command.AddOption(carrierOption);
        command.AddOption(encryptOption);
        command.AddOption(compressOption);
        command.AddOption(jsonOption);
        command.AddOption(quietOption);
        command.AddOption(verboseOption);

        command.SetAction(async parseResult =>
        {
            var carrierPath = parseResult.GetValueForOption(carrierOption)!;
            var encrypt = parseResult.GetValueForOption(encryptOption) ?? "optional";
            var compress = parseResult.GetValueForOption(compressOption) ?? "auto";
            var json = parseResult.GetValueForOption(jsonOption);
            var quiet = parseResult.GetValueForOption(quietOption);
            var verbose = parseResult.GetValueForOption(verboseOption);

            var diagnostics = DiagnosticContext.Create("info", CommandExecution.DeriveCarrierFormatHint(carrierPath));

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var request = new InfoRequest(
                    carrierPath,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose));

                var response = await infoService.GetInfoAsync(request, cancellationToken).ConfigureAwait(false);
                return (ICommandOutput)new InfoCommandOutput(
                    Command: "info",
                    FormatId: response.FormatId,
                    FormatDetails: response.FormatDetails,
                    CarrierSizeBytes: response.CarrierSizeBytes,
                    EstimatedCapacityBytes: response.EstimatedCapacityBytes,
                    AvailableCapacityBytes: response.AvailableCapacityBytes,
                    EmbeddedDataPresent: response.EmbeddedDataPresent,
                    SupportsEncryption: response.SupportsEncryption,
                    SupportsCompression: response.SupportsCompression,
                    PayloadMetadata: response.PayloadMetadata,
                    ProtectionDescriptors: response.ProtectionDescriptors,
                    Diagnostics: response.Diagnostics);
            }, json, diagnostics).ConfigureAwait(false);
        });

        return command;
    }
}
