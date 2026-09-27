using System.Text.Json.Serialization;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public class GraphCalendarsResponse
{
    public List<GraphCalendar>? Value { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

public class GraphCalendar
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public bool IsDefaultCalendar { get; set; }
    public bool CanEdit { get; set; }
}

public class GraphEventsResponse
{
    public List<GraphEvent>? Value { get; set; }

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

public class GraphEvent
{
    public string? Id { get; set; }
    public string? ICalUId { get; set; }
    public string? Subject { get; set; }
    public string? BodyPreview { get; set; }
    public string? Importance { get; set; }
    public string? Sensitivity { get; set; }
    public string? ShowAs { get; set; }
    public string? Type { get; set; }
    public string? SeriesMasterId { get; set; }
    public string? OriginalStart { get; set; }
    public string? OriginalStartTimeZone { get; set; }
    public string? LastModifiedDateTime { get; set; }
    public string? OnlineMeetingUrl { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsReminderOn { get; set; }
    public int ReminderMinutesBeforeStart { get; set; }
    public bool ResponseRequested { get; set; }

    [JsonPropertyName("@odata.etag")]
    public string? Etag { get; set; }

    public GraphDateTimeTimeZone? Start { get; set; }
    public GraphDateTimeTimeZone? End { get; set; }
    public GraphItemBody? Body { get; set; }
    public GraphLocation? Location { get; set; }
    public GraphRecipient? Organizer { get; set; }
    public List<GraphAttendee>? Attendees { get; set; }
    public List<string>? Categories { get; set; }
    public GraphPatternedRecurrence? Recurrence { get; set; }

    // both need $select or $expand, and only come back from a GET on a single series master
    public List<string>? CancelledOccurrences { get; set; }
    public List<GraphEvent>? ExceptionOccurrences { get; set; }
}

public class GraphDateTimeTimeZone
{
    public string? DateTime { get; set; }
    public string? TimeZone { get; set; }
}

public class GraphItemBody
{
    public string? ContentType { get; set; }
    public string? Content { get; set; }
}

public class GraphLocation
{
    public string? DisplayName { get; set; }
}

public class GraphRecipient
{
    public GraphEmailAddress? EmailAddress { get; set; }
}

public class GraphEmailAddress
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class GraphAttendee
{
    public string? Type { get; set; }
    public GraphEmailAddress? EmailAddress { get; set; }
    public GraphResponseStatus? Status { get; set; }
}

public class GraphResponseStatus
{
    public string? Response { get; set; }
    public string? Time { get; set; }
}

public class GraphPatternedRecurrence
{
    public GraphRecurrencePattern? Pattern { get; set; }
    public GraphRecurrenceRange? Range { get; set; }
}

public class GraphRecurrencePattern
{
    public string? Type { get; set; }
    public int Interval { get; set; }
    public int Month { get; set; }
    public int DayOfMonth { get; set; }
    public List<string>? DaysOfWeek { get; set; }
    public string? FirstDayOfWeek { get; set; }
    public string? Index { get; set; }
}

public class GraphRecurrenceRange
{
    public string? Type { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int NumberOfOccurrences { get; set; }
    public string? RecurrenceTimeZone { get; set; }
}
