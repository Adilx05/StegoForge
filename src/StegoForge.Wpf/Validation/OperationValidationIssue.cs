using StegoForge.Core.Errors;

namespace StegoForge.Wpf.Validation;

public sealed record OperationValidationIssue(string PropertyName, StegoErrorCode Code, string Message);
