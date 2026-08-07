namespace ReLiveWP.Backend.Mailbox.Validation;

// Thrown from the SaveChanges interceptor when one or more tracked items fail a hard rule.
// Aborts the whole SaveChanges call before any SQL is issued, so nothing in the batch persists.
public sealed class ItemValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception(BuildMessage(issues))
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;

    private static string BuildMessage(IReadOnlyList<ValidationIssue> issues) =>
        "item validation failed: " +
        string.Join("; ", issues.Select(i => $"{i.Rule} ({i.Field}): {i.Message}"));
}
