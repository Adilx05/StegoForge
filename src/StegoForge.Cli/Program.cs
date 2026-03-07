using Microsoft.Extensions.DependencyInjection;
using StegoForge.Application;
using StegoForge.Core.Abstractions;
using StegoForge.Cli;

try
{
    var services = new ServiceCollection()
        .AddStegoForgeApplicationServices()
        .BuildServiceProvider();

    _ = services.GetRequiredService<IEmbedService>();
    _ = services.GetRequiredService<IExtractService>();
    _ = services.GetRequiredService<IInfoService>();
    _ = services.GetRequiredService<ICapacityService>();

    Console.WriteLine("StegoForge CLI baseline ready.");
    return 0;
}
catch (Exception exception)
{
    var failure = CliErrorContract.CreateFailureFromException(exception);
    Console.Error.WriteLine(failure.Message);
    return failure.ExitCode;
}
