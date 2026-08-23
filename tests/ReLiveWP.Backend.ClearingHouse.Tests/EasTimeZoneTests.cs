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

    [Fact]
    public void The_blob_is_172_bytes()
    {
        Assert.Equal(172, EasTimeZone.Size);
        Assert.Equal(172, EasTimeZone.Build(TimeZoneInfo.Utc, Winter).Length);
    }

    // the spec's own example: the bias for Pacific Time (UTC-8) is 480, not -480
    [Fact]
    public void Bias_is_the_negated_utc_offset()
    {
        Assert.Equal(480, Build("America/Los_Angeles").Bias);
        Assert.Equal(0, Build("Europe/London").Bias);
        Assert.Equal(-60, Build("Europe/Berlin").Bias);
        Assert.Equal(-600, Build("Australia/Brisbane").Bias);
    }

    [Fact]
    public void Standard_bias_is_zero_because_the_base_offset_already_carries_it()
    {
        Assert.Equal(0, Build("Europe/London").StandardBias);
    }

    // an hour of DST is -60 on top of Bias, matching the sign convention above
    [Fact]
    public void Daylight_bias_is_the_negated_delta()
    {
        Assert.Equal(-60, Build("Europe/London").DaylightBias);
        Assert.Equal(-60, Build("America/New_York").DaylightBias);
    }

    // Europe/London goes to BST on the last Sunday of March and back on the last Sunday of October
    [Fact]
    public void London_transitions_are_floating_last_sunday_rules()
    {
        var blob = Build("Europe/London");

        Assert.Equal(0, blob.DaylightDate.Year);
        Assert.Equal(3, blob.DaylightDate.Month);
        Assert.Equal((ushort)DayOfWeek.Sunday, blob.DaylightDate.DayOfWeek);
        Assert.Equal(5, blob.DaylightDate.Day);

        Assert.Equal(0, blob.StandardDate.Year);
        Assert.Equal(10, blob.StandardDate.Month);
        Assert.Equal((ushort)DayOfWeek.Sunday, blob.StandardDate.DayOfWeek);
        Assert.Equal(5, blob.StandardDate.Day);
    }

    // New York is the second Sunday of March and the first Sunday of November, so the occurrence
    // number lands somewhere other than "last" and a hardcoded 5 would not survive
    [Fact]
    public void New_york_transitions_carry_their_occurrence_numbers()
    {
        var blob = Build("America/New_York");

        Assert.Equal(3, blob.DaylightDate.Month);
        Assert.Equal(2, blob.DaylightDate.Day);
        Assert.Equal((ushort)DayOfWeek.Sunday, blob.DaylightDate.DayOfWeek);

        Assert.Equal(11, blob.StandardDate.Month);
        Assert.Equal(1, blob.StandardDate.Day);
        Assert.Equal((ushort)DayOfWeek.Sunday, blob.StandardDate.DayOfWeek);
    }

    // wMonth 0 is how "this zone never transitions" reads
    [Fact]
    public void A_zone_with_no_dst_leaves_both_transitions_zeroed()
    {
        var blob = Build("Australia/Brisbane");

        Assert.Equal(0, blob.DaylightDate.Month);
        Assert.Equal(0, blob.StandardDate.Month);
        Assert.Equal(0, blob.DaylightBias);
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
