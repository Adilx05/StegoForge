using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Application.Validation;

public sealed class OperationPolicyValidator
{
    public void ValidateEmbedRequest(EmbedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCarrierExists(request.CarrierPath);
        EnsureSupportedPasswordOptions(request.PasswordOptions);
        EnsureProcessingOptionsAreDefined(request.ProcessingOptions);
        EnsurePasswordAndModeCompatibility(request.ProcessingOptions, request.PasswordOptions);
        EnsureCompressionAndEncryptionModeCompatibility(request.ProcessingOptions);
        EnsureEncryptionRequiredHasPasswordSource(request.ProcessingOptions, request.PasswordOptions);
        EnsureOutputPolicy(request.OutputPath, request.ProcessingOptions.OverwriteBehavior);
    }

    public void ValidateExtractRequest(ExtractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCarrierExists(request.CarrierPath);
        EnsureSupportedPasswordOptions(request.PasswordOptions);
        EnsureProcessingOptionsAreDefined(request.ProcessingOptions);
        EnsurePasswordAndModeCompatibility(request.ProcessingOptions, request.PasswordOptions);
        EnsureCompressionAndEncryptionModeCompatibility(request.ProcessingOptions);
        EnsureEncryptionRequiredHasPasswordSource(request.ProcessingOptions, request.PasswordOptions);
        EnsureExtractOutputPolicy(request);
    }

    public void ValidateInfoRequest(InfoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureCarrierExists(request.CarrierPath);
        EnsureProcessingOptionsAreDefined(request.ProcessingOptions);
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
                ?? throw new InvalidArgumentsException($"Password environment variable '{passwordOptions.SourceReference}' was not found or empty."),
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

    private static void EnsureProcessingOptionsAreDefined(ProcessingOptions processingOptions)
    {
        if (!Enum.IsDefined(processingOptions.CompressionMode))
        {
            throw new InvalidArgumentsException($"Unsupported compression mode '{processingOptions.CompressionMode}'.");
        }

        if (!Enum.IsDefined(processingOptions.EncryptionMode))
        {
            throw new InvalidArgumentsException($"Unsupported encryption mode '{processingOptions.EncryptionMode}'.");
        }

        if (!Enum.IsDefined(processingOptions.OverwriteBehavior))
        {
            throw new InvalidArgumentsException($"Unsupported overwrite behavior '{processingOptions.OverwriteBehavior}'.");
        }
    }

    private static void EnsurePasswordAndModeCompatibility(ProcessingOptions processingOptions, PasswordOptions passwordOptions)
    {
        if (processingOptions.EncryptionMode == EncryptionMode.None && passwordOptions.Requirement == PasswordRequirement.Required)
        {
            throw new InvalidArgumentsException("Password requirement cannot be 'Required' when encryption mode is 'None'.");
        }

        if (processingOptions.EncryptionMode == EncryptionMode.Required && passwordOptions.Requirement == PasswordRequirement.Optional)
        {
            throw new InvalidArgumentsException("Password requirement cannot be 'Optional' when encryption mode is 'Required'.");
        }
    }


    private static void EnsureCompressionAndEncryptionModeCompatibility(ProcessingOptions processingOptions)
    {
        if (processingOptions.CompressionMode == CompressionMode.Disabled && processingOptions.CompressionLevel != 0)
        {
            throw new InvalidArgumentsException("Compression mode 'Disabled' requires compression level 0.");
        }
    }

    private static void EnsureEncryptionRequiredHasPasswordSource(ProcessingOptions processingOptions, PasswordOptions passwordOptions)
    {
        if (processingOptions.EncryptionMode != EncryptionMode.Required)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(passwordOptions.SourceReference))
        {
            throw new InvalidArgumentsException("Encryption mode 'Required' requires a password source reference.");
        }

    }

    private void EnsureExtractOutputPolicy(ExtractRequest request)
    {
        EnsureOutputPolicy(request.OutputPath, request.ProcessingOptions.OverwriteBehavior);
    }
}
