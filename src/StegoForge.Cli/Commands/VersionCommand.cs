using System.CommandLine;
using System.Reflection;

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
            return await CommandExecution.ExecuteAsync(_ =>
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var payload = new
                {
                    command = "version",
                    name = assembly.Name,
                    version = assembly.Version?.ToString() ?? "unknown"
                };

                return Task.FromResult<object>(payload);
            }, json).ConfigureAwait(false);
        });

        return command;
    }
}
