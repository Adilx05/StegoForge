using StegoForge.Cli;

try
{
    Console.WriteLine("StegoForge CLI baseline ready.");
    return 0;
}
catch (Exception exception)
{
    var failure = CliErrorContract.CreateFailureFromException(exception);
    Console.Error.WriteLine(failure.Message);
    return failure.ExitCode;
}
