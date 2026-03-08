using System.CommandLine;
using System.Reflection;
using StegoForge.Application.Diagnostics;
using StegoForge.Cli.Output;

namespace StegoForge.Cli.Commands;

public sealed class VersionCommand
{
    public Command Build()
    {
        var command = new Command("version", "Show StegoForge CLI version information.\nExample: stegoforge version --json");
        var jsonOption = CommonCliOptions.JsonOption();

        command.AddOption(jsonOption);

        command.SetAction(async parseResult =>
        {
            var json = parseResult.GetValueForOption(jsonOption);
            var diagnostics = DiagnosticContext.Create("version", "unknown");
            return await CommandExecution.ExecuteAsync(_ =>
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var payload = new VersionCommandOutput(
                    Command: "version",
                    Name: assembly.Name ?? "StegoForge.Cli",
                    Version: assembly.Version?.ToString() ?? "unknown");

                return Task.FromResult<ICommandOutput>(payload);
            }, json, diagnostics).ConfigureAwait(false);
        });

        return command;
    }
}
