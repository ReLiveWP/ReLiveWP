using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Validation;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class ItemValidationRulesTests
{
    private static bool Rejected(IReadOnlyList<ValidationIssue> issues, string rule) =>
        issues.Any(i => i.Severity == ValidationSeverity.Rejected && i.Rule == rule);

    private static bool Corrected(IReadOnlyList<ValidationIssue> issues, string rule) =>
        issues.Any(i => i.Severity == ValidationSeverity.Corrected && i.Rule == rule);

    private static bool AnyRejected(IReadOnlyList<ValidationIssue> issues) =>
        issues.Any(i => i.Severity == ValidationSeverity.Rejected);

    [Theory]
    [InlineData(DbFolderType.TasksDefault)]
    [InlineData(DbFolderType.ContactsDefault)]
    [InlineData(DbFolderType.CalendarDefault)]
    [InlineData(DbFolderType.NotesDefault)]
    public void Email_in_a_non_mail_folder_is_rejected(DbFolderType folder)
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbEmail(), folder);
        Assert.True(Rejected(issues, "folder-class-congruence"));
    }

    [Theory]
    [InlineData(DbFolderType.InboxDefault)]
    [InlineData(DbFolderType.Mail)]
    [InlineData(DbFolderType.SentItemsDefault)]
    [InlineData(DbFolderType.DeletedItemsDefault)]
    public void Email_in_a_mail_folder_is_accepted(DbFolderType folder)
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbEmail(), folder);
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void Contact_in_the_me_contact_folder_is_accepted()
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbContactItem(), DbFolderType.MeContact);
        Assert.False(AnyRejected(issues));
    }

    [Theory]
    [InlineData(DbFolderType.Generic)]
    [InlineData(DbFolderType.Unknown)]
    [InlineData(DbFolderType.RecipientInformationCache)]
    public void Permissive_folder_types_skip_the_congruence_check(DbFolderType folder)
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbEmail(), folder);
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void An_unknown_folder_skips_the_congruence_check()
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbEmail(), folderType: null);
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void Complete_task_without_a_date_gets_one_stamped()
    {
        var task = new DbTask { Complete = true, DateCompleted = null };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.NotNull(task.DateCompleted);
        Assert.True(Corrected(issues, "task-complete-datecompleted"));
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void Reopened_task_has_its_completion_date_cleared()
    {
        var task = new DbTask { Complete = false, DateCompleted = new DateTime(2026, 1, 1) };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.Null(task.DateCompleted);
        Assert.True(Corrected(issues, "task-complete-datecompleted"));
    }

    [Fact]
    public void Complete_task_that_already_has_a_date_is_left_alone()
    {
        var stamped = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var task = new DbTask { Complete = true, DateCompleted = stamped };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.Equal(stamped, task.DateCompleted);
        Assert.Empty(issues);
    }

    [Fact]
    public void Task_starting_after_it_is_due_is_rejected()
    {
        var task = new DbTask
        {
            StartDate = new DateTime(2026, 5, 2),
            DueDate = new DateTime(2026, 5, 1),
        };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.True(Rejected(issues, "task-date-order"));
    }

    [Fact]
    public void Task_recurrence_without_a_start_is_rejected()
    {
        var task = new DbTask { RecurrenceType = 1, RecurrenceStart = null };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.True(Rejected(issues, "task-recurrence-required"));
    }

    [Fact]
    public void Task_with_a_complete_recurrence_is_accepted()
    {
        var task = new DbTask { RecurrenceType = 1, RecurrenceStart = new DateTime(2026, 5, 1) };

        var issues = ItemValidationRules.ValidateAndCorrect(task, DbFolderType.TasksDefault);

        Assert.False(AnyRejected(issues));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("IPM.Note")]
    public void Note_with_a_non_conforming_message_class_is_corrected(string? messageClass)
    {
        var note = new DbNote { MessageClass = messageClass };

        var issues = ItemValidationRules.ValidateAndCorrect(note, DbFolderType.NotesDefault);

        Assert.Equal("IPM.StickyNote", note.MessageClass);
        Assert.True(Corrected(issues, "note-message-class"));
    }

    [Fact]
    public void Note_message_class_subclass_is_preserved()
    {
        var note = new DbNote { MessageClass = "IPM.StickyNote.Custom", Subject = "s", LastModifiedDate = DateTime.UtcNow };

        var issues = ItemValidationRules.ValidateAndCorrect(note, DbFolderType.NotesDefault);

        Assert.Equal("IPM.StickyNote.Custom", note.MessageClass);
        Assert.False(Corrected(issues, "note-message-class"));
    }

    [Fact]
    public void Note_missing_server_owned_fields_gets_defaults()
    {
        var note = new DbNote { MessageClass = "IPM.StickyNote" };

        var issues = ItemValidationRules.ValidateAndCorrect(note, DbFolderType.NotesDefault);

        Assert.NotNull(note.LastModifiedDate);
        Assert.Equal(string.Empty, note.Subject);
        Assert.True(Corrected(issues, "note-required-defaults"));
    }

    [Fact]
    public void Note_with_an_out_of_range_body_type_is_rejected()
    {
        var note = new DbNote { MessageClass = "IPM.StickyNote", NativeBodyType = 9 };

        var issues = ItemValidationRules.ValidateAndCorrect(note, DbFolderType.NotesDefault);

        Assert.True(Rejected(issues, "note-body-type"));
    }

    [Fact]
    public void Completed_flag_gets_both_completion_stamps()
    {
        var email = new DbEmail { FlagStatus = 2 };

        var issues = ItemValidationRules.ValidateAndCorrect(email, DbFolderType.InboxDefault);

        Assert.NotNull(email.FlagDateCompleted);
        Assert.NotNull(email.FlagCompleteTime);
        Assert.True(Corrected(issues, "email-flag-complete"));
    }

    [Fact]
    public void Flag_starting_after_it_is_due_is_rejected()
    {
        var email = new DbEmail
        {
            FlagStartDate = new DateTime(2026, 5, 2),
            FlagDueDate = new DateTime(2026, 5, 1),
        };

        var issues = ItemValidationRules.ValidateAndCorrect(email, DbFolderType.InboxDefault);

        Assert.True(Rejected(issues, "email-flag-date-order"));
    }

    [Fact]
    public void An_unflagged_email_is_untouched()
    {
        var issues = ItemValidationRules.ValidateAndCorrect(new DbEmail(), DbFolderType.InboxDefault);
        Assert.Empty(issues);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(6)]
    public void Invalid_meeting_status_is_rejected(byte status)
    {
        var cal = new DbCalendarItem { MeetingStatus = status };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-meeting-status"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(15)]
    public void Valid_meeting_status_is_accepted(byte status)
    {
        var cal = new DbCalendarItem { MeetingStatus = status };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.False(Rejected(issues, "calendar-meeting-status"));
    }

    [Fact]
    public void Out_of_range_busy_status_is_rejected()
    {
        var cal = new DbCalendarItem { BusyStatus = 7 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-busy-status"));
    }

    [Fact]
    public void Out_of_range_sensitivity_is_rejected()
    {
        var cal = new DbCalendarItem { Sensitivity = 4 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-sensitivity"));
    }

    [Fact]
    public void Weekly_recurrence_without_a_day_of_week_is_rejected()
    {
        var cal = new DbCalendarItem { RecurrenceType = 1 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-recurrence-required"));
    }

    // Type is what the serializer gates the whole Recurrence block on, so a recurrence carrying
    // any other field but no Type would silently serialize as a one-off event.
    [Theory]
    [InlineData(nameof(DbCalendarItem.RecurrenceInterval))]
    [InlineData(nameof(DbCalendarItem.RecurrenceDayOfWeek))]
    [InlineData(nameof(DbCalendarItem.RecurrenceOccurrences))]
    [InlineData(nameof(DbCalendarItem.RecurrenceDayOfMonth))]
    public void Calendar_recurrence_without_a_type_is_rejected(string field)
    {
        var cal = new DbCalendarItem();
        switch (field)
        {
            case nameof(DbCalendarItem.RecurrenceInterval): cal.RecurrenceInterval = 2; break;
            case nameof(DbCalendarItem.RecurrenceDayOfWeek): cal.RecurrenceDayOfWeek = 2; break;
            case nameof(DbCalendarItem.RecurrenceOccurrences): cal.RecurrenceOccurrences = 5; break;
            case nameof(DbCalendarItem.RecurrenceDayOfMonth): cal.RecurrenceDayOfMonth = 3; break;
        }

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-recurrence-type-required"));
    }

    [Fact]
    public void Calendar_without_any_recurrence_is_not_flagged()
    {
        var cal = new DbCalendarItem();

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.False(Rejected(issues, "calendar-recurrence-type-required"));
    }

    [Fact]
    public void Weekly_recurrence_defaults_first_day_of_week()
    {
        var cal = new DbCalendarItem { RecurrenceType = 1, RecurrenceDayOfWeek = 2 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.Equal((byte)0, cal.RecurrenceFirstDayOfWeek);
        Assert.True(Corrected(issues, "calendar-recurrence-firstdow"));
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void Day_of_month_outside_a_monthly_or_yearly_recurrence_is_rejected()
    {
        var cal = new DbCalendarItem { RecurrenceType = 1, RecurrenceDayOfWeek = 2, RecurrenceDayOfMonth = 4 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-recurrence-forbidden"));
    }

    [Fact]
    public void Yearly_recurrence_requires_day_and_month()
    {
        var cal = new DbCalendarItem { RecurrenceType = 5, RecurrenceDayOfMonth = 4 };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-recurrence-required"));
    }

    [Fact]
    public void Recurrence_bounded_by_both_occurrences_and_until_is_rejected()
    {
        var cal = new DbCalendarItem
        {
            RecurrenceOccurrences = 5,
            RecurrenceUntil = new DateTime(2026, 6, 1),
        };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-recurrence-bound"));
    }

    [Fact]
    public void All_day_event_drops_its_timezone_and_time_of_day()
    {
        var cal = new DbCalendarItem
        {
            AllDayEvent = true,
            Timezone = "some-tz-blob",
            StartTime = new DateTime(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 5, 1, 17, 45, 0, DateTimeKind.Utc),
        };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.Null(cal.Timezone);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), cal.StartTime);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), cal.EndTime);
        Assert.True(Corrected(issues, "calendar-all-day"));
        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void A_timed_event_keeps_its_timezone_and_time_of_day()
    {
        var start = new DateTime(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc);
        var cal = new DbCalendarItem { AllDayEvent = false, Timezone = "tz", StartTime = start };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.Equal("tz", cal.Timezone);
        Assert.Equal(start, cal.StartTime);
        Assert.False(Corrected(issues, "calendar-all-day"));
    }

    [Fact]
    public void Calendar_exception_without_a_start_time_is_rejected()
    {
        var cal = new DbCalendarItem
        {
            Exceptions = [new DbCalendarException { ExceptionStartTime = null }],
        };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(Rejected(issues, "calendar-exception-start"));
    }

    [Fact]
    public void Calendar_exception_all_day_is_forced_to_match_the_master()
    {
        var cal = new DbCalendarItem
        {
            AllDayEvent = true,
            Exceptions = [new DbCalendarException { ExceptionStartTime = "20260501T000000Z", AllDayEvent = false }],
        };

        var issues = ItemValidationRules.ValidateAndCorrect(cal, DbFolderType.CalendarDefault);

        Assert.True(cal.Exceptions[0].AllDayEvent);
        Assert.True(Corrected(issues, "calendar-exception-all-day"));
    }

    [Fact]
    public void Oversized_contact_picture_is_rejected()
    {
        var contact = new DbContactItem { Picture = new byte[40 * 1024] };

        var issues = ItemValidationRules.ValidateAndCorrect(contact, DbFolderType.ContactsDefault);

        Assert.True(Rejected(issues, "contact-picture-size"));
    }

    [Fact]
    public void Contact_picture_within_the_limit_is_accepted()
    {
        var contact = new DbContactItem { Picture = new byte[30 * 1024] };

        var issues = ItemValidationRules.ValidateAndCorrect(contact, DbFolderType.ContactsDefault);

        Assert.False(AnyRejected(issues));
    }

    [Fact]
    public void A_plain_contact_is_untouched()
    {
        var issues = ItemValidationRules.ValidateAndCorrect(
            new DbContactItem { FirstName = "Ada" }, DbFolderType.ContactsDefault);

        Assert.Empty(issues);
    }
}
