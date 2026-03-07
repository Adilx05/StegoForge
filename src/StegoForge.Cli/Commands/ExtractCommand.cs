using System.CommandLine;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Models;

namespace StegoForge.Cli.Commands;

public sealed class ExtractCommand(IExtractService extractService)
{
    public Command Build()
    {
        var command = new Command("extract", "Extract payload data from a carrier file.\nExample: stegoforge extract --carrier out.png --out recovered.bin --password secret --json");

        var carrierOption = CommonCliOptions.CarrierPathOption();
        var outputOption = CommonCliOptions.OutputPathOption();
        var encryptOption = CommonCliOptions.EncryptOption();
        var compressOption = CommonCliOptions.CompressOption();
        var passwordOption = CommonCliOptions.PasswordOption();
        var jsonOption = CommonCliOptions.JsonOption();
        var quietOption = CommonCliOptions.QuietOption();
        var verboseOption = CommonCliOptions.VerboseOption();

        command.AddOption(carrierOption);
        command.AddOption(outputOption);
        command.AddOption(encryptOption);
        command.AddOption(compressOption);
        command.AddOption(passwordOption);
        command.AddOption(jsonOption);
        command.AddOption(quietOption);
        command.AddOption(verboseOption);

        command.SetAction(async context =>
        {
            var carrierPath = context.ParseResult.GetValueForOption(carrierOption)!;
            var outputPath = context.ParseResult.GetValueForOption(outputOption)!;
            var encrypt = context.ParseResult.GetValueForOption(encryptOption)!;
            var compress = context.ParseResult.GetValueForOption(compressOption)!;
            var password = context.ParseResult.GetValueForOption(passwordOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var request = new ExtractRequest(
                    carrierPath,
                    outputPath,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose),
                    passwordOptions: CommandExecution.BuildPasswordOptions(password));

                var response = await extractService.ExtractAsync(request, cancellationToken).ConfigureAwait(false);
                return new
                {
                    command = "extract",
                    response.OutputPath,
                    response.ResolvedOutputPath,
                    response.CarrierFormatId,
                    response.PayloadSizeBytes,
                    response.WasCompressed,
                    response.WasEncrypted,
                    response.OriginalFileName,
                    response.IntegrityVerificationResult,
                    response.Warnings,
                    response.Diagnostics
                };
            }, json).ConfigureAwait(false);
        });

        return command;
    }
}
