using Google.Protobuf.WellKnownTypes;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

// Calendar datetimes must serialize compact (20260716T200000Z); the extended form made strict
// clients (iOS) reject the item and re-prime. Tasks/Contacts stay on the extended form.
public class CalendarSerializationTests
{
    private static Timestamp Ts(int y, int mo, int d, int h, int mi, int s) =>
        Timestamp.FromDateTime(new DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc));

    private static string Xml(ApplicationData appData) =>
        string.Concat(appData.Elements.Select(e => e.OuterXml));

    [Fact]
    public void Calendar_StartTime_EndTime_DtStamp_serialize_in_compact_form()
    {
        var cal = new CalendarItem
        {
            StartTime = Ts(2026, 7, 16, 20, 0, 0),
            EndTime = Ts(2026, 7, 16, 21, 0, 0),
            DtStamp = Ts(2026, 7, 16, 19, 23, 30),
            Subject = "Test",
        };

        var xml = Xml(cal.ToApplicationData());

        Assert.Contains("20260716T200000Z", xml);
        Assert.Contains("20260716T210000Z", xml);
        Assert.Contains("20260716T192330Z", xml);
        Assert.DoesNotContain("2026-07-16T", xml);
    }

    [Fact]
    public void Calendar_recurrence_Until_serializes_in_compact_form()
    {
        var cal = new CalendarItem
        {
            StartTime = Ts(2026, 7, 16, 20, 0, 0),
            RecurrenceType = 1, // weekly; presence emits the Recurrence block
            RecurrenceUntil = Ts(2026, 8, 16, 20, 0, 0),
        };

        var xml = Xml(cal.ToApplicationData());

        Assert.Contains("20260816T200000Z", xml);
        Assert.DoesNotContain("2026-08-16T", xml);
    }

    [Fact]
    public void Task_dates_stay_in_extended_form()
    {
        var task = new TaskItem
        {
            Subject = "Test",
            DueDate = Ts(2026, 7, 16, 20, 0, 0),
        };

        var xml = Xml(task.ToApplicationData());

        Assert.Contains("2026-07-16T20:00:00.000Z", xml);
        Assert.DoesNotContain("20260716T200000Z", xml);
    }
}
