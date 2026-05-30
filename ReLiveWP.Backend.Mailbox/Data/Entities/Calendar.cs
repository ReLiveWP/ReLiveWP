namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public class DbCalendarItem : DbItem
{
    // Timing
    public string? Timezone { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? DtStamp { get; set; }

    // Identity
    public string? Uid { get; set; }
    public string? ClientUid { get; set; }

    // Meeting
    public string? Subject { get; set; }
    public string? Location { get; set; }
    public uint? Reminder { get; set; }
    public bool? AllDayEvent { get; set; }
    public byte? BusyStatus { get; set; }
    public byte? Sensitivity { get; set; }
    public byte? MeetingStatus { get; set; }
    public string? OrganizerName { get; set; }
    public string? OrganizerEmail { get; set; }

    // Body
    public string? Notes { get; set; }
    public byte? NativeBodyType { get; set; }
    public string? BodyLegacy { get; set; }
    public bool? BodyTruncated { get; set; }

    // Response
    public DateTime? AppointmentReplyTime { get; set; }
    public uint? ResponseType { get; set; }
    public bool? ResponseRequested { get; set; }
    public bool? DisallowNewTimeProposal { get; set; }

    // Online meeting
    public string? OnlineMeetingConfLink { get; set; }
    public string? OnlineMeetingExternalLink { get; set; }

    // Recurrence
    public byte? RecurrenceType { get; set; }
    public ushort? RecurrenceOccurrences { get; set; }
    public ushort? RecurrenceInterval { get; set; }
    public byte? RecurrenceWeekOfMonth { get; set; }
    public ushort? RecurrenceDayOfWeek { get; set; }
    public byte? RecurrenceMonthOfYear { get; set; }
    public DateTime? RecurrenceUntil { get; set; }
    public byte? RecurrenceDayOfMonth { get; set; }
    public byte? RecurrenceCalendarType { get; set; }
    public bool? RecurrenceIsLeapMonth { get; set; }
    public byte? RecurrenceFirstDayOfWeek { get; set; }

    // Child collections
    public List<DbCalendarAttendee> Attendees { get; set; } = [];
    public List<DbCalendarCategory> Categories { get; set; } = [];
    public List<DbCalendarException> Exceptions { get; set; } = [];
}

public class DbCalendarAttendee
{
    public string Id { get; set; } = null!;
    public string CalendarItemId { get; set; } = null!;
    public DbCalendarItem CalendarItem { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte? AttendeeStatus { get; set; }
    public byte? AttendeeType { get; set; }
}

public class DbCalendarCategory
{
    public string Id { get; set; } = null!;
    public string CalendarItemId { get; set; } = null!;
    public DbCalendarItem CalendarItem { get; set; } = null!;
    public string Category { get; set; } = null!;
}

public class DbCalendarException
{
    public string Id { get; set; } = null!;
    public string CalendarItemId { get; set; } = null!;
    public DbCalendarItem CalendarItem { get; set; } = null!;

    public bool? Deleted { get; set; }
    public string? ExceptionStartTime { get; set; }
    public string? InstanceId { get; set; }

    public string? Subject { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Location { get; set; }
    public byte? Sensitivity { get; set; }
    public byte? BusyStatus { get; set; }
    public bool? AllDayEvent { get; set; }
    public uint? Reminder { get; set; }
    public DateTime? DtStamp { get; set; }
    public byte? MeetingStatus { get; set; }
    public DateTime? AppointmentReplyTime { get; set; }
    public uint? ResponseType { get; set; }
    public string? OnlineMeetingConfLink { get; set; }
    public string? OnlineMeetingExternalLink { get; set; }
    public string? Notes { get; set; }
    public string? BodyLegacy { get; set; }

    public List<DbCalendarExceptionAttendee> Attendees { get; set; } = [];
    public List<DbCalendarExceptionCategory> Categories { get; set; } = [];
}

public class DbCalendarExceptionAttendee
{
    public string Id { get; set; } = null!;
    public string CalendarExceptionId { get; set; } = null!;
    public DbCalendarException CalendarException { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte? AttendeeStatus { get; set; }
    public byte? AttendeeType { get; set; }
}

public class DbCalendarExceptionCategory
{
    public string Id { get; set; } = null!;
    public string CalendarExceptionId { get; set; } = null!;
    public DbCalendarException CalendarException { get; set; } = null!;
    public string Category { get; set; } = null!;
}
