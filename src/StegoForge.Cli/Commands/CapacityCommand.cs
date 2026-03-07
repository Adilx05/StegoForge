using System.CommandLine;
using StegoForge.Core.Abstractions;

namespace StegoForge.Cli.Commands;

public sealed class CapacityCommand(ICapacityService capacityService)
{
    public Command Build()
    {
        var command = new Command("capacity", "Estimate available capacity for a carrier file.\nExample: stegoforge capacity --carrier in.png --payload 2048 --json");

        var carrierOption = CommonCliOptions.CarrierPathOption();
        var payloadOption = new Option<long>("--payload", "Payload size in bytes to evaluate against capacity.");
        payloadOption.AddAlias("-p");

        var encryptOption = CommonCliOptions.EncryptOption();
        var compressOption = CommonCliOptions.CompressOption();
        var jsonOption = CommonCliOptions.JsonOption();
        var quietOption = CommonCliOptions.QuietOption();
        var verboseOption = CommonCliOptions.VerboseOption();

        command.AddOption(carrierOption);
        command.AddOption(payloadOption);
        command.AddOption(encryptOption);
        command.AddOption(compressOption);
        command.AddOption(jsonOption);
        command.AddOption(quietOption);
        command.AddOption(verboseOption);

        command.SetAction(async parseResult =>
        {
            var carrierPath = parseResult.GetValueForOption(carrierOption)!;
            var payloadSize = parseResult.GetValueForOption(payloadOption);
            var encrypt = parseResult.GetValueForOption(encryptOption) ?? "optional";
            var compress = parseResult.GetValueForOption(compressOption) ?? "auto";
            var json = parseResult.GetValueForOption(jsonOption);
            var quiet = parseResult.GetValueForOption(quietOption);
            var verbose = parseResult.GetValueForOption(verboseOption);

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var request = new Core.Models.CapacityRequest(
                    carrierPath,
                    payloadSize,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose));

                var response = await capacityService.GetCapacityAsync(request, cancellationToken).ConfigureAwait(false);
                return new
                {
                    command = "capacity",
                    response.CarrierFormatId,
                    response.RequestedPayloadSizeBytes,
                    response.AvailableCapacityBytes,
                    response.MaximumCapacityBytes,
                    response.SafeUsableCapacityBytes,
                    response.EstimatedOverheadBytes,
                    response.CanEmbed,
                    response.RemainingBytes,
                    response.FailureReason,
                    response.ConstraintBreakdown,
                    response.Diagnostics
                };
            }, json).ConfigureAwait(false);
        });

        return command;
    }
}
