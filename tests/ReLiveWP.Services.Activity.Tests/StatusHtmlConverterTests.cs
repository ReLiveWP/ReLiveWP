using ReLiveWP.Services.Activity.Providers.Mastodon;

namespace ReLiveWP.Services.Activity.Tests;

public class StatusHtmlConverterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("plain text", "plain text")]
    public void Nothing_much_in_means_nothing_much_out(string? html, string expected)
    {
        Assert.Equal(expected, StatusHtmlConverter.ToPlainText(html));
    }

    [Fact]
    public void Paragraphs_become_blank_lines_and_br_becomes_a_newline()
    {
        var text = StatusHtmlConverter.ToPlainText("<p>first line<br>second line</p><p>next paragraph</p>");

        Assert.Equal("first line\nsecond line\n\nnext paragraph", text);
    }

    // mastodon hides part of a long url in invisible spans, the text has to carry the whole thing
    [Fact]
    public void A_shortened_link_comes_out_whole()
    {
        const string html = """<p>look <a href="https://example.com/a/very/long/path" rel="nofollow noopener" target="_blank"><span class="invisible">https://</span><span class="ellipsis">example.com/a/very/lo</span><span class="invisible">ng/path</span></a> ok</p>""";

        Assert.Equal("look https://example.com/a/very/long/path ok", StatusHtmlConverter.ToPlainText(html));
    }

    [Fact]
    public void Mentions_and_hashtags_keep_their_sigils()
    {
        const string html = """<p><span class="h-card" translate="no"><a href="https://snug.moe/@wam" class="u-url mention">@<span>wam</span></a></span> hi <a href="https://mastodon.social/tags/fedi" class="mention hashtag" rel="tag">#<span>fedi</span></a></p>""";

        Assert.Equal("@wam hi #fedi", StatusHtmlConverter.ToPlainText(html));
    }

    [Fact]
    public void Entities_are_decoded()
    {
        Assert.Equal("fish & chips <3 \"quoted\" it's", StatusHtmlConverter.ToPlainText("<p>fish &amp; chips &lt;3 &quot;quoted&quot; it&#39;s</p>"));
    }

    [Fact]
    public void Iceshrimp_span_wrapped_paragraphs_read_cleanly()
    {
        Assert.Equal("did i break bridgyfed :(", StatusHtmlConverter.ToPlainText("<p><span>did i break bridgyfed :(</span></p>"));
    }

    [Fact]
    public void Source_whitespace_is_collapsed()
    {
        Assert.Equal("a b c", StatusHtmlConverter.ToPlainText("<p>a\n   b\t\tc</p>"));
    }

    [Fact]
    public void Preformatted_text_keeps_its_whitespace()
    {
        var text = StatusHtmlConverter.ToPlainText("<p>code:</p><pre><code>if (x)\n    y();</code></pre>");

        Assert.Equal("code:\n\nif (x)\n    y();", text);
    }

    [Fact]
    public void Lists_become_dashed_lines()
    {
        var text = StatusHtmlConverter.ToPlainText("<p>todo</p><ul><li>one</li><li>two</li></ul><p>done</p>");

        Assert.Equal("todo\n\n- one\n- two\n\ndone", text);
    }

    [Fact]
    public void Scripts_styles_and_comments_are_dropped()
    {
        var text = StatusHtmlConverter.ToPlainText("<p>a<script>alert(1)</script><style>p{}</style><!-- hidden -->b</p>");

        Assert.Equal("ab", text);
    }

    [Fact]
    public void Broken_markup_still_gives_the_text()
    {
        Assert.Equal("unclosed bold\n\nand a < b", StatusHtmlConverter.ToPlainText("<p>unclosed <b>bold</p><p>and a &lt; b"));
    }

    [Fact]
    public void Attributes_with_angle_brackets_do_not_leak_into_the_text()
    {
        Assert.Equal("link", StatusHtmlConverter.ToPlainText("""<a href="https://x.example/?a=>b" title="1 > 0">link</a>"""));
    }

    [Fact]
    public void A_quote_post_prefix_reads_as_text()
    {
        const string html = """<p class="quote-inline">RE: <a href="https://mastodon.social/@Gargron/1"><span class="invisible">https://</span><span class="ellipsis">mastodon.social/@Gargron/1</span><span class="invisible"></span></a></p><p>reply text</p>""";

        Assert.Equal("RE: https://mastodon.social/@Gargron/1\n\nreply text", StatusHtmlConverter.ToPlainText(html));
    }
}
