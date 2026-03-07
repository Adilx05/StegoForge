using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application.Diagnostics;
using StegoForge.Cli.Commands;
using StegoForge.Cli.Output;
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
            var exitCode = CliErrorContract.GetExitCode(parserError.Code);
            var formatter = HasJsonFlag(args)
                ? (IOutputFormatter)new JsonOutputFormatter(Console.Out, errorWriter ?? Console.Error)
                : new TextOutputFormatter(Console.Out, errorWriter ?? Console.Error);

            var diagnostics = DiagnosticContext.Create(ResolveOperationType(args), "unknown");
            var sanitized = SanitizedErrorDiagnostics.From(parserError, diagnostics);
            await formatter.WriteFailureAsync(new CliCommandFailure(exitCode, parserError, sanitized.ToCliText(), sanitized)).ConfigureAwait(false);
            return exitCode;
        }

        return await parseResult.InvokeAsync().ConfigureAwait(false);
    }

    private static bool HasJsonFlag(string[] args)
        => args.Any(static arg => string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase));

    private static string ResolveOperationType(string[] args)
    {
        if (args.Length == 0)
        {
            return "root";
        }

        var first = args[0].Trim().ToLowerInvariant();
        return first switch
        {
            "embed" or "extract" or "capacity" or "info" or "version" or "help" => first,
            _ => "root"
        };
    }
}
