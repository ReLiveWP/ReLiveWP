namespace ReLiveWP.Backend.Mailbox.Validation;

public enum ValidationSeverity
{
    // The rule mutated the entity in place to make it conform; the write proceeds.
    Corrected,
    // The entity cannot be made valid without guessing; the write is refused (live) or the
    // item is quarantined (reconciliation sweep).
    Rejected,
}

public record ValidationIssue(string Rule, string Field, string Message, ValidationSeverity Severity)
{
    public static ValidationIssue Corrected(string rule, string field, string message) =>
        new(rule, field, message, ValidationSeverity.Corrected);

    public static ValidationIssue Rejected(string rule, string field, string message) =>
        new(rule, field, message, ValidationSeverity.Rejected);
}
