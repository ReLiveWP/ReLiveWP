using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.ServiceDefaults.Contacts;

namespace ReLiveWP.Backend.Mailbox.Validation;

// pure, EF-free congruence rules; each entry corrects the entity in place or rejects it when it
// can't be safely fixed. callers decide what a rejection means: interceptor throws, sweep flags.
public static class ItemValidationRules
{
    private static readonly HashSet<byte> ValidMeetingStatus = [0, 1, 3, 5, 7, 9, 11, 13, 15];

    public static IReadOnlyList<ValidationIssue> ValidateAndCorrect(DbItem item, DbFolderType? folderType)
    {
        var issues = new List<ValidationIssue>();

        // an unknown folder type skips the check rather than guessing and risking a false rejection
        if (folderType is { } ft && ExpectedClass(ft) is { } expected)
        {
            var actual = ItemClassOf(item);
            if (expected != actual)
                issues.Add(ValidationIssue.Rejected("folder-class-congruence", nameof(DbItem.CollectionId),
                    $"a {actual} item cannot live in a {ft} folder (expects {expected})"));
        }

        switch (item)
        {
            case DbTask task: ValidateTask(task, issues); break;
            case DbNote note: ValidateNote(note, issues); break;
            case DbEmail email: ValidateEmail(email, issues); break;
            case DbCalendarItem cal: ValidateCalendar(cal, issues); break;
            case DbContactItem contact: ValidateContact(contact, issues); break;
        }

        return issues;
    }

    // null means the folder holds no single class and stays permissive
    private static string? ExpectedClass(DbFolderType type) => type switch
    {
        DbFolderType.InboxDefault or DbFolderType.DraftsDefault or DbFolderType.SentItemsDefault
            or DbFolderType.OutboxDefault or DbFolderType.DeletedItemsDefault or DbFolderType.Mail => "Email",
        DbFolderType.ContactsDefault or DbFolderType.Contacts or DbFolderType.MeContact => "Contact",
        DbFolderType.CalendarDefault or DbFolderType.Calendar => "Calendar",
        DbFolderType.TasksDefault or DbFolderType.Task => "Task",
        DbFolderType.NotesDefault or DbFolderType.Notes => "Note",
        _ => null,
    };

    private static string ItemClassOf(DbItem item) => item switch
    {
        DbEmail => "Email",
        DbContactItem => "Contact",
        DbCalendarItem => "Calendar",
        DbTask => "Task",
        DbNote => "Note",
        _ => "Unknown",
    };

    private static void ValidateTask(DbTask t, List<ValidationIssue> issues)
    {
        if (t.Complete == true && t.DateCompleted is null)
        {
            t.DateCompleted = DateTime.UtcNow;
            issues.Add(ValidationIssue.Corrected("task-complete-datecompleted", nameof(t.DateCompleted),
                "stamped DateCompleted for a task marked complete without one"));
        }
        else if (t.Complete != true && t.DateCompleted is not null)
        {
            t.DateCompleted = null;
            issues.Add(ValidationIssue.Corrected("task-complete-datecompleted", nameof(t.DateCompleted),
                "cleared DateCompleted on a task that is not complete"));
        }

        RejectIfStartAfterDue(t.StartDate, t.DueDate, "task-date-order", issues);
        RejectIfStartAfterDue(t.UtcStartDate, t.UtcDueDate, "task-utc-date-order", issues);

        bool recurrencePresent = t.RecurrenceType.HasValue || t.RecurrenceStart.HasValue
            || t.RecurrenceUntil.HasValue || t.RecurrenceOccurrences.HasValue || t.RecurrenceInterval.HasValue
            || t.RecurrenceDayOfWeek.HasValue || t.RecurrenceDayOfMonth.HasValue
            || t.RecurrenceWeekOfMonth.HasValue || t.RecurrenceMonthOfYear.HasValue;
        if (recurrencePresent && (t.RecurrenceType is null || t.RecurrenceStart is null))
            issues.Add(ValidationIssue.Rejected("task-recurrence-required", "Recurrence",
                "task recurrence requires both Type and Start"));
    }

    private static void ValidateNote(DbNote n, List<ValidationIssue> issues)
    {
        if (string.IsNullOrEmpty(n.MessageClass) ||
            !n.MessageClass.StartsWith("IPM.StickyNote", StringComparison.OrdinalIgnoreCase))
        {
            n.MessageClass = "IPM.StickyNote";
            issues.Add(ValidationIssue.Corrected("note-message-class", nameof(n.MessageClass),
                "reset non-conforming note MessageClass to IPM.StickyNote"));
        }

        if (n.LastModifiedDate is null)
        {
            n.LastModifiedDate = DateTime.UtcNow;
            issues.Add(ValidationIssue.Corrected("note-required-defaults", nameof(n.LastModifiedDate),
                "defaulted missing LastModifiedDate"));
        }

        if (n.Subject is null)
        {
            n.Subject = string.Empty;
            issues.Add(ValidationIssue.Corrected("note-required-defaults", nameof(n.Subject),
                "defaulted missing Subject"));
        }

        RejectIfBadBodyType(n.NativeBodyType, "note-body-type", issues);
    }

    private static void ValidateEmail(DbEmail e, List<ValidationIssue> issues)
    {
        RejectIfStartAfterDue(e.FlagStartDate, e.FlagDueDate, "email-flag-date-order", issues);
        RejectIfStartAfterDue(e.FlagUtcStartDate, e.FlagUtcDueDate, "email-flag-utc-date-order", issues);

        if (e.FlagStatus == 2)
        {
            if (e.FlagDateCompleted is null)
            {
                e.FlagDateCompleted = DateTime.UtcNow;
                issues.Add(ValidationIssue.Corrected("email-flag-complete", nameof(e.FlagDateCompleted),
                    "stamped FlagDateCompleted for a completed flag"));
            }
            if (e.FlagCompleteTime is null)
            {
                e.FlagCompleteTime = e.FlagDateCompleted ?? DateTime.UtcNow;
                issues.Add(ValidationIssue.Corrected("email-flag-complete", nameof(e.FlagCompleteTime),
                    "stamped FlagCompleteTime for a completed flag"));
            }
        }

        RejectIfBadBodyType(e.BodyType, "email-body-type", issues);
    }

    private static void ValidateCalendar(DbCalendarItem c, List<ValidationIssue> issues)
    {
        if (c.MeetingStatus is { } ms && !ValidMeetingStatus.Contains(ms))
            issues.Add(ValidationIssue.Rejected("calendar-meeting-status", nameof(c.MeetingStatus),
                $"MeetingStatus {ms} is not a valid value"));
        if (c.BusyStatus is { } bs && bs > 4)
            issues.Add(ValidationIssue.Rejected("calendar-busy-status", nameof(c.BusyStatus),
                $"BusyStatus {bs} out of range 0..4"));
        if (c.Sensitivity is { } sn && sn > 3)
            issues.Add(ValidationIssue.Rejected("calendar-sensitivity", nameof(c.Sensitivity),
                $"Sensitivity {sn} out of range 0..3"));

        bool recurrencePresent = c.RecurrenceType.HasValue || c.RecurrenceUntil.HasValue
            || c.RecurrenceOccurrences.HasValue || c.RecurrenceInterval.HasValue
            || c.RecurrenceDayOfWeek.HasValue || c.RecurrenceDayOfMonth.HasValue
            || c.RecurrenceWeekOfMonth.HasValue || c.RecurrenceMonthOfYear.HasValue
            || c.RecurrenceCalendarType.HasValue || c.RecurrenceIsLeapMonth.HasValue
            || c.RecurrenceFirstDayOfWeek.HasValue;
        if (recurrencePresent && c.RecurrenceType is null)
            issues.Add(ValidationIssue.Rejected("calendar-recurrence-type-required", "Recurrence",
                "calendar recurrence requires Type"));

        if (c.RecurrenceType is { } rt)
            ValidateCalendarRecurrence(c, rt, issues);

        if (c.RecurrenceOccurrences.HasValue && c.RecurrenceUntil.HasValue)
            issues.Add(ValidationIssue.Rejected("calendar-recurrence-bound", "Recurrence",
                "Occurrences and Until are mutually exclusive"));

        if (c.RecurrenceType == 1 && c.RecurrenceFirstDayOfWeek is null)
        {
            c.RecurrenceFirstDayOfWeek = 0; // Sunday
            issues.Add(ValidationIssue.Corrected("calendar-recurrence-firstdow", nameof(c.RecurrenceFirstDayOfWeek),
                "defaulted FirstDayOfWeek for a weekly recurrence"));
        }

        if (c.AllDayEvent == true)
        {
            if (c.Timezone is not null)
            {
                c.Timezone = null;
                issues.Add(ValidationIssue.Corrected("calendar-all-day", nameof(c.Timezone),
                    "dropped Timezone on an all-day event"));
            }
            CorrectAllDayTime(c, issues);
        }

        foreach (var ex in c.Exceptions)
        {
            if (string.IsNullOrEmpty(ex.ExceptionStartTime))
                issues.Add(ValidationIssue.Rejected("calendar-exception-start", "Exception.ExceptionStartTime",
                    "each calendar exception requires ExceptionStartTime"));

            if (c.AllDayEvent == true && ex.AllDayEvent != true)
            {
                ex.AllDayEvent = true;
                issues.Add(ValidationIssue.Corrected("calendar-exception-all-day", "Exception.AllDayEvent",
                    "forced exception AllDayEvent to match the master item"));
            }
        }
    }

    private static void CorrectAllDayTime(DbCalendarItem c, List<ValidationIssue> issues)
    {
        var start = TruncateToDate(c.StartTime);
        if (start != c.StartTime)
        {
            c.StartTime = start;
            issues.Add(ValidationIssue.Corrected("calendar-all-day", nameof(c.StartTime),
                "zeroed time-of-day of StartTime on an all-day event"));
        }
        var end = TruncateToDate(c.EndTime);
        if (end != c.EndTime)
        {
            c.EndTime = end;
            issues.Add(ValidationIssue.Corrected("calendar-all-day", nameof(c.EndTime),
                "zeroed time-of-day of EndTime on an all-day event"));
        }
        var until = TruncateToDate(c.RecurrenceUntil);
        if (until != c.RecurrenceUntil)
        {
            c.RecurrenceUntil = until;
            issues.Add(ValidationIssue.Corrected("calendar-all-day", nameof(c.RecurrenceUntil),
                "zeroed time-of-day of RecurrenceUntil on an all-day event"));
        }
    }

    private static void ValidateCalendarRecurrence(DbCalendarItem c, byte type, List<ValidationIssue> issues)
    {
        bool dow = c.RecurrenceDayOfWeek.HasValue;
        bool dom = c.RecurrenceDayOfMonth.HasValue;
        bool wom = c.RecurrenceWeekOfMonth.HasValue;
        bool moy = c.RecurrenceMonthOfYear.HasValue;

        void Require(bool present, string field)
        {
            if (!present)
                issues.Add(ValidationIssue.Rejected("calendar-recurrence-required", field,
                    $"recurrence Type {type} requires {field}"));
        }
        void Forbid(bool present, string field)
        {
            if (present)
                issues.Add(ValidationIssue.Rejected("calendar-recurrence-forbidden", field,
                    $"recurrence Type {type} must not include {field}"));
        }

        switch (type)
        {
            case 0: // daily
                Forbid(dom, "DayOfMonth"); Forbid(moy, "MonthOfYear"); break;
            case 1: // weekly
                Require(dow, "DayOfWeek"); Forbid(dom, "DayOfMonth"); Forbid(moy, "MonthOfYear"); break;
            case 2: // monthly by day-of-month
                Require(dom, "DayOfMonth"); Forbid(moy, "MonthOfYear"); break;
            case 3: // monthly by nth weekday
                Require(wom, "WeekOfMonth"); Require(dow, "DayOfWeek");
                Forbid(dom, "DayOfMonth"); Forbid(moy, "MonthOfYear"); break;
            case 5: // yearly by day/month
                Require(dom, "DayOfMonth"); Require(moy, "MonthOfYear"); break;
            case 6: // yearly by nth weekday
                Require(wom, "WeekOfMonth"); Require(moy, "MonthOfYear"); Forbid(dom, "DayOfMonth"); break;
        }
    }

    public const int UnknownYear = ContactContract.UnknownBirthYear;

    private static void ValidateContact(DbContactItem c, List<ValidationIssue> issues)
    {
        if (c.Picture is { } pic)
        {
            long base64Len = ((long)pic.Length + 2) / 3 * 4;
            if (base64Len > ContactContract.MaxPictureBase64Bytes)
                issues.Add(ValidationIssue.Rejected("contact-picture-size", nameof(c.Picture),
                    $"contact picture ({base64Len} B base64) exceeds the {ContactContract.MaxPictureBase64Bytes / 1024} KB limit"));
        }

        CorrectAnnualDate(c.Birthday, v => c.Birthday = v, nameof(c.Birthday), issues);
        CorrectAnnualDate(c.Anniversary, v => c.Anniversary = v, nameof(c.Anniversary), issues);
    }

    private static void CorrectAnnualDate(
        DateTime? value, Action<DateTime?> assign, string field, List<ValidationIssue> issues)
    {
        if (value is not { } date) return;

        if (date.Year < UnknownYear)
        {
            issues.Add(ValidationIssue.Corrected("contact-unknown-year", field,
                $"clamped an implausible year ({date.Year}) to {UnknownYear}; the provider meant the year was unknown"));

            date = new DateTime(UnknownYear, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        // MS-ASCNTC 2.2.2.6 and 2.2.2.3: the time is meant to be ignored, and midday is what keeps a
        // client that ignores it anyway from landing on the day before
        var anchored = new DateTime(date.Year, date.Month, date.Day, 11, 59, 0, DateTimeKind.Utc);
        if (anchored == date && date.Kind == DateTimeKind.Utc) return;

        if (anchored != date)
            issues.Add(ValidationIssue.Corrected("contact-annual-date-time", field,
                "anchored the time to 11:59 UTC so a time zone offset cannot move the date"));

        assign(anchored);
    }

    private static void RejectIfStartAfterDue(DateTime? start, DateTime? due, string rule, List<ValidationIssue> issues)
    {
        if (start is { } s && due is { } d && s > d)
            issues.Add(ValidationIssue.Rejected(rule, "StartDate/DueDate",
                "StartDate must not be later than DueDate"));
    }

    private static void RejectIfBadBodyType(byte? type, string rule, List<ValidationIssue> issues)
    {
        if (type is { } t && (t < 1 || t > 4))
            issues.Add(ValidationIssue.Rejected(rule, "Body.Type", $"body Type {t} out of range 1..4"));
    }

    private static DateTime? TruncateToDate(DateTime? value) =>
        value is { } v ? DateTime.SpecifyKind(v.Date, v.Kind) : null;
}
