namespace ReLiveWP.Backend.ConnectedServices.Tests;

// The proxy composes its target as https://{path}{query}. Google's holiday calendar ids carry a '#'
// (en.uk#holiday@group.v.calendar.google.com), and routing hands the catch-all back decoded, so an
// unescaped one is read as a fragment and everything after it is dropped before the request goes out.
public class ProxyPathTests
{
    private const string CalendarId = "en.uk#holiday@group.v.calendar.google.com";

    private static Uri Compose(string path) => new($"https://{path}");

    private static string Path(string calendarId) =>
        $"www.googleapis.com/calendar/v3/calendars/{calendarId}/events";

    [Fact]
    public void A_bare_hash_truncates_the_path_into_a_fragment()
    {
        var uri = Compose(Path(CalendarId));

        Assert.Equal("/calendar/v3/calendars/en.uk", uri.AbsolutePath);
        Assert.NotEqual(string.Empty, uri.Fragment);
    }

    [Fact]
    public void An_escaped_hash_stays_in_the_path()
    {
        var uri = Compose(Path(CalendarId.Replace("#", "%23")));

        Assert.Equal(
            "/calendar/v3/calendars/en.uk%23holiday@group.v.calendar.google.com/events",
            uri.AbsolutePath);
        Assert.Equal(string.Empty, uri.Fragment);
    }

    // the normalisation the handler applies has to leave an already-escaped path alone, because it
    // cannot tell whether routing decoded on the way in
    [Fact]
    public void Escaping_an_already_escaped_hash_changes_nothing()
    {
        var once = Path(CalendarId).Replace("#", "%23");

        Assert.Equal(once, once.Replace("#", "%23"));
    }
}
