using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application;
using StegoForge.Cli;

var services = new ServiceCollection()
    .AddStegoForgeApplicationServices()
    .BuildServiceProvider();

return await CliApplication.RunAsync(args, services).ConfigureAwait(false);
