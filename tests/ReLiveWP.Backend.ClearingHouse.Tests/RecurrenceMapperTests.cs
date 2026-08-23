using ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// EAS carries a much smaller recurrence model than RFC 5545, and MS-OXCICAL 2.1.3.2.2 is where the
// two are reconciled. Its six templates come with worked examples, so those are the cases here.
// Anything outside them has to say so rather than quietly losing the tail of a series.
public class RecurrenceMapperTests
{
    // 2026-06-14 is a Sunday, so start-derived defaults are visible rather than coincidental
    private static readonly DateTime Start = new(2026, 6, 14, 9, 0, 0, DateTimeKind.Utc);

    private static EasRecurrence Map(string rrule, DateTime? start = null)
    {
        var mapping = RecurrenceMapper.Map(RRuleParser.Parse(rrule), start ?? Start);

        Assert.True(mapping.Mapped, $"expected {rrule} to map, got: {mapping.Reason}");
        return mapping.Recurrence!;
    }

    private static string Reason(string rrule, DateTime? start = null)
    {
        var mapping = RecurrenceMapper.Map(RRuleParser.Parse(rrule), start ?? Start);

        Assert.False(mapping.Mapped, $"expected {rrule} to be unrepresentable");
        return mapping.Reason!;
    }

    // MS-OXCICAL 2.1.3.2.2.1
    [Theory]
    [InlineData("FREQ=DAILY", 1u)]
    [InlineData("FREQ=DAILY;BYMINUTE=30;BYHOUR=15", 1u)]
    [InlineData("FREQ=DAILY;INTERVAL=3", 3u)]
    [InlineData("FREQ=DAILY;INTERVAL=3;BYMINUTE=30;BYHOUR=15", 3u)]
    public void The_daily_template_maps_to_type_0(string rrule, uint interval)
    {
        var recurrence = Map(rrule);

        Assert.Equal(0u, recurrence.Type);
        Assert.Equal(interval, recurrence.Interval);
        Assert.Null(recurrence.DayOfWeek);
        Assert.Null(recurrence.DayOfMonth);
        Assert.Null(recurrence.MonthOfYear);
    }

    [Fact]
    public void A_daily_count_becomes_occurrences()
    {
        var recurrence = Map("FREQ=DAILY;INTERVAL=3;BYMINUTE=30;BYHOUR=15;COUNT=30");

        Assert.Equal(30u, recurrence.Occurrences);
        Assert.Null(recurrence.Until);
    }

    // MS-OXCICAL 2.1.3.2.2.2
    [Theory]
    [InlineData("FREQ=WEEKLY;BYDAY=MO,TU", 1u)]
    [InlineData("FREQ=WEEKLY;BYDAY=MO,TU;BYMINUTE=30;BYHOUR=15", 1u)]
    [InlineData("FREQ=WEEKLY;BYDAY=MO,TU;INTERVAL=2;COUNT=7", 2u)]
    public void The_weekly_template_maps_to_type_1(string rrule, uint interval)
    {
        var recurrence = Map(rrule);

        Assert.Equal(1u, recurrence.Type);
        Assert.Equal(interval, recurrence.Interval);
        Assert.Equal(EasDayOfWeek.Monday | EasDayOfWeek.Tuesday, recurrence.DayOfWeek);
    }

    // MS-ASCAL 2.2.2.24: SU=0 through SA=6
    [Fact]
    public void Wkst_sets_first_day_of_week()
    {
        var recurrence = Map("FREQ=WEEKLY;BYDAY=SU,MO;INTERVAL=2;WKST=MO");

        Assert.Equal(1u, recurrence.FirstDayOfWeek);
        Assert.Equal(EasDayOfWeek.Sunday | EasDayOfWeek.Monday, recurrence.DayOfWeek);
    }

    // RFC 5545 defaults an absent WKST to MO, MS-OXCICAL 2.1.3.2.1.9 defaults it to SU. the rest of
    // the stack follows MS, including the mailbox validator, so this must not drift to the RFC.
    [Fact]
    public void An_absent_wkst_follows_MS_and_defaults_to_sunday()
    {
        Assert.Equal(0u, Map("FREQ=WEEKLY;BYDAY=MO,TU").FirstDayOfWeek);
    }

    [Fact]
    public void A_weekly_rule_with_no_byday_takes_the_day_from_the_start()
    {
        Assert.Equal(EasDayOfWeek.Sunday, Map("FREQ=WEEKLY").DayOfWeek);
    }

    // MS-OXCICAL 2.1.3.2.2.3
    [Theory]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=10;BYMINUTE=30;BYHOUR=15", 10u, 1u)]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=15;INTERVAL=3;COUNT=7", 15u, 3u)]
    public void The_monthly_template_maps_to_type_2(string rrule, uint dayOfMonth, uint interval)
    {
        var recurrence = Map(rrule);

        Assert.Equal(2u, recurrence.Type);
        Assert.Equal(dayOfMonth, recurrence.DayOfMonth);
        Assert.Equal(interval, recurrence.Interval);
        Assert.Null(recurrence.MonthOfYear);
    }

    [Fact]
    public void A_monthly_rule_with_no_bymonthday_takes_the_day_from_the_start()
    {
        Assert.Equal(14u, Map("FREQ=MONTHLY").DayOfMonth);
    }

    // DayOfMonth is 1..31 (MS-ASCAL 2.2.2.14), so the last day of the month cannot be type 2. it
    // goes through the nth form with the all-days mask instead.
    [Fact]
    public void The_last_day_of_the_month_becomes_an_nth_rule()
    {
        var recurrence = Map("FREQ=MONTHLY;BYMONTHDAY=-1");

        Assert.Equal(3u, recurrence.Type);
        Assert.Equal(EasDayOfWeek.LastDayOfMonth, recurrence.DayOfWeek);
        Assert.Equal(5u, recurrence.WeekOfMonth);
        Assert.Null(recurrence.DayOfMonth);
    }

    // MS-OXCICAL 2.1.3.2.2.4
    [Fact]
    public void The_third_sunday_of_every_month_maps_to_type_3()
    {
        var recurrence = Map("FREQ=MONTHLY;BYDAY=SU;BYSETPOS=3");

        Assert.Equal(3u, recurrence.Type);
        Assert.Equal(EasDayOfWeek.Sunday, recurrence.DayOfWeek);
        Assert.Equal(3u, recurrence.WeekOfMonth);
    }

    // the spec's own "last weekday of every month". BYDAY=MO,TU,WE,TH,FR is not an arbitrary set,
    // it is the 62 mask from MS-ASCAL 2.2.2.15, which is what makes this representable at all.
    [Fact]
    public void The_last_weekday_of_every_month_maps_to_the_weekdays_mask()
    {
        var recurrence = Map("FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1;BYMINUTE=30;BYHOUR=15");

        Assert.Equal(3u, recurrence.Type);
        Assert.Equal(62u, recurrence.DayOfWeek);
        Assert.Equal(5u, recurrence.WeekOfMonth);
    }

    [Fact]
    public void A_weekend_day_set_maps_to_the_65_mask()
    {
        Assert.Equal(65u, Map("FREQ=MONTHLY;BYDAY=SA,SU;BYSETPOS=1").DayOfWeek);
    }

    [Fact]
    public void The_first_monday_of_every_month_for_seven_occurrences()
    {
        var recurrence = Map("FREQ=MONTHLY;BYDAY=MO;BYSETPOS=1;COUNT=7");

        Assert.Equal(3u, recurrence.Type);
        Assert.Equal(1u, recurrence.WeekOfMonth);
        Assert.Equal(7u, recurrence.Occurrences);
    }

    // google and caldav write the nth form inline; MS writes it with BYSETPOS. both have to land in
    // the same place or every real provider's monthly rules fall through to expansion.
    [Theory]
    [InlineData("FREQ=MONTHLY;BYDAY=3SU")]
    [InlineData("FREQ=MONTHLY;BYDAY=SU;BYSETPOS=3")]
    public void Both_spellings_of_the_nth_form_agree(string rrule)
    {
        var recurrence = Map(rrule);

        Assert.Equal(3u, recurrence.Type);
        Assert.Equal(EasDayOfWeek.Sunday, recurrence.DayOfWeek);
        Assert.Equal(3u, recurrence.WeekOfMonth);
    }

    [Fact]
    public void A_trailing_inline_ordinal_maps_to_the_last_week()
    {
        Assert.Equal(5u, Map("FREQ=MONTHLY;BYDAY=-1FR").WeekOfMonth);
    }

    // MS-OXCICAL 2.1.3.2.2.5
    [Theory]
    [InlineData("FREQ=YEARLY;BYMONTHDAY=10;BYMONTH=1;BYMINUTE=30;BYHOUR=15", 10u, 1u, 1u)]
    [InlineData("FREQ=YEARLY;BYMONTHDAY=15;BYMONTH=3;INTERVAL=3;COUNT=7", 15u, 3u, 3u)]
    public void The_yearly_template_maps_to_type_5(string rrule, uint day, uint month, uint interval)
    {
        var recurrence = Map(rrule);

        Assert.Equal(5u, recurrence.Type);
        Assert.Equal(day, recurrence.DayOfMonth);
        Assert.Equal(month, recurrence.MonthOfYear);
        Assert.Equal(interval, recurrence.Interval);
    }

    [Fact]
    public void The_last_day_of_every_september_becomes_an_nth_rule()
    {
        var recurrence = Map("FREQ=YEARLY;BYMONTHDAY=-1;BYMONTH=9");

        Assert.Equal(6u, recurrence.Type);
        Assert.Equal(EasDayOfWeek.LastDayOfMonth, recurrence.DayOfWeek);
        Assert.Equal(5u, recurrence.WeekOfMonth);
        Assert.Equal(9u, recurrence.MonthOfYear);
    }

    // MS-OXCICAL 2.1.3.2.2.6
    [Fact]
    public void The_third_sunday_of_every_june_maps_to_type_6()
    {
        var recurrence = Map("FREQ=YEARLY;BYDAY=SU;BYSETPOS=3;BYMONTH=6");

        Assert.Equal(6u, recurrence.Type);
        Assert.Equal(EasDayOfWeek.Sunday, recurrence.DayOfWeek);
        Assert.Equal(3u, recurrence.WeekOfMonth);
        Assert.Equal(6u, recurrence.MonthOfYear);
    }

    [Fact]
    public void The_last_weekday_of_every_april()
    {
        var recurrence = Map("FREQ=YEARLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1;BYMONTH=4;BYMINUTE=30;BYHOUR=15");

        Assert.Equal(6u, recurrence.Type);
        Assert.Equal(62u, recurrence.DayOfWeek);
        Assert.Equal(5u, recurrence.WeekOfMonth);
        Assert.Equal(4u, recurrence.MonthOfYear);
    }

    [Fact]
    public void The_first_monday_of_every_october_every_three_years()
    {
        var recurrence = Map("FREQ=YEARLY;BYDAY=MO;BYSETPOS=1;BYMONTH=10;INTERVAL=3;COUNT=7");

        Assert.Equal(6u, recurrence.Type);
        Assert.Equal(3u, recurrence.Interval);
        Assert.Equal(7u, recurrence.Occurrences);
    }

    [Fact]
    public void Until_carries_across_and_stays_utc()
    {
        var recurrence = Map("FREQ=DAILY;UNTIL=20261231T235900Z");

        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 0, DateTimeKind.Utc), recurrence.Until);
        Assert.Null(recurrence.Occurrences);
    }

    // every occurrence of every day is a weekly mask, which is how EAS says "every weekday"
    [Fact]
    public void A_daily_rule_with_byday_becomes_a_weekly_mask()
    {
        var recurrence = Map("FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR");

        Assert.Equal(1u, recurrence.Type);
        Assert.Equal(62u, recurrence.DayOfWeek);
    }

    [Theory]
    // an arbitrary nth-weekday set is not one of the four MS-OXCICAL allows
    [InlineData("FREQ=MONTHLY;BYDAY=MO,WE;BYSETPOS=1")]
    // BYSETPOS outside -1 and 1..4 has no WeekOfMonth
    [InlineData("FREQ=MONTHLY;BYDAY=MO;BYSETPOS=2,3")]
    [InlineData("FREQ=MONTHLY;BYDAY=MO;BYSETPOS=-2")]
    // an nth-weekday rule with no position at all is "every monday of every month"
    [InlineData("FREQ=MONTHLY;BYDAY=MO")]
    // lists EAS has no room for
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=1,15")]
    [InlineData("FREQ=YEARLY;BYMONTH=1,6;BYMONTHDAY=1")]
    // parts with no EAS equivalent at all
    [InlineData("FREQ=YEARLY;BYWEEKNO=20")]
    [InlineData("FREQ=YEARLY;BYYEARDAY=100")]
    [InlineData("FREQ=HOURLY;INTERVAL=6")]
    [InlineData("FREQ=MINUTELY")]
    // out of range for the EAS element
    [InlineData("FREQ=DAILY;INTERVAL=1000")]
    [InlineData("FREQ=DAILY;COUNT=1000")]
    // every n days filtered to weekdays is not a weekly mask
    [InlineData("FREQ=DAILY;BYDAY=MO,TU;INTERVAL=3")]
    public void An_unrepresentable_rule_says_why(string rrule)
    {
        Assert.False(string.IsNullOrWhiteSpace(Reason(rrule)));
    }

    // a reason is what gets logged and what sends the series down the expansion path, so it has to
    // be present on every rejection rather than an empty string
    [Fact]
    public void A_mapped_rule_carries_no_reason()
    {
        var mapping = RecurrenceMapper.Map(RRuleParser.Parse("FREQ=DAILY"), Start);

        Assert.True(mapping.Mapped);
        Assert.Null(mapping.Reason);
    }
}
