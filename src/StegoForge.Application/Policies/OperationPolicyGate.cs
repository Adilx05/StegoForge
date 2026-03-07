using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Application.Policies;

public sealed class OperationPolicyGate
{
    public void ValidateEmbedRequest(EmbedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCarrierExists(request.CarrierPath);
        EnsureOutputPolicy(request.OutputPath, request.ProcessingOptions.OverwriteBehavior);
        EnsureSupportedPasswordOptions(request.PasswordOptions);
    }

    public void ValidateExtractRequest(ExtractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCarrierExists(request.CarrierPath);
        EnsureSupportedPasswordOptions(request.PasswordOptions);
    }

    public void ValidateInfoRequest(InfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureCarrierExists(request.CarrierPath);
    }

    public void ValidateCapacityRequest(CapacityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureCarrierExists(request.CarrierPath);
    }

    public void EnsureOutputPolicy(string outputPath, OverwriteBehavior overwriteBehavior)
    {
        if (!Enum.IsDefined(overwriteBehavior))
        {
            throw new InvalidArgumentsException($"Unsupported overwrite behavior '{overwriteBehavior}'.");
        }

        if (!File.Exists(outputPath))
        {
            return;
        }

        if (overwriteBehavior == OverwriteBehavior.Disallow)
        {
            throw new OutputExistsException(outputPath);
        }
    }

    public string? ResolvePassphrase(PasswordOptions passwordOptions)
    {
        ArgumentNullException.ThrowIfNull(passwordOptions);

        EnsureSupportedPasswordOptions(passwordOptions);

        if (string.IsNullOrWhiteSpace(passwordOptions.SourceReference))
        {
            return null;
        }

        return passwordOptions.SourceHint switch
        {
            PasswordSourceHint.None => passwordOptions.SourceReference,
            PasswordSourceHint.Prompt => passwordOptions.SourceReference,
            PasswordSourceHint.SecureStore => passwordOptions.SourceReference,
            PasswordSourceHint.EnvironmentVariable => Environment.GetEnvironmentVariable(passwordOptions.SourceReference)
                ?? throw new WrongPasswordException($"Password environment variable '{passwordOptions.SourceReference}' was not found or empty."),
            _ => throw new InvalidArgumentsException($"Unsupported password source hint '{passwordOptions.SourceHint}'.")
        };
    }

    private static void EnsureCarrierExists(string carrierPath)
    {
        if (!File.Exists(carrierPath))
        {
            throw new FileNotFoundStegoException(carrierPath);
        }
    }

    private static void EnsureSupportedPasswordOptions(PasswordOptions passwordOptions)
    {
        if (!Enum.IsDefined(passwordOptions.Requirement))
        {
            throw new InvalidArgumentsException($"Unsupported password requirement '{passwordOptions.Requirement}'.");
        }

        if (!Enum.IsDefined(passwordOptions.SourceHint))
        {
            throw new InvalidArgumentsException($"Unsupported password source hint '{passwordOptions.SourceHint}'.");
        }
    }
}
