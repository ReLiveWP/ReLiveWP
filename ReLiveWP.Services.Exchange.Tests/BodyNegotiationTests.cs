using System.Text;
using System.Xml;
using ReLiveWP.Services.Exchange.Extensions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Tests;

public class BodyNegotiationTests
{
    private sealed record BodyView(BodyType Type, int? EstimatedDataSize, int? Truncated,
                                   string? Data, string? Preview);

    private static BodyView? Body(EmailItem e, params BodyPreference[] prefs)
    {
        var appData = e.ToApplicationData(prefs.Length == 0 ? null : prefs);
        var el = appData.Elements.FirstOrDefault(x => x.LocalName == "Body");
        if (el is null) return null;

        string? Child(string name) => el.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(c => c.LocalName == name)?.InnerText;

        int? Int(string name) => Child(name) is { } v && int.TryParse(v, out var n) ? n : null;

        return new BodyView(
            (BodyType)(Int("Type") ?? 1),
            Int("EstimatedDataSize"),
            Int("Truncated"),
            Child("Data"),
            Child("Preview"));
    }

    private static EmailItem Html(string body) =>
        new() { Subject = "s", Body = body, BodyType = 2, NativeBodyType = 2 };

    private static EmailItem Plain(string body) =>
        new() { Subject = "s", Body = body, BodyType = 1, NativeBodyType = 1 };

    [Fact]
    public void Html_item_requested_as_plain_text_is_converted_and_labelled_plain()
    {
        var e = Html("<html><body><p>Hello</p><p>World</p></body></html>");

        var body = Body(e, new BodyPreference { Type = BodyType.PlainText })!;

        Assert.Equal(BodyType.PlainText, body.Type);
        Assert.DoesNotContain("<p>", body.Data);
        Assert.Contains("Hello", body.Data);
        Assert.Contains("World", body.Data);
    }

    [Fact]
    public void Html_item_requested_as_html_is_returned_as_stored()
    {
        var e = Html("<html><body><p>Hello</p></body></html>");

        var body = Body(e, new BodyPreference { Type = BodyType.HTML })!;

        Assert.Equal(BodyType.HTML, body.Type);
        Assert.Contains("<p>Hello</p>", body.Data);
    }

    [Fact]
    public void Plain_item_requested_as_html_is_converted_and_labelled_html()
    {
        var e = Plain("a < b & c");

        var body = Body(e, new BodyPreference { Type = BodyType.HTML })!;

        Assert.Equal(BodyType.HTML, body.Type);
        Assert.Contains("&lt;", body.Data);
        Assert.Contains("&amp;", body.Data);
    }

    // MS-ASAIRS 2.2.2.9: the preference matching the native format wins, regardless of order
    [Fact]
    public void Native_type_preference_wins_over_the_first_listed()
    {
        var e = Plain("just text");

        var body = Body(e,
            new BodyPreference { Type = BodyType.HTML },
            new BodyPreference { Type = BodyType.PlainText })!;

        Assert.Equal(BodyType.PlainText, body.Type);
        Assert.Equal("just text", body.Data);
    }

    [Fact]
    public void Html_entities_are_decoded_when_converting_to_plain_text()
    {
        var e = Html("<div>caf&eacute; &amp; cr&#232;me</div>");

        var body = Body(e, new BodyPreference { Type = BodyType.PlainText })!;

        Assert.Contains("café", body.Data);
        Assert.Contains("&", body.Data);
        Assert.Contains("crème", body.Data);
    }

    [Fact]
    public void Script_and_style_content_is_dropped_when_converting_to_plain_text()
    {
        var e = Html("<style>p{color:red}</style><script>alert(1)</script><p>Visible</p>");

        var body = Body(e, new BodyPreference { Type = BodyType.PlainText })!;

        Assert.DoesNotContain("color:red", body.Data);
        Assert.DoesNotContain("alert", body.Data);
        Assert.Contains("Visible", body.Data);
    }

    // --- sizes and truncation --------------------------------------------------------

    [Fact]
    public void EstimatedDataSize_is_counted_in_bytes_not_utf16_chars()
    {
        // 5 CJK code points: 5 chars in UTF-16, 15 bytes in UTF-8
        var e = Plain("東京タワー");

        var body = Body(e, new BodyPreference { Type = BodyType.PlainText })!;

        Assert.Equal(15, body.EstimatedDataSize);
    }

    [Fact]
    public void Truncation_cuts_on_a_utf8_boundary()
    {
        var e = Plain("東京タワー");

        // 7 bytes lands mid-character; the cut must back off rather than emit a partial sequence
        var body = Body(e, new BodyPreference { Type = BodyType.PlainText, TruncationSize = 7 })!;

        Assert.Equal(1, body.Truncated);
        Assert.Equal("東京", body.Data);
        Assert.DoesNotContain('�', body.Data!);
        Assert.True(Encoding.UTF8.GetByteCount(body.Data!) <= 7);
    }

    [Fact]
    public void AllOrNone_withholds_the_body_instead_of_truncating()
    {
        var e = Plain(new string('x', 500));

        var body = Body(e, new BodyPreference
        { Type = BodyType.PlainText, TruncationSize = 100, AllOrNone = 1 })!;

        Assert.Null(body.Data);
        Assert.Equal(1, body.Truncated);
        Assert.Equal(500, body.EstimatedDataSize);
    }

    [Fact]
    public void AllOrNone_on_the_native_type_falls_back_to_another_offered_preference()
    {
        var e = Html("<p>" + new string('x', 500) + "</p>");

        // native (HTML) refuses partial data, so the plain-text preference should serve instead
        var body = Body(e,
            new BodyPreference { Type = BodyType.HTML, TruncationSize = 10, AllOrNone = 1 },
            new BodyPreference { Type = BodyType.PlainText, TruncationSize = 100 })!;

        Assert.Equal(BodyType.PlainText, body.Type);
        Assert.Equal(1, body.Truncated);
        Assert.NotNull(body.Data);
    }

    [Fact]
    public void Untruncated_body_reports_no_Truncated_element()
    {
        var e = Plain("short");

        var body = Body(e, new BodyPreference { Type = BodyType.PlainText, TruncationSize = 1000 })!;

        Assert.Null(body.Truncated);
        Assert.Equal("short", body.Data);
    }

    // --- MIME ------------------------------------------------------------------------

    [Fact]
    public void Mime_is_returned_when_requested_and_stored()
    {
        var e = new EmailItem { Subject = "s", Body = "text", BodyType = 1, MimeRaw = "From: a@b\r\n\r\nhi" };

        var body = Body(e, new BodyPreference { Type = BodyType.MIME })!;

        Assert.Equal(BodyType.MIME, body.Type);
        Assert.Contains("From: a@b", body.Data);
    }

    [Fact]
    public void Mime_body_is_truncated_like_any_other()
    {
        var e = new EmailItem
        {
            Subject = "s",
            MimeRaw = "From: a@b\r\n\r\n" + new string('y', 5000),
        };

        var body = Body(e, new BodyPreference { Type = BodyType.MIME, TruncationSize = 50 })!;

        Assert.Equal(BodyType.MIME, body.Type);
        Assert.Equal(1, body.Truncated);
        Assert.Equal(50, Encoding.UTF8.GetByteCount(body.Data!));
        Assert.Equal(5013, body.EstimatedDataSize);
    }

    [Fact]
    public void Mime_request_falls_back_to_the_stored_body_when_no_mime_is_held()
    {
        var e = Plain("just text");

        var body = Body(e, new BodyPreference { Type = BodyType.MIME })!;

        Assert.Equal(BodyType.PlainText, body.Type);
        Assert.Equal("just text", body.Data);
    }

    // --- absence ---------------------------------------------------------------------

    // WP7 refuses an item carrying a body it didn't ask for and wedges the collection
    [Fact]
    public void No_body_preference_means_no_body_element()
    {
        Assert.Null(Body(Plain("text")));
    }

    [Fact]
    public void Empty_body_emits_no_body_element_rather_than_an_empty_one()
    {
        var e = new EmailItem { Subject = "s", Body = "   ", BodyType = 1 };

        Assert.Null(Body(e, new BodyPreference { Type = BodyType.PlainText }));
    }
}
