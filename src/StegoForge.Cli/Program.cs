using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application;
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

    var helpCommand = new Command("help", "Show root command help.\nExample: stegoforge help");
    helpCommand.SetAction(async _ => await root.Parse(new[] { "--help" }).InvokeAsync().ConfigureAwait(false));

    root.AddCommand(helpCommand);

    root.Description += "\n\nExamples:\n  stegoforge embed --carrier in.png --payload secret.bin --out out.png\n  stegoforge extract --carrier out.png --out recovered.bin\n  stegoforge capacity --carrier in.png --payload 1024\n  stegoforge info --carrier out.png\n  stegoforge version";

    return root;
}
