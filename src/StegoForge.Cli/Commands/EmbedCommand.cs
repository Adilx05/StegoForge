using System.CommandLine;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Cli.Commands;

public sealed class EmbedCommand(IEmbedService embedService)
{
    public Command Build()
    {
        var command = new Command("embed", "Embed payload data into a carrier file.\nExample: stegoforge embed --carrier in.png --payload secret.bin --out out.png --encrypt required --compress auto");

        var carrierOption = CommonCliOptions.CarrierPathOption();
        var payloadOption = CommonCliOptions.PayloadPathOption();
        var outputOption = CommonCliOptions.OutputPathOption();
        var encryptOption = CommonCliOptions.EncryptOption();
        var compressOption = CommonCliOptions.CompressOption();
        var passwordOption = CommonCliOptions.PasswordOption();
        var jsonOption = CommonCliOptions.JsonOption();
        var quietOption = CommonCliOptions.QuietOption();
        var verboseOption = CommonCliOptions.VerboseOption();

        command.AddOption(carrierOption);
        command.AddOption(payloadOption);
        command.AddOption(outputOption);
        command.AddOption(encryptOption);
        command.AddOption(compressOption);
        command.AddOption(passwordOption);
        command.AddOption(jsonOption);
        command.AddOption(quietOption);
        command.AddOption(verboseOption);

        command.SetAction(async parseResult =>
        {
            var carrierPath = parseResult.GetValueForOption(carrierOption)!;
            var payloadPath = parseResult.GetValueForOption(payloadOption)!;
            var outputPath = parseResult.GetValueForOption(outputOption)!;
            var encrypt = parseResult.GetValueForOption(encryptOption) ?? "optional";
            var compress = parseResult.GetValueForOption(compressOption) ?? "auto";
            var password = parseResult.GetValueForOption(passwordOption);
            var json = parseResult.GetValueForOption(jsonOption);
            var quiet = parseResult.GetValueForOption(quietOption);
            var verbose = parseResult.GetValueForOption(verboseOption);

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var payloadBytes = await File.ReadAllBytesAsync(payloadPath, cancellationToken).ConfigureAwait(false);
                var request = new EmbedRequest(
                    carrierPath,
                    outputPath,
                    payloadBytes,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose),
                    passwordOptions: CommandExecution.BuildPasswordOptions(password));

                var response = await embedService.EmbedAsync(request, cancellationToken).ConfigureAwait(false);
                return new
                {
                    command = "embed",
                    response.OutputPath,
                    response.CarrierFormatId,
                    response.PayloadSizeBytes,
                    response.BytesEmbedded,
                    response.Diagnostics
                };
            }, json).ConfigureAwait(false);
        });

        return command;
    }
}
