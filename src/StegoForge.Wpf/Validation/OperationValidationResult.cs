namespace StegoForge.Wpf.Validation;

public sealed class OperationValidationResult
{
    public static OperationValidationResult Valid { get; } = new([]);

    public IReadOnlyList<OperationValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public OperationValidationResult(IReadOnlyList<OperationValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues;
    }
}
