using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

public sealed record RemoteCalendarEvent(
    string ExternalId,
    CalendarItem Calendar,
    string? Etag = null) : IRemoteItem
{
    public void ApplyTo(CreateItemRequest request) => request.Calendar = Calendar;

    public void ApplyTo(UpdateItemRequest request) => request.Calendar = Calendar;
}
