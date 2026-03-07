using System.CommandLine;

namespace StegoForge.Cli.Commands;

internal static class SystemCommandLineCompatibilityExtensions
{
    public static void AddOption(this Command command, Option option)
        => command.Options.Add(option);

    public static void AddCommand(this Command command, Command subcommand)
        => command.Subcommands.Add(subcommand);

    public static void AddAlias(this Command command, string alias)
        => command.Aliases.Add(alias);

    public static void AddAlias(this Option option, string alias)
        => option.Aliases.Add(alias);

    public static T? GetValueForOption<T>(this ParseResult parseResult, Option<T> option)
        => parseResult.GetValue(option);
}
