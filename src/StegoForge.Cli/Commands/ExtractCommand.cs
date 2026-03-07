using System.CommandLine;
using StegoForge.Application.Diagnostics;
using StegoForge.Cli.Output;
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

        command.SetAction(async parseResult =>
        {
            var carrierPath = parseResult.GetValueForOption(carrierOption)!;
            var outputPath = parseResult.GetValueForOption(outputOption)!;
            var encrypt = parseResult.GetValueForOption(encryptOption) ?? "optional";
            var compress = parseResult.GetValueForOption(compressOption) ?? "auto";
            var password = parseResult.GetValueForOption(passwordOption);
            var json = parseResult.GetValueForOption(jsonOption);
            var quiet = parseResult.GetValueForOption(quietOption);
            var verbose = parseResult.GetValueForOption(verboseOption);

            var diagnostics = DiagnosticContext.Create("extract", CommandExecution.DeriveCarrierFormatHint(carrierPath));

            return await CommandExecution.ExecuteAsync(async cancellationToken =>
            {
                var request = new ExtractRequest(
                    carrierPath,
                    outputPath,
                    processingOptions: CommandExecution.BuildProcessingOptions(compress, encrypt, quiet, verbose),
                    passwordOptions: CommandExecution.BuildPasswordOptions(password));

                var response = await extractService.ExtractAsync(request, cancellationToken).ConfigureAwait(false);
                return (ICommandOutput)new ExtractCommandOutput(
                    Command: "extract",
                    OutputPath: response.OutputPath,
                    ResolvedOutputPath: response.ResolvedOutputPath,
                    CarrierFormatId: response.CarrierFormatId,
                    PayloadSizeBytes: response.PayloadSizeBytes,
                    WasCompressed: response.WasCompressed,
                    WasEncrypted: response.WasEncrypted,
                    OriginalFileName: response.OriginalFileName,
                    IntegrityVerificationResult: response.IntegrityVerificationResult,
                    Warnings: response.Warnings,
                    Diagnostics: response.Diagnostics);
            }, json, diagnostics).ConfigureAwait(false);
        });

        return command;
    }
}
