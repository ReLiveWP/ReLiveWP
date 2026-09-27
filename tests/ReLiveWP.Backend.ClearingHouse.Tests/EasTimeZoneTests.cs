using System.Buffers.Binary;
using System.Text;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// MS-ASDTYPE 2.7.6 fixes the byte layout and MS-OXCICAL 2.1.3.1.1.19.2.2 fixes the SYSTEMTIME
// convention. The reader below walks the offsets independently of the writer, so a layout mistake
// has to be made twice to pass.
public class EasTimeZoneTests
{
    private static readonly DateTime Winter = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly record struct SystemTime(
        ushort Year, ushort Month, ushort DayOfWeek, ushort Day, ushort Hour, ushort Minute);

    private readonly record struct Blob(
        int Bias, string StandardName, SystemTime StandardDate, int StandardBias,
        string DaylightName, SystemTime DaylightDate, int DaylightBias);

    private static Blob Read(byte[] bytes) => new(
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4)),
        Name(bytes, 4),
        Time(bytes, 68),
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(84, 4)),
        Name(bytes, 88),
        Time(bytes, 152),
        BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(168, 4)));

    private static string Name(byte[] bytes, int offset) =>
        Encoding.Unicode.GetString(bytes, offset, 64).TrimEnd('\0');

    private static SystemTime Time(byte[] bytes, int offset)
    {
        ushort At(int i) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + i * 2, 2));
        return new(At(0), At(1), At(2), At(3), At(4), At(5));
    }

    private static Blob Build(string id, DateTime? at = null) =>
        Read(EasTimeZone.Build(TimeZoneInfo.FindSystemTimeZoneById(id), at ?? Winter));

    private static TimeZoneInfo ZoneWithOneRule(DateTime ruleStart, DateTime ruleEnd, TimeSpan daylightDelta)
    {
        var springForward = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday);
        var fallBack = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(ruleStart, ruleEnd, daylightDelta, springForward, fallBack);

        return TimeZoneInfo.CreateCustomTimeZone("Test/OneRule", TimeSpan.FromHours(3), "Test", "Test Standard", "Test Daylight", [rule]);
    }

    [Fact]
    public void The_blob_is_172_bytes()
    {
        Assert.Equal(172, EasTimeZone.Size);
        Assert.Equal(172, EasTimeZone.Build(TimeZoneInfo.Utc, Winter).Length);
    }

    [Fact]
    public void Dst_the_zone_has_since_dropped_does_not_come_back()
    {
        var zone = ZoneWithOneRule(new DateTime(2000, 1, 1), new DateTime(2015, 12, 31), TimeSpan.FromHours(1));

        var blob = Read(EasTimeZone.Build(zone, Winter));

        Assert.Equal(0, blob.DaylightDate.Month);
        Assert.Equal(0, blob.StandardDate.Month);
        Assert.Equal(0, blob.DaylightBias);
    }

    [Fact]
    public void Dst_still_applies_inside_the_years_its_rule_covers()
    {
        var zone = ZoneWithOneRule(new DateTime(2000, 1, 1), new DateTime(2015, 12, 31), TimeSpan.FromHours(1));

        var blob = Read(EasTimeZone.Build(zone, new DateTime(2010, 1, 15, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(3, blob.DaylightDate.Month);
        Assert.Equal(10, blob.StandardDate.Month);
        Assert.Equal(-60, blob.DaylightBias);
    }

    [Fact]
    public void A_rule_that_adds_no_daylight_time_is_not_dst()
    {
        var zone = ZoneWithOneRule(new DateTime(2000, 1, 1), DateTime.MaxValue.Date, TimeSpan.Zero);

        var blob = Read(EasTimeZone.Build(zone, Winter));

        Assert.Equal(0, blob.DaylightDate.Month);
        Assert.Equal(0, blob.StandardDate.Month);
    }

    // every numeric field is zero for UTC; the name fields still carry a name
    [Fact]
    public void Utc_has_no_offset_and_no_transitions()
    {
        var blob = Read(EasTimeZone.Build(TimeZoneInfo.Utc, Winter));

        Assert.Equal(0, blob.Bias);
        Assert.Equal(0, blob.StandardBias);
        Assert.Equal(0, blob.DaylightBias);
        Assert.Equal(0, blob.StandardDate.Month);
        Assert.Equal(0, blob.DaylightDate.Month);
    }

    // names are 32 WCHARs with the unused ones zeroed, so a long one still leaves a terminator
    [Fact]
    public void Names_round_trip_and_stay_terminated()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var bytes = EasTimeZone.Build(zone, Winter);
        var blob = Read(bytes);

        Assert.StartsWith(blob.StandardName, zone.StandardName);
        Assert.True(blob.StandardName.Length <= 31);
        Assert.Equal(0, bytes[4 + 62]);
        Assert.Equal(0, bytes[4 + 63]);
    }

    [Fact]
    public void Base64_is_the_wire_form()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

        Assert.Equal(
            Convert.ToBase64String(EasTimeZone.Build(zone, Winter)),
            EasTimeZone.ToBase64(zone, Winter));
    }
}
