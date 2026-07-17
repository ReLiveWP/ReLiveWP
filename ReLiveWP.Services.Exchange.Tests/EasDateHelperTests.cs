using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Tests;

public class EasDateHelperTests
{
    private static readonly DateTime Sample = new(2026, 7, 16, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromDateTimeCompact_writes_the_compact_form()
    {
        Assert.Equal("20260716T200000Z", EasDateHelper.FromDateTimeCompact(Sample));
    }

    [Fact]
    public void FromDateTime_writes_the_extended_form()
    {
        Assert.Equal("2026-07-16T20:00:00.000Z", EasDateHelper.FromDateTime(Sample));
    }

    [Fact]
    public void Writers_return_null_for_null_input()
    {
        Assert.Null(EasDateHelper.FromDateTime(null));
        Assert.Null(EasDateHelper.FromDateTimeCompact(null));
    }

    [Theory]
    [InlineData("20260716T200000Z")]         // compact, what WP7 sends
    [InlineData("2026-07-16T20:00:00.000Z")] // extended
    [InlineData("2026-07-16T20:00:00Z")]     // extended, no fractional seconds
    public void ToDateTime_reads_both_wire_forms(string wire)
    {
        var dt = EasDateHelper.ToDateTime(wire);

        Assert.Equal(Sample, dt);
        Assert.Equal(DateTimeKind.Utc, dt!.Value.Kind);
    }

    [Fact]
    public void ToDateTime_returns_null_for_null_or_garbage()
    {
        Assert.Null(EasDateHelper.ToDateTime(null));
        Assert.Null(EasDateHelper.ToDateTime("not-a-date"));
    }

    [Fact]
    public void Compact_write_then_read_round_trips()
    {
        var wire = EasDateHelper.FromDateTimeCompact(Sample);
        Assert.Equal(Sample, EasDateHelper.ToDateTime(wire));
    }
}
