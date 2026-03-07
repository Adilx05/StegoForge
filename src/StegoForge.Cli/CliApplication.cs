using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Cli.Commands;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;

namespace StegoForge.Cli;

public static class CliApplication
{
    public static RootCommand BuildRootCommand(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var root = new RootCommand("StegoForge CLI for embedding, extracting, and inspecting steganographic carriers.")
        {
            new EmbedCommand(services.GetRequiredService<IEmbedService>()).Build(),
            new ExtractCommand(services.GetRequiredService<IExtractService>()).Build(),
            new CapacityCommand(services.GetRequiredService<ICapacityService>()).Build(),
            new InfoCommand(services.GetRequiredService<IInfoService>()).Build(),
            new VersionCommand().Build()
        };

        root.AddAlias("stegoforge");

        var helpCommand = new Command("help", "Show root command help.\nExample: stegoforge help");
        helpCommand.SetAction(async _ => await root.Parse(new[] { "--help" }).InvokeAsync().ConfigureAwait(false));

        root.AddCommand(helpCommand);

        root.Description += "\n\nExamples:\n  stegoforge embed --carrier in.png --payload secret.bin --out out.png\n  stegoforge extract --carrier out.png --out recovered.bin\n  stegoforge capacity --carrier in.png --payload 1024\n  stegoforge info --carrier out.png\n  stegoforge version";

        return root;
    }

    public static async Task<int> RunAsync(string[] args, IServiceProvider services, TextWriter? errorWriter = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(services);

        var root = BuildRootCommand(services);
        var parseResult = root.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            var parserMessage = string.Join(" ", parseResult.Errors.Select(static error => error.Message));
            var parserError = StegoError.InvalidArguments(parserMessage);
            await (errorWriter ?? Console.Error).WriteLineAsync(CliErrorContract.FormatError(parserError)).ConfigureAwait(false);
            return CliErrorContract.GetExitCode(parserError.Code);
        }

        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }
}
