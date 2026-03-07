using System.IO;
using StegoForge.Application.Validation;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;

namespace StegoForge.Wpf.Validation;

public sealed class UiOperationPolicyValidator
{
    private readonly OperationPolicyValidator _validator;

    public UiOperationPolicyValidator(OperationPolicyValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    public OperationValidationResult ValidateEmbed(
        string carrierPath,
        string payloadPath,
        string outputPath,
        bool requireEncryption,
        string? password,
        bool allowOverwrite)
    {
        var issues = new List<OperationValidationIssue>();

        AddRequiredFieldIssue(issues, "CarrierPath", carrierPath, "Carrier file is required.");
        AddRequiredFieldIssue(issues, "PayloadPath", payloadPath, "Payload file is required.");
        AddRequiredFieldIssue(issues, "OutputPath", outputPath, "Output file is required.");

        if (issues.Count > 0)
        {
            return new OperationValidationResult(issues);
        }

        if (!File.Exists(payloadPath))
        {
            issues.Add(new OperationValidationIssue("PayloadPath", StegoErrorCode.FileNotFound, $"File not found: {payloadPath}"));
            return new OperationValidationResult(issues);
        }

        var processingOptions = new ProcessingOptions(
            encryptionMode: requireEncryption ? EncryptionMode.Required : EncryptionMode.Optional,
            overwriteBehavior: allowOverwrite ? OverwriteBehavior.Allow : OverwriteBehavior.Disallow);

        var passwordOptions = string.IsNullOrWhiteSpace(password)
            ? new PasswordOptions(
                requirement: requireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                sourceHint: PasswordSourceHint.None,
                sourceReference: null)
            : new PasswordOptions(
                requirement: requireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                sourceHint: PasswordSourceHint.Prompt,
                sourceReference: password);

        TryValidatePolicy(() =>
        {
            var request = new EmbedRequest(carrierPath, outputPath, payload: [0x01], processingOptions, passwordOptions);
            _validator.ValidateEmbedRequest(request);
        }, issues, "OutputPath");

        return new OperationValidationResult(issues);
    }

    public OperationValidationResult ValidateExtract(
        string carrierPath,
        string outputPath,
        bool requireEncryption,
        string? password,
        bool allowOverwrite)
    {
        var issues = new List<OperationValidationIssue>();

        AddRequiredFieldIssue(issues, "CarrierPath", carrierPath, "Carrier file is required.");
        AddRequiredFieldIssue(issues, "OutputPath", outputPath, "Output file/folder is required.");

        if (issues.Count > 0)
        {
            return new OperationValidationResult(issues);
        }

        var processingOptions = new ProcessingOptions(
            encryptionMode: requireEncryption ? EncryptionMode.Required : EncryptionMode.Optional,
            overwriteBehavior: allowOverwrite ? OverwriteBehavior.Allow : OverwriteBehavior.Disallow);

        var passwordOptions = string.IsNullOrWhiteSpace(password)
            ? new PasswordOptions(
                requirement: requireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                sourceHint: PasswordSourceHint.None,
                sourceReference: null)
            : new PasswordOptions(
                requirement: requireEncryption ? PasswordRequirement.Required : PasswordRequirement.Optional,
                sourceHint: PasswordSourceHint.Prompt,
                sourceReference: password);

        TryValidatePolicy(() =>
        {
            var request = new ExtractRequest(carrierPath, outputPath, processingOptions, passwordOptions);
            _validator.ValidateExtractRequest(request);
        }, issues, "OutputPath");

        return new OperationValidationResult(issues);
    }

    private static void AddRequiredFieldIssue(
        ICollection<OperationValidationIssue> issues,
        string propertyName,
        string value,
        string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        issues.Add(new OperationValidationIssue(propertyName, StegoErrorCode.InvalidArguments, message));
    }

    private static void TryValidatePolicy(Action action, ICollection<OperationValidationIssue> issues, string defaultPropertyName)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var mappedError = StegoErrorMapper.FromException(ex);
            var propertyName = ResolvePropertyName(ex, defaultPropertyName);
            issues.Add(new OperationValidationIssue(propertyName, mappedError.Code, mappedError.Message));
        }
    }

    private static string ResolvePropertyName(Exception exception, string defaultPropertyName)
    {
        return exception switch
        {
            FileNotFoundStegoException => "CarrierPath",
            OutputExistsException => "OutputPath",
            InvalidArgumentsException => defaultPropertyName,
            _ => defaultPropertyName
        };
    }
}
