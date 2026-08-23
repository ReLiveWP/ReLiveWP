namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class GoogleCalendarListResponse
{
    public List<GoogleCalendarListEntry>? Items { get; set; }
    public string? NextPageToken { get; set; }
}

public class GoogleCalendarListEntry
{
    public string? Id { get; set; }
    public string? Summary { get; set; }
    public string? SummaryOverride { get; set; }
    public string? TimeZone { get; set; }
    public string? AccessRole { get; set; }
    public bool Primary { get; set; }
    public bool Deleted { get; set; }
}

public class GoogleEventsResponse
{
    public List<GoogleEvent>? Items { get; set; }
    public string? NextPageToken { get; set; }
    public string? NextSyncToken { get; set; }
}

public class GoogleEvent
{
    public string? Id { get; set; }
    public string? ICalUID { get; set; }
    public string? Status { get; set; }
    public string? Summary { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Transparency { get; set; }
    public string? Visibility { get; set; }
    public string? HangoutLink { get; set; }
    public string? Updated { get; set; }
    public string? EventType { get; set; }

    public GoogleEventDate? Start { get; set; }
    public GoogleEventDate? End { get; set; }
    public bool EndTimeUnspecified { get; set; }

    public List<string>? Recurrence { get; set; }
    public string? RecurringEventId { get; set; }
    public GoogleEventDate? OriginalStartTime { get; set; }

    public GoogleEventPerson? Organizer { get; set; }
    public List<GoogleEventPerson>? Attendees { get; set; }
    public GoogleEventReminders? Reminders { get; set; }
}

public class GoogleEventDate
{
    public string? Date { get; set; }
    public string? DateTime { get; set; }
    public string? TimeZone { get; set; }
}

public class GoogleEventPerson
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool Self { get; set; }
    public bool Optional { get; set; }
    public bool Resource { get; set; }
    public string? ResponseStatus { get; set; }
}

public class GoogleEventReminders
{
    public bool UseDefault { get; set; }
    public List<GoogleEventReminder>? Overrides { get; set; }
}

public class GoogleEventReminder
{
    public string? Method { get; set; }
    public int Minutes { get; set; }
}
