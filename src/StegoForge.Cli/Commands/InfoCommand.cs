using System.CommandLine;
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

        command.SetAction(async context =>
        {
            var carrierPath = context.ParseResult.GetValueForOption(carrierOption)!;
            var encrypt = context.ParseResult.GetValueForOption(encryptOption)!;
            var compress = context.ParseResult.GetValueForOption(compressOption)!;
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var request = new InfoRequest(
                    carrierPath,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose));

                var response = await infoService.GetInfoAsync(request, cancellationToken).ConfigureAwait(false);
                return new
                {
                    command = "info",
                    response.FormatId,
                    response.FormatDetails,
                    response.CarrierSizeBytes,
                    response.EstimatedCapacityBytes,
                    response.AvailableCapacityBytes,
                    response.EmbeddedDataPresent,
                    response.SupportsEncryption,
                    response.SupportsCompression,
                    response.PayloadMetadata,
                    response.ProtectionDescriptors,
                    response.Diagnostics
                };
            }, json, context).ConfigureAwait(false);
        });

        return command;
    }
}
