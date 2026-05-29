namespace ReLiveWP.Services.Exchange.Data.Entities;

// ── Calendar (code page 2) ────────────────────────────────────────────────────
// Scalar fields map to nullable TPH columns. Multi-valued elements (Attendees,
// Categories, Exceptions) use separate child tables. Recurrence is embedded as
// prefixed columns since there is at most one Recurrence per item.

public class CalendarItem : Item
{
    // ── Timing ────────────────────────────────────────────────────────────────
    public string? Timezone { get; set; }       // base64 TimeZone blob; null if AllDayEvent (v16.0+)
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? DtStamp { get; set; }

    // ── Identity ──────────────────────────────────────────────────────────────
    public string? Uid { get; set; }            // max 300 chars; server-assigned in v16.0+
    public string? ClientUid { get; set; }      // client-supplied random ID; v16.0+ only

    // ── Meeting info ──────────────────────────────────────────────────────────
    public string? Subject { get; set; }
    public string? Location { get; set; }       // calendar:Location; v2.5–14.1 (plain string)
    public uint? Reminder { get; set; }         // xs:unsignedInt; minutes before start
    public bool? AllDayEvent { get; set; }
    public byte? BusyStatus { get; set; }       // 0=Free 1=Tentative 2=Busy 3=OOF 4=WorkingElsewhere
    public byte? Sensitivity { get; set; }      // 0=Normal 1=Personal 2=Private 3=Confidential
    public byte? MeetingStatus { get; set; }    // bitmask: bit0=Meeting bit1=Received bit2=Cancelled
    public string? OrganizerName { get; set; }
    public string? OrganizerEmail { get; set; }

    // ── Notes / body ─────────────────────────────────────────────────────────
    public string? Notes { get; set; }          // body text (from airsyncbase:Body.Data)
    public byte? NativeBodyType { get; set; }   // xs:unsignedByte; 1=PlainText 2=HTML 3=RTF

    // ── Notes (v2.5 only, calendar namespace) ─────────────────────────────────
    public string? BodyLegacy { get; set; }
    public bool? BodyTruncated { get; set; }

    // ── Meeting response ──────────────────────────────────────────────────────
    public DateTime? AppointmentReplyTime { get; set; }
    public uint? ResponseType { get; set; }     // xs:unsignedInt; 0-5
    public bool? ResponseRequested { get; set; }
    public bool? DisallowNewTimeProposal { get; set; }

    // ── Online meeting ────────────────────────────────────────────────────────
    public string? OnlineMeetingConfLink { get; set; }
    public string? OnlineMeetingExternalLink { get; set; }

    // ── Recurrence (embedded; at most one per item) ───────────────────────────
    public byte? RecurrenceType { get; set; }           // 0=Daily 1=Weekly 2=Monthly 3=MonthlyNth 5=Yearly 6=YearlyNth
    public ushort? RecurrenceOccurrences { get; set; }  // xs:unsignedShort; max 999
    public ushort? RecurrenceInterval { get; set; }     // xs:unsignedShort; max 999
    public byte? RecurrenceWeekOfMonth { get; set; }    // 1–5; Type=3 or 6
    public ushort? RecurrenceDayOfWeek { get; set; }    // bitmask: 1=Sun 2=Mon 4=Tue ... 64=Sat
    public byte? RecurrenceMonthOfYear { get; set; }    // 1–12; Type=5 or 6
    public DateTime? RecurrenceUntil { get; set; }      // last occurrence start
    public byte? RecurrenceDayOfMonth { get; set; }     // 1–31; Type=2 or 5
    public byte? RecurrenceCalendarType { get; set; }   // 0=Default 1=Gregorian …
    public bool? RecurrenceIsLeapMonth { get; set; }
    public byte? RecurrenceFirstDayOfWeek { get; set; } // 0=Sun … 6=Sat

    // ── Child collections ─────────────────────────────────────────────────────
    public List<CalendarAttendee> Attendees { get; set; } = [];
    public List<CalendarCategory> Categories { get; set; } = [];
    public List<CalendarException> Exceptions { get; set; } = [];
}

// ── Attendee ──────────────────────────────────────────────────────────────────
public class CalendarAttendee
{
    public string Id { get; set; }
    public string CalendarItemId { get; set; }
    public CalendarItem CalendarItem { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte? AttendeeStatus { get; set; }   // 0=Unknown 2=Tentative 3=Accept 4=Decline 5=NotResponded
    public byte? AttendeeType { get; set; }     // 1=Required 2=Optional 3=Resource
}

// ── Category ──────────────────────────────────────────────────────────────────
public class CalendarCategory
{
    public string Id { get; set; }
    public string CalendarItemId { get; set; }
    public CalendarItem CalendarItem { get; set; } = null!;

    public string Category { get; set; } = null!;
}

// ── Exception (a modified or deleted occurrence in a recurring series) ─────────
// ExceptionStartTime identifies the replaced occurrence for v2.5–14.1;
// InstanceId (airsyncbase) is used instead in v16.0+.
public class CalendarException
{
    public string Id { get; set; }
    public string CalendarItemId { get; set; }
    public CalendarItem CalendarItem { get; set; } = null!;

    public bool? Deleted { get; set; }
    public string? ExceptionStartTime { get; set; } // Compact DateTime string; v2.5–14.1
    public string? InstanceId { get; set; }          // airsyncbase:InstanceId; v16.0+

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
    public string? BodyLegacy { get; set; }         // calendar:Body; v2.5 only

    public List<CalendarExceptionAttendee> Attendees { get; set; } = [];
    public List<CalendarExceptionCategory> Categories { get; set; } = [];
}

// ── Exception attendee ────────────────────────────────────────────────────────
public class CalendarExceptionAttendee
{
    public string Id { get; set; }
    public string CalendarExceptionId { get; set; }
    public CalendarException CalendarException { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte? AttendeeStatus { get; set; }
    public byte? AttendeeType { get; set; }
}

// ── Exception category ────────────────────────────────────────────────────────
public class CalendarExceptionCategory
{
    public string Id { get; set; }
    public string CalendarExceptionId { get; set; }
    public CalendarException CalendarException { get; set; } = null!;

    public string Category { get; set; } = null!;
}
