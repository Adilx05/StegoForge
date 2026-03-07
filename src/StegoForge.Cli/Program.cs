using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application;
using StegoForge.Cli;
using StegoForge.Cli.Commands;
using StegoForge.Core.Abstractions;

var services = new ServiceCollection()
    .AddStegoForgeApplicationServices()
    .BuildServiceProvider();

var root = BuildRootCommand(services);
return await root.Parse(args).InvokeAsync();

static RootCommand BuildRootCommand(IServiceProvider services)
{
    var root = new RootCommand("StegoForge CLI for embedding, extracting, and inspecting steganographic carriers.")
    {
        new EmbedCommand(services.GetRequiredService<IEmbedService>()).Build(),
        new ExtractCommand(services.GetRequiredService<IExtractService>()).Build(),
        new CapacityCommand(services.GetRequiredService<ICapacityService>()).Build(),
        new InfoCommand(services.GetRequiredService<IInfoService>()).Build(),
        new VersionCommand().Build()
    };

    root.AddAlias("stegoforge");

    var helpCommand = new Command("help", "Show command help.\nExample: stegoforge help embed")
    {
        new Argument<string?>("command", () => null, "Optional command name to inspect.")
    };

    helpCommand.SetAction(async context =>
    {
        var command = context.ParseResult.GetValue<string?>("command");
        var targetArgs = string.IsNullOrWhiteSpace(command)
            ? ["--help"]
            : [command, "--help"];

        return await root.Parse(targetArgs).InvokeAsync().ConfigureAwait(false);
    });

    root.AddCommand(helpCommand);

    root.Description += "\n\nExamples:\n  stegoforge embed --carrier in.png --payload secret.bin --out out.png\n  stegoforge extract --carrier out.png --out recovered.bin\n  stegoforge capacity --carrier in.png --payload 1024\n  stegoforge info --carrier out.png\n  stegoforge version";

    return root;
}
