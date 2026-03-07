using System.CommandLine;
using StegoForge.Core.Abstractions;

namespace StegoForge.Cli.Commands;

public sealed class CapacityCommand(ICapacityService capacityService)
{
    public Command Build()
    {
        var command = new Command("capacity", "Estimate available capacity for a carrier file.\nExample: stegoforge capacity --carrier in.png --payload 2048 --json");

        var carrierOption = CommonCliOptions.CarrierPathOption();
        var payloadOption = new Option<long>(["--payload", "-p"], () => 0L, "Payload size in bytes to evaluate against capacity.");
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

        command.SetAction(async context =>
        {
            var carrierPath = context.ParseResult.GetValueForOption(carrierOption)!;
            var payloadSize = context.ParseResult.GetValueForOption(payloadOption);
            var encrypt = context.ParseResult.GetValueForOption(encryptOption)!;
            var compress = context.ParseResult.GetValueForOption(compressOption)!;
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

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
